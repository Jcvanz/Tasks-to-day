using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Tasks.App.Data;
using Tasks.App.Models;

namespace Tasks.App.Services;

public class AuthResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public User? User { get; set; }
    public bool RequiresVerification { get; set; }
}

public interface IAuthService
{
    User? CurrentUser { get; }
    Task<User?> GetActiveSessionUserAsync();
    Task<AuthResult> LoginAsync(string email, string password, bool rememberMe);
    Task<AuthResult> RegisterAsync(string name, string email, string password);
    Task<AuthResult> VerifyCodeAsync(string email, string code);
    Task<bool> ResendCodeAsync(string email);
    Task LogoutAsync();
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;

    public User? CurrentUser { get; private set; }

    public AuthService(AppDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    public async Task<User?> GetActiveSessionUserAsync()
    {
        await _context.EnsureTablesCreatedAsync();

        var session = await _context.UserSessions
            .Include(s => s.User)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();

        if (session?.User != null && session.User.IsEmailVerified)
        {
            CurrentUser = session.User;
            return CurrentUser;
        }

        return null;
    }

    public async Task<AuthResult> RegisterAsync(string name, string email, string password)
    {
        await _context.EnsureTablesCreatedAsync();

        name = name.Trim();
        email = email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(name))
            return new AuthResult { Success = false, ErrorMessage = "Por favor, informe seu nome." };

        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@') || !email.Contains('.'))
            return new AuthResult { Success = false, ErrorMessage = "Por favor, informe um e-mail válido." };

        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
            return new AuthResult { Success = false, ErrorMessage = "A senha deve conter no mínimo 6 caracteres." };

        var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (existingUser != null)
        {
            if (existingUser.IsEmailVerified)
            {
                return new AuthResult { Success = false, ErrorMessage = "Este e-mail já está cadastrado. Faça login." };
            }
            // Se já cadastrou mas não verificou, reutilizar e reenviar código
            existingUser.Name = name;
            var (newHash, newSalt) = HashPassword(password);
            existingUser.PasswordHash = newHash;
            existingUser.PasswordSalt = newSalt;
            await _context.SaveChangesAsync();

            await GenerateAndSendVerificationCodeAsync(existingUser);
            return new AuthResult { Success = true, User = existingUser, RequiresVerification = true };
        }

        var (hash, salt) = HashPassword(password);
        var user = new User
        {
            Name = name,
            Email = email,
            PasswordHash = hash,
            PasswordSalt = salt,
            IsEmailVerified = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        await GenerateAndSendVerificationCodeAsync(user);

        return new AuthResult
        {
            Success = true,
            User = user,
            RequiresVerification = true
        };
    }

    public async Task<AuthResult> VerifyCodeAsync(string email, string code)
    {
        email = email.Trim().ToLowerInvariant();
        code = code.Trim();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return new AuthResult { Success = false, ErrorMessage = "Usuário não encontrado." };

        var verification = await _context.EmailVerificationCodes
            .Where(c => c.UserId == user.Id && !c.IsUsed && c.Code == code && c.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync();

        if (verification == null)
        {
            return new AuthResult { Success = false, ErrorMessage = "Código inválido ou expirado. Tente reenviar o código." };
        }

        verification.IsUsed = true;
        user.IsEmailVerified = true;

        // Criar sessão de login
        var session = new UserSession
        {
            UserId = user.Id,
            SessionToken = Guid.NewGuid().ToString("N"),
            CreatedAt = DateTime.UtcNow
        };
        await _context.UserSessions.AddAsync(session);
        await _context.SaveChangesAsync();

        CurrentUser = user;
        return new AuthResult { Success = true, User = user };
    }

    public async Task<bool> ResendCodeAsync(string email)
    {
        email = email.Trim().ToLowerInvariant();
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null) return false;

        await GenerateAndSendVerificationCodeAsync(user);
        return true;
    }

    public async Task<AuthResult> LoginAsync(string email, string password, bool rememberMe)
    {
        await _context.EnsureTablesCreatedAsync();

        email = email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user == null)
            return new AuthResult { Success = false, ErrorMessage = "E-mail ou senha incorretos." };

        if (!VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
            return new AuthResult { Success = false, ErrorMessage = "E-mail ou senha incorretos." };

        if (!user.IsEmailVerified)
        {
            await GenerateAndSendVerificationCodeAsync(user);
            return new AuthResult
            {
                Success = false,
                ErrorMessage = "Seu e-mail ainda não foi verificado. Enviamos um novo código de verificação para o seu e-mail.",
                User = user,
                RequiresVerification = true
            };
        }

        if (rememberMe)
        {
            // Limpar sessões antigas
            var oldSessions = await _context.UserSessions.Where(s => s.UserId == user.Id).ToListAsync();
            _context.UserSessions.RemoveRange(oldSessions);

            var session = new UserSession
            {
                UserId = user.Id,
                SessionToken = Guid.NewGuid().ToString("N"),
                CreatedAt = DateTime.UtcNow
            };
            await _context.UserSessions.AddAsync(session);
            await _context.SaveChangesAsync();
        }

        CurrentUser = user;
        return new AuthResult { Success = true, User = user };
    }

    public async Task LogoutAsync()
    {
        if (CurrentUser != null)
        {
            var sessions = await _context.UserSessions.Where(s => s.UserId == CurrentUser.Id).ToListAsync();
            _context.UserSessions.RemoveRange(sessions);
            await _context.SaveChangesAsync();
        }
        CurrentUser = null;
    }

    private async Task GenerateAndSendVerificationCodeAsync(User user)
    {
        // Gerar código aleatório de 6 dígitos
        var random = new Random();
        string code = random.Next(100000, 999999).ToString();

        var verification = new EmailVerificationCode
        {
            UserId = user.Id,
            Code = code,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            IsUsed = false,
            CreatedAt = DateTime.UtcNow
        };

        await _context.EmailVerificationCodes.AddAsync(verification);
        await _context.SaveChangesAsync();

        _ = _emailService.SendVerificationCodeEmailAsync(user.Email, user.Name, code);
    }

    // Hash PBKDF2 com Salt
    private static (string Hash, string Salt) HashPassword(string password)
    {
        byte[] saltBytes = new byte[16];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(saltBytes);
        string salt = Convert.ToBase64String(saltBytes);

        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        string hash = Convert.ToBase64String(hashBytes);
        return (hash, salt);
    }

    private static bool VerifyPassword(string password, string storedHash, string storedSalt)
    {
        byte[] saltBytes = Convert.FromBase64String(storedSalt);
        byte[] hashBytes = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            saltBytes,
            iterations: 100_000,
            hashAlgorithm: HashAlgorithmName.SHA256,
            outputLength: 32);

        string computedHash = Convert.ToBase64String(hashBytes);
        return computedHash == storedHash;
    }
}

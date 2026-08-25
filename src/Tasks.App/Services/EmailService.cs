using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text.Json;
using System.Windows;

namespace Tasks.App.Services;

public class SmtpConfig
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string SenderName { get; set; } = "Tasks App";
    public string SenderEmail { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool EnableSsl { get; set; } = true;
}

public class AppConfig
{
    public SmtpConfig? SmtpSettings { get; set; }
}

public interface IEmailService
{
    Task<bool> SendVerificationCodeEmailAsync(string toEmail, string userName, string code);
}

public class EmailService : IEmailService
{
    private SmtpConfig? _smtpConfig;

    public EmailService()
    {
        LoadConfiguration();
    }

    private void LoadConfiguration()
    {
        _smtpConfig = new SmtpConfig();

        // Carrega do arquivo appsettings.json se existir
        try
        {
            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
            if (File.Exists(configPath))
            {
                string json = File.ReadAllText(configPath);
                var config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config?.SmtpSettings != null)
                {
                    _smtpConfig = config.SmtpSettings;
                }
            }
        }
        catch
        {
            // fallback
        }

        // Carrega do arquivo .env (se existir na raiz ou na pasta de execução)
        LoadFromDotEnvFile();
    }

    private void LoadFromDotEnvFile()
    {
        try
        {
            // Procurar .env no diretório base ou nos diretórios pai (raiz do projeto/solução)
            var candidates = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
                Path.Combine(Directory.GetCurrentDirectory(), ".env")
            };

            var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
            while (dir != null)
            {
                candidates.Add(Path.Combine(dir.FullName, ".env"));
                dir = dir.Parent;
            }

            foreach (var path in candidates)
            {
                if (File.Exists(path))
                {
                    var lines = File.ReadAllLines(path);
                    foreach (var rawLine in lines)
                    {
                        var line = rawLine.Trim();
                        if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) continue;

                        int eqIndex = line.IndexOf('=');
                        if (eqIndex > 0)
                        {
                            string key = line.Substring(0, eqIndex).Trim().ToUpperInvariant();
                            string val = line.Substring(eqIndex + 1).Trim().Trim('"', '\'');

                            _smtpConfig ??= new SmtpConfig();

                            if (key is "SMTP_HOST" or "TASKS_SMTP_HOST" or "HOST") _smtpConfig.Host = val;
                            else if (key is "SMTP_PORT" or "TASKS_SMTP_PORT" or "PORT")
                            {
                                if (int.TryParse(val, out int port)) _smtpConfig.Port = port;
                            }
                            else if (key is "SMTP_USER" or "SMTP_USERNAME" or "TASKS_SMTP_USER" or "USERNAME") _smtpConfig.Username = val;
                            else if (key is "SMTP_PASS" or "SMTP_PASSWORD" or "TASKS_SMTP_PASS" or "PASSWORD") _smtpConfig.Password = val;
                            else if (key is "SMTP_SENDER_NAME" or "SENDER_NAME") _smtpConfig.SenderName = val;
                            else if (key is "SMTP_SENDER_EMAIL" or "SENDER_EMAIL") _smtpConfig.SenderEmail = val;
                        }
                    }
                    break;
                }
            }
        }
        catch
        {
            // ignora erros de leitura de .env
        }
    }

    public async Task<bool> SendVerificationCodeEmailAsync(string toEmail, string userName, string code)
    {
        string htmlBody = $@"
        <div style='font-family: Arial, sans-serif; background-color: #0f172a; padding: 40px 20px; color: #f8fafc;'>
            <div style='max-width: 500px; margin: 0 auto; background-color: #1e293b; border-radius: 16px; padding: 32px; border: 1px solid #334155; text-align: center;'>
                <div style='display: inline-block; width: 50px; height: 50px; background-color: #6366f1; border-radius: 12px; line-height: 50px; font-size: 24px; color: white; margin-bottom: 20px;'>
                    ✓
                </div>
                <h2 style='color: #ffffff; margin-bottom: 8px;'>Verificação de Conta</h2>
                <p style='color: #94a3b8; font-size: 14px; margin-bottom: 24px;'>
                    Olá, <strong>{userName}</strong>! Use o código de 6 dígitos abaixo para confirmar seu cadastro no <strong>Tasks</strong>.
                </p>
                <div style='background-color: #0f172a; border: 2px dashed #6366f1; border-radius: 12px; padding: 18px; margin-bottom: 24px;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #818cf8;'>{code}</span>
                </div>
                <p style='color: #64748b; font-size: 12px; margin-bottom: 0;'>
                    Este código expira em 15 minutos. Se você não solicitou este cadastro, desconsidere esta mensagem.
                </p>
            </div>
        </div>";

        // Se houver configuração SMTP válida
        if (_smtpConfig != null && 
            !string.IsNullOrWhiteSpace(_smtpConfig.Host) && 
            !string.IsNullOrWhiteSpace(_smtpConfig.Username) && 
            !string.IsNullOrWhiteSpace(_smtpConfig.Password))
        {
            try
            {
                using var client = new SmtpClient(_smtpConfig.Host, _smtpConfig.Port)
                {
                    Credentials = new NetworkCredential(_smtpConfig.Username, _smtpConfig.Password),
                    EnableSsl = _smtpConfig.EnableSsl
                };

                string senderEmail = !string.IsNullOrWhiteSpace(_smtpConfig.SenderEmail) ? _smtpConfig.SenderEmail : _smtpConfig.Username;
                using var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, _smtpConfig.SenderName),
                    Subject = $"{code} é o seu código de verificação no Tasks",
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                ShowDevCodeModal(toEmail, code, $"Tentativa de envio SMTP falhou ({ex.Message}).");
                return true;
            }
        }
        else
        {
            // Modo local de desenvolvimento quando SMTP ainda não foi preenchido
            ShowDevCodeModal(toEmail, code, "Para envio de e-mails reais, configure o arquivo '.env' ou 'appsettings.json' com seu SMTP.");
            return true;
        }
    }

    private static void ShowDevCodeModal(string email, string code, string note)
    {
        Application.Current?.Dispatcher?.Invoke(() =>
        {
            MessageBox.Show(
                $"Código de Verificação gerado para [{email}]:\n\n👉  {code}  👈\n\n{note}",
                "Verificação de E-mail (Tasks)",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        });
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public enum AuthViewState
{
    Login,
    Register,
    Verification
}

public partial class AuthViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private AuthViewState _currentState = AuthViewState.Login;

    // Login fields
    [ObservableProperty]
    private string _loginEmail = string.Empty;

    [ObservableProperty]
    private string _loginPassword = string.Empty;

    [ObservableProperty]
    private bool _rememberMe = true;

    // Register fields
    [ObservableProperty]
    private string _registerName = string.Empty;

    [ObservableProperty]
    private string _registerEmail = string.Empty;

    [ObservableProperty]
    private string _registerPassword = string.Empty;

    [ObservableProperty]
    private string _registerConfirmPassword = string.Empty;

    // Verification fields
    [ObservableProperty]
    private string _verificationEmail = string.Empty;

    [ObservableProperty]
    private string _verificationCode = string.Empty;

    // Status / Messages
    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private string? _successMessage;

    [ObservableProperty]
    private bool _isLoading;

    public bool IsLoginState => CurrentState == AuthViewState.Login;
    public bool IsRegisterState => CurrentState == AuthViewState.Register;
    public bool IsVerificationState => CurrentState == AuthViewState.Verification;

    public event Action? AuthenticationSucceeded;

    public AuthViewModel(IAuthService authService)
    {
        _authService = authService;
    }

    [RelayCommand]
    public void SwitchToRegister()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        CurrentState = AuthViewState.Register;
        NotifyStateProperties();
    }

    [RelayCommand]
    public void SwitchToLogin()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        CurrentState = AuthViewState.Login;
        NotifyStateProperties();
    }

    [RelayCommand]
    public async Task LoginAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;

        try
        {
            var result = await _authService.LoginAsync(LoginEmail, LoginPassword, RememberMe);
            if (result.Success)
            {
                AuthenticationSucceeded?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
                if (result.RequiresVerification)
                {
                    VerificationEmail = LoginEmail;
                    CurrentState = AuthViewState.Verification;
                    NotifyStateProperties();
                }
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task RegisterAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (RegisterPassword != RegisterConfirmPassword)
        {
            ErrorMessage = "As senhas não coincidem.";
            return;
        }

        IsLoading = true;
        try
        {
            var result = await _authService.RegisterAsync(RegisterName, RegisterEmail, RegisterPassword);
            if (result.Success)
            {
                VerificationEmail = RegisterEmail;
                SuccessMessage = "Código de verificação enviado para o seu e-mail!";
                CurrentState = AuthViewState.Verification;
                NotifyStateProperties();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task VerifyCodeAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;

        try
        {
            var result = await _authService.VerifyCodeAsync(VerificationEmail, VerificationCode);
            if (result.Success)
            {
                AuthenticationSucceeded?.Invoke();
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ResendCodeAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;
        IsLoading = true;

        try
        {
            bool sent = await _authService.ResendCodeAsync(VerificationEmail);
            if (sent)
            {
                SuccessMessage = "Novo código reenviado para o seu e-mail!";
            }
            else
            {
                ErrorMessage = "Falha ao reenviar código.";
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotifyStateProperties()
    {
        OnPropertyChanged(nameof(IsLoginState));
        OnPropertyChanged(nameof(IsRegisterState));
        OnPropertyChanged(nameof(IsVerificationState));
    }
}

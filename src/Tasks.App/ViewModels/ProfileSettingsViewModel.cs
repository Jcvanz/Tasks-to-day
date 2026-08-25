using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class ProfileSettingsViewModel : ObservableObject
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string? _phone;

    [ObservableProperty]
    private string? _profilePicturePath;

    // Alteração de Senha
    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmNewPassword = string.Empty;

    // Exclusão de Conta
    [ObservableProperty]
    private string _deleteConfirmationPassword = string.Empty;

    // Mensagens de Feedback
    [ObservableProperty]
    private string? _profileErrorMessage;

    [ObservableProperty]
    private string? _profileSuccessMessage;

    [ObservableProperty]
    private string? _passwordErrorMessage;

    [ObservableProperty]
    private string? _passwordSuccessMessage;

    [ObservableProperty]
    private string? _deleteErrorMessage;

    [ObservableProperty]
    private bool _isLoading;

    public bool HasProfilePicture => !string.IsNullOrWhiteSpace(ProfilePicturePath) && File.Exists(ProfilePicturePath);

    public event Action? ProfileUpdated;
    public event Action? AccountDeleted;

    public ProfileSettingsViewModel(IAuthService authService)
    {
        _authService = authService;
        LoadUserData();
    }

    public void LoadUserData()
    {
        var user = _authService.CurrentUser;
        if (user != null)
        {
            Name = user.Name;
            Email = user.Email;
            Phone = user.Phone;
            ProfilePicturePath = user.ProfilePicturePath;
            OnPropertyChanged(nameof(HasProfilePicture));
        }
    }

    [RelayCommand]
    public void ChooseProfilePicture()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Selecionar Foto de Perfil",
            Filter = "Imagens (*.png;*.jpg;*.jpeg;*.webp)|*.png;*.jpg;*.jpeg;*.webp|Todos os Arquivos (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true)
        {
            ProfilePicturePath = dialog.FileName;
            OnPropertyChanged(nameof(HasProfilePicture));
        }
    }

    [RelayCommand]
    public void RemoveProfilePicture()
    {
        ProfilePicturePath = null;
        OnPropertyChanged(nameof(HasProfilePicture));
    }

    [RelayCommand]
    public async Task SaveProfileAsync()
    {
        ProfileErrorMessage = null;
        ProfileSuccessMessage = null;

        var user = _authService.CurrentUser;
        if (user == null) return;

        IsLoading = true;
        try
        {
            var result = await _authService.UpdateProfileAsync(user.Id, Name, Phone, ProfilePicturePath);
            if (result.Success)
            {
                ProfileSuccessMessage = "Perfil atualizado com sucesso!";
                LoadUserData();
                ProfileUpdated?.Invoke();
            }
            else
            {
                ProfileErrorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task ChangePasswordAsync()
    {
        PasswordErrorMessage = null;
        PasswordSuccessMessage = null;

        if (NewPassword != ConfirmNewPassword)
        {
            PasswordErrorMessage = "A nova senha e a confirmação não coincidem.";
            return;
        }

        var user = _authService.CurrentUser;
        if (user == null) return;

        IsLoading = true;
        try
        {
            var result = await _authService.ChangePasswordAsync(user.Id, CurrentPassword, NewPassword);
            if (result.Success)
            {
                PasswordSuccessMessage = "Senha alterada com sucesso!";
                CurrentPassword = string.Empty;
                NewPassword = string.Empty;
                ConfirmNewPassword = string.Empty;
            }
            else
            {
                PasswordErrorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    public async Task DeleteAccountAsync()
    {
        DeleteErrorMessage = null;

        if (string.IsNullOrWhiteSpace(DeleteConfirmationPassword))
        {
            DeleteErrorMessage = "Por favor, digite sua senha para confirmar a exclusão.";
            return;
        }

        var user = _authService.CurrentUser;
        if (user == null) return;

        var confirm = MessageBox.Show(
            "Tem certeza de que deseja excluir sua conta permanentemente?\n\nEssa ação é IRREVERSÍVEL e todos os seus dados, tarefas diárias e quadros de objetivos serão apagados.",
            "Confirmar Exclusão Permanente",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        IsLoading = true;
        try
        {
            var result = await _authService.DeleteAccountAsync(user.Id, DeleteConfirmationPassword);
            if (result.Success)
            {
                MessageBox.Show("Sua conta foi excluída com sucesso.", "Conta Excluída", MessageBoxButton.OK, MessageBoxImage.Information);
                AccountDeleted?.Invoke();
            }
            else
            {
                DeleteErrorMessage = result.ErrorMessage;
            }
        }
        finally
        {
            IsLoading = false;
        }
    }
}

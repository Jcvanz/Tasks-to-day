using System.Windows;
using Tasks.App.ViewModels;
using Wpf.Ui.Controls;

namespace Tasks.App.Views;

public partial class ProfileSettingsWindow : FluentWindow
{
    public ProfileSettingsViewModel ViewModel { get; }

    public ProfileSettingsWindow(ProfileSettingsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        ViewModel.AccountDeleted += () =>
        {
            DialogResult = false;
            Close();
        };
    }

    private void CurrentPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.PasswordBox pb)
        {
            ViewModel.CurrentPassword = pb.Password;
        }
    }

    private void NewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.PasswordBox pb)
        {
            ViewModel.NewPassword = pb.Password;
        }
    }

    private void ConfirmNewPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.PasswordBox pb)
        {
            ViewModel.ConfirmNewPassword = pb.Password;
        }
    }

    private void DeletePasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is Wpf.Ui.Controls.PasswordBox pb)
        {
            ViewModel.DeleteConfirmationPassword = pb.Password;
        }
    }
}

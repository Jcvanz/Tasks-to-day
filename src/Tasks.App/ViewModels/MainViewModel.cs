using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Appearance;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IDailyTaskService _dailyTaskService;
    private readonly ITaskService _kanbanService;
    private readonly Func<Task<bool>> _openProfileSettingsFunc;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    [ObservableProperty]
    private bool _isWindowVisible = true;

    [ObservableProperty]
    private int _selectedNavigationIndex = 0; // 0 = Meu Dia a Dia, 1 = Objetivos (Kanban)

    public string CurrentUserName => _authService.CurrentUser?.Name ?? "Minha Conta";
    public string CurrentUserEmail => _authService.CurrentUser?.Email ?? string.Empty;
    public string? CurrentUserProfilePicture => _authService.CurrentUser?.ProfilePicturePath;
    public bool HasUserProfilePicture => !string.IsNullOrWhiteSpace(CurrentUserProfilePicture) && File.Exists(CurrentUserProfilePicture);

    public DailyTasksViewModel DailyTasksVm { get; }
    public KanbanViewModel KanbanVm { get; }

    public event Action? RequestLogout;

    public MainViewModel(
        IAuthService authService, 
        IDailyTaskService dailyTaskService, 
        ITaskService kanbanService, 
        DailyTasksViewModel dailyTasksVm, 
        KanbanViewModel kanbanVm,
        Func<Task<bool>> openProfileSettingsFunc)
    {
        _authService = authService;
        _dailyTaskService = dailyTaskService;
        _kanbanService = kanbanService;
        DailyTasksVm = dailyTasksVm;
        KanbanVm = kanbanVm;
        _openProfileSettingsFunc = openProfileSettingsFunc;
    }

    public void RefreshUserData()
    {
        OnPropertyChanged(nameof(CurrentUserName));
        OnPropertyChanged(nameof(CurrentUserEmail));
        OnPropertyChanged(nameof(CurrentUserProfilePicture));
        OnPropertyChanged(nameof(HasUserProfilePicture));
    }

    [RelayCommand]
    public async Task OpenProfileSettingsAsync()
    {
        bool result = await _openProfileSettingsFunc();
        RefreshUserData();

        // Se o usuário foi excluído ou deslogado
        if (_authService.CurrentUser == null)
        {
            RequestLogout?.Invoke();
        }
    }

    [RelayCommand]
    public void ToggleTheme()
    {
        if (CurrentTheme == ApplicationTheme.Dark)
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Light);
            CurrentTheme = ApplicationTheme.Light;
        }
        else
        {
            ApplicationThemeManager.Apply(ApplicationTheme.Dark);
            CurrentTheme = ApplicationTheme.Dark;
        }
    }

    [RelayCommand]
    public void ShowWindow()
    {
        IsWindowVisible = true;
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.Show();
            Application.Current.MainWindow.WindowState = WindowState.Normal;
            Application.Current.MainWindow.Activate();
        }
    }

    [RelayCommand]
    public void HideWindow()
    {
        IsWindowVisible = false;
        if (Application.Current.MainWindow != null)
        {
            Application.Current.MainWindow.Hide();
        }
    }

    [RelayCommand]
    public void ExitApplication()
    {
        Application.Current.Shutdown();
    }

    [RelayCommand]
    public async Task LogoutAsync()
    {
        var result = MessageBox.Show(
            "Deseja realmente sair da sua conta?",
            "Confirmar Saída",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            await _authService.LogoutAsync();
            RequestLogout?.Invoke();
        }
    }

    [RelayCommand]
    public async Task AddQuickTaskAsync()
    {
        ShowWindow();
        if (SelectedNavigationIndex == 0)
        {
            await DailyTasksVm.AddNewDailyTaskAsync();
        }
        else
        {
            await KanbanVm.AddNewTaskAsync(null);
        }
    }
}

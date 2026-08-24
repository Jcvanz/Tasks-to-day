using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wpf.Ui.Appearance;
using Tasks.App.Services;

namespace Tasks.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ITaskService _taskService;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    [ObservableProperty]
    private bool _isWindowVisible = true;

    public KanbanViewModel KanbanVm { get; }

    public MainViewModel(ITaskService taskService, KanbanViewModel kanbanVm)
    {
        _taskService = taskService;
        KanbanVm = kanbanVm;
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
    public async Task AddQuickTaskAsync()
    {
        ShowWindow();
        await KanbanVm.AddNewTaskAsync(null);
    }
}

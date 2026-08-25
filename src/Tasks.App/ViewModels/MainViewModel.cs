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
    private readonly IDailyTaskService _dailyTaskService;
    private readonly ITaskService _kanbanService;

    [ObservableProperty]
    private ApplicationTheme _currentTheme = ApplicationTheme.Dark;

    [ObservableProperty]
    private bool _isWindowVisible = true;

    [ObservableProperty]
    private int _selectedNavigationIndex = 0; // 0 = Meu Dia a Dia, 1 = Objetivos (Kanban)

    public DailyTasksViewModel DailyTasksVm { get; }
    public KanbanViewModel KanbanVm { get; }

    public MainViewModel(IDailyTaskService dailyTaskService, ITaskService kanbanService, DailyTasksViewModel dailyTasksVm, KanbanViewModel kanbanVm)
    {
        _dailyTaskService = dailyTaskService;
        _kanbanService = kanbanService;
        DailyTasksVm = dailyTasksVm;
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

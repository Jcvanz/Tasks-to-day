using System;
using System.ComponentModel;
using System.Windows;
using Tasks.App.ViewModels;
using Wpf.Ui.Controls;

namespace Tasks.App.Views;

public partial class MainWindow : FluentWindow
{
    public MainViewModel ViewModel { get; }

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.DailyTasksVm.InitializeAsync();
        await ViewModel.KanbanVm.LoadBoardAsync();
    }

    private void NavDailyTasks_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedNavigationIndex = 0;
        DailyTasksViewControl.Visibility = Visibility.Visible;
        GoalsKanbanViewControl.Visibility = Visibility.Collapsed;
    }

    private void NavGoals_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.SelectedNavigationIndex = 1;
        DailyTasksViewControl.Visibility = Visibility.Collapsed;
        GoalsKanbanViewControl.Visibility = Visibility.Visible;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Minimizar para a bandeja do sistema
        e.Cancel = true;
        ViewModel.HideWindow();
        base.OnClosing(e);
    }
}

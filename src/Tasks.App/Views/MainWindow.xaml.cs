using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
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
        await ViewModel.KanbanVm.LoadBoardAsync();
    }

    private void TaskCard_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is TaskItemViewModel selectedTask)
        {
            ViewModel.KanbanVm.EditTaskCommand.Execute(selectedTask);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Minimizar para o System Tray em vez de fechar
        e.Cancel = true;
        ViewModel.HideWindow();
        base.OnClosing(e);
    }
}

using System;
using System.Windows;
using Tasks.App.ViewModels;
using Wpf.Ui.Controls;

namespace Tasks.App.Views;

public partial class TaskEditorWindow : FluentWindow
{
    public TaskEditorViewModel ViewModel { get; }

    public TaskEditorWindow(TaskEditorViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = viewModel;
        InitializeComponent();

        Loaded += TaskEditorWindow_Loaded;
    }

    private void TaskEditorWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Bloqueia seleção de datas passadas para prazos de entrega
            DueDatePicker.DisplayDateStart = DateTime.Today;
        }
        catch
        {
            // fallback
        }
    }

    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ViewModel.Title))
        {
            System.Windows.MessageBox.Show("Por favor, preencha o título da tarefa.", "Aviso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        bool success = await ViewModel.SaveAsync();
        if (success)
        {
            DialogResult = true;
            Close();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}

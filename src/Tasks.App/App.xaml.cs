using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Tasks.App.Data;
using Tasks.App.Services;
using Tasks.App.ViewModels;
using Tasks.App.Views;

namespace Tasks.App;

public partial class App : Application
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Tratamento global de exceções
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Ocorreu um erro inesperado:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}", "Erro no Aplicativo", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Ocorreu um erro fatal:\n\n{ex.Message}\n\n{ex.StackTrace}", "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        };

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao inicializar o aplicativo:\n\n{ex.Message}\n\n{ex.StackTrace}", "Erro na Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Banco de Dados SQLite
        services.AddDbContext<AppDbContext>();

        // Serviços de Negócio
        services.AddScoped<IDailyTaskService, DailyTaskService>();
        services.AddScoped<ITaskService, TaskService>();

        // Fábrica para o Modal de Tarefas Diárias
        services.AddSingleton<Func<DateTime, Task<bool>>>(sp => (initialDate) =>
        {
            var taskService = sp.GetRequiredService<IDailyTaskService>();
            var editorVm = new DailyTaskEditorViewModel(initialDate, taskService);
            var editorWindow = new DailyTaskEditorWindow(editorVm)
            {
                Owner = Application.Current.MainWindow
            };
            var result = editorWindow.ShowDialog();
            return Task.FromResult(result == true);
        });

        // Fábrica para o Modal de Metas do Kanban
        services.AddSingleton<Func<TaskItemViewModel?, int, Task<bool>>>(sp => (existingTask, columnId) =>
        {
            var taskService = sp.GetRequiredService<ITaskService>();
            var editorVm = new TaskEditorViewModel(existingTask, columnId, taskService);
            var editorWindow = new TaskEditorWindow(editorVm)
            {
                Owner = Application.Current.MainWindow
            };
            var result = editorWindow.ShowDialog();
            return Task.FromResult(result == true);
        });

        // ViewModels
        services.AddSingleton<DailyTasksViewModel>();
        services.AddSingleton<KanbanViewModel>();
        services.AddSingleton<MainViewModel>();

        // Janela Principal
        services.AddSingleton<MainWindow>();
    }
}

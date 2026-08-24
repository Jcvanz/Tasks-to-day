using System;
using System.IO;
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

        var services = new ServiceCollection();
        ConfigureServices(services);
        ServiceProvider = services.BuildServiceProvider();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Banco de Dados
        services.AddDbContext<AppDbContext>();

        // Serviços
        services.AddScoped<ITaskService, TaskService>();

        // ViewModels
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

        services.AddSingleton<KanbanViewModel>();
        services.AddSingleton<MainViewModel>();

        // Views
        services.AddSingleton<MainWindow>();
    }
}

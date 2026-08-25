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

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Evita que o app feche automaticamente quando o AuthWindow fechar
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Tratamento global de exceções
        DispatcherUnhandledException += (s, args) =>
        {
            MessageBox.Show($"Ocorreu um aviso no aplicativo:\n\n{args.Exception.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            if (args.ExceptionObject is Exception ex)
            {
                MessageBox.Show($"Erro:\n\n{ex.Message}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        };

        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();

            var authService = ServiceProvider.GetRequiredService<IAuthService>();
            var savedUser = await authService.GetActiveSessionUserAsync();

            if (savedUser == null)
            {
                // Abrir tela de autenticação
                var authWindow = ServiceProvider.GetRequiredService<AuthWindow>();
                bool? authResult = authWindow.ShowDialog();

                if (authResult != true)
                {
                    Shutdown();
                    return;
                }
            }

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            MainWindow = mainWindow;
            
            // Define que o fechamento da janela principal encerra a aplicação
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            mainWindow.Show();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao inicializar o aplicativo:\n\n{ex.Message}\n\n{ex.StackTrace}", "Erro na Inicialização", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Banco de Dados SQLite
        services.AddDbContext<AppDbContext>();

        // Serviços de Negócio
        services.AddSingleton<IEmailService, EmailService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddScoped<IDailyTaskService, DailyTaskService>();
        services.AddScoped<ITaskService, TaskService>();

        // Fábrica para o Modal de Tarefas Diárias
        services.AddSingleton<Func<DateTime, Task<bool>>>(sp => (initialDate) =>
        {
            var taskService = sp.GetRequiredService<IDailyTaskService>();
            var authService = sp.GetRequiredService<IAuthService>();
            var editorVm = new DailyTaskEditorViewModel(initialDate, taskService, authService);
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
        services.AddTransient<AuthViewModel>();
        services.AddSingleton<DailyTasksViewModel>();
        services.AddSingleton<KanbanViewModel>();
        services.AddSingleton<MainViewModel>();

        // Telas / Views
        services.AddTransient<AuthWindow>();
        services.AddSingleton<MainWindow>();
    }
}

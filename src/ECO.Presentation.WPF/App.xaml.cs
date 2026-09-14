using System.IO;
using System.Windows;

using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;
using ECO.Application.Scheduling.Abstractions;
using ECO.Application.Scheduling.UseCases;
using ECO.Infrastructure.Input;
using ECO.Infrastructure.Persistence;
using ECO.Infrastructure.Scheduling;
using ECO.Presentation.WPF.ViewModels;

using Microsoft.Extensions.DependencyInjection;

namespace ECO.Presentation.WPF;

public partial class App : System.Windows.Application
{
    private ServiceProvider? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var connectionString = BuildConnectionString();
        SqliteSchema.EnsureCreated(connectionString);

        _services = BuildServiceProvider(connectionString);

        // O agendador roda em segundo plano enquanto o app estiver aberto.
        _services.GetRequiredService<IScheduleWatcher>().Start();

        _services.GetRequiredService<MainWindow>().Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _services?.GetRequiredService<IScheduleWatcher>().Stop();
        _services?.Dispose();

        base.OnExit(e);
    }

    // O banco fica em %AppData%\ECO\eco.db — fora da pasta de instalação, pra não depender de
    // permissão de escrita em Program Files e pra sobreviver a uma reinstalação do programa.
    private static string BuildConnectionString()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ECO");

        Directory.CreateDirectory(folder);

        return $"Data Source={Path.Combine(folder, "eco.db")}";
    }

    // É aqui, e só aqui, que o projeto decide qual implementação concreta entra em cada contrato.
    // Nenhuma classe do Application ou do Domain sabe que existe SQLite ou API do Windows.
    private static ServiceProvider BuildServiceProvider(string connectionString)
    {
        var services = new ServiceCollection();

        // Infrastructure
        services.AddSingleton<IMacroRepository>(_ => new SqliteMacroRepository(connectionString));
        services.AddSingleton<IScheduleRepository>(_ => new SqliteScheduleRepository(connectionString));
        services.AddSingleton<IInputRecorder, WindowsInputRecorder>();
        services.AddSingleton<IInputPlayer, WindowsInputPlayer>();
        services.AddSingleton<IScheduleWatcher, TimerScheduleWatcher>();
        services.AddSingleton(TimeProvider.System);

        // Application
        services.AddSingleton<ListMacrosUseCase>();
        services.AddSingleton<RecordMacroUseCase>();
        services.AddSingleton<IPlayMacroUseCase, PlayMacroUseCase>();
        services.AddSingleton<PauseResumeMacroUseCase>();
        services.AddSingleton<EditMacroStepUseCase>();
        services.AddSingleton<ScheduleMacroUseCase>();

        // Presentation
        services.AddSingleton<MainWindowViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}

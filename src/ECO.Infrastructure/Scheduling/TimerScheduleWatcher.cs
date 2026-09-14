using Cronos;

using ECO.Application.Macros.Abstractions;
using ECO.Application.Scheduling.Abstractions;
using ECO.Domain.Scheduling;

namespace ECO.Infrastructure.Scheduling;

// Convenção: todo horário aqui dentro (ScheduledAt, avaliação do cron, "agora") é tratado em UTC.
// A conversão de/para o horário local do usuário é responsabilidade da UI, na hora de
// salvar/exibir um Schedule — o Scheduler em si nunca lida com fuso horário local.
public class TimerScheduleWatcher : IScheduleWatcher, IDisposable
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(30);

    private readonly IScheduleRepository _scheduleRepository;
    private readonly IPlayMacroUseCase _playMacroUseCase;
    private readonly TimeProvider _timeProvider;

    private PeriodicTimer? _timer;
    private CancellationTokenSource? _cancellationTokenSource;
    private DateTime _lastCheckedAt;

    public TimerScheduleWatcher(
        IScheduleRepository scheduleRepository, IPlayMacroUseCase playMacroUseCase, TimeProvider? timeProvider = null)
    {
        _scheduleRepository = scheduleRepository;
        _playMacroUseCase = playMacroUseCase;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _lastCheckedAt = _timeProvider.GetUtcNow().UtcDateTime;
    }

    public void Start()
    {
        _lastCheckedAt = _timeProvider.GetUtcNow().UtcDateTime;
        _cancellationTokenSource = new CancellationTokenSource();
        _timer = new PeriodicTimer(CheckInterval);

        _ = RunAsync(_cancellationTokenSource.Token);
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
        _timer?.Dispose();
        _timer = null;
    }

    public void Dispose() => Stop();

    // Público de propósito: é o que permite testar a lógica de "quem está devido" chamando
    // essa checagem diretamente, sem esperar o PeriodicTimer real disparar.
    public async Task CheckSchedulesAsync()
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var schedules = await _scheduleRepository.GetAllAsync();

        foreach (var schedule in schedules.Where(s => s.IsActive))
        {
            if (IsDue(schedule, now))
                await FireAsync(schedule);
        }

        _lastCheckedAt = now;
    }

    private bool IsDue(Schedule schedule, DateTime now)
    {
        if (schedule.ScheduledAt is { } scheduledAt)
            return scheduledAt <= now;

        if (schedule.RecurrenceRule is { } rule)
        {
            var cron = CronExpression.Parse(rule);
            var next = cron.GetNextOccurrence(_lastCheckedAt, TimeZoneInfo.Utc);
            return next is not null && next.Value <= now;
        }

        return false;
    }

    private async Task FireAsync(Schedule schedule)
    {
        await _playMacroUseCase.PlayAsync(schedule.MacroId);

        if (schedule.ScheduledAt is not null)
        {
            // Execução única: desativa depois de rodar, pra não disparar de novo no próximo tick.
            schedule.IsActive = false;
            await _scheduleRepository.UpdateAsync(schedule);
        }
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_timer is not null && await _timer.WaitForNextTickAsync(cancellationToken))
            {
                await CheckSchedulesAsync();
            }
        }
        catch (OperationCanceledException)
        {
            // Stop() foi chamado — encerra o loop normalmente.
        }
    }
}

using ECO.Application.Macros.Abstractions;
using ECO.Application.Scheduling.Abstractions;
using ECO.Domain.Scheduling;
using ECO.Infrastructure.Scheduling;

using NSubstitute;

namespace ECO.Infrastructure.Tests.Scheduling;

public class TimerScheduleWatcherTests
{
    private readonly IScheduleRepository _scheduleRepository = Substitute.For<IScheduleRepository>();
    private readonly IPlayMacroUseCase _playMacroUseCase = Substitute.For<IPlayMacroUseCase>();
    private readonly FixedTimeProvider _timeProvider = new();

    private TimerScheduleWatcher CreateWatcher() => new(_scheduleRepository, _playMacroUseCase, _timeProvider);

    [Fact(DisplayName = "Agendamento único com data no passado dispara e vira inativo")]
    public async Task CheckSchedulesAsync_OneTimeInThePast_FiresAndDeactivates()
    {
        // Arrange
        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);
        var schedule = new Schedule
        {
            Id = 1, MacroId = 42, ScheduledAt = new DateTime(2026, 9, 15, 9, 0, 0), IsActive = true
        };
        _scheduleRepository.GetAllAsync().Returns(new List<Schedule> { schedule });

        // Act
        await CreateWatcher().CheckSchedulesAsync();

        // Assert
        await _playMacroUseCase.Received(1).PlayAsync(42, Arg.Any<CancellationToken>());
        Assert.False(schedule.IsActive);
        await _scheduleRepository.Received(1).UpdateAsync(schedule);
    }

    [Fact(DisplayName = "Agendamento único com data no futuro não dispara")]
    public async Task CheckSchedulesAsync_OneTimeInTheFuture_DoesNotFire()
    {
        // Arrange
        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 8, 0, 0, TimeSpan.Zero);
        var schedule = new Schedule
        {
            Id = 1, MacroId = 42, ScheduledAt = new DateTime(2026, 9, 15, 9, 0, 0), IsActive = true
        };
        _scheduleRepository.GetAllAsync().Returns(new List<Schedule> { schedule });

        // Act
        await CreateWatcher().CheckSchedulesAsync();

        // Assert
        await _playMacroUseCase.DidNotReceiveWithAnyArgs().PlayAsync(default, default);
    }

    [Fact(DisplayName = "Agendamento inativo nunca dispara, mesmo com data no passado")]
    public async Task CheckSchedulesAsync_InactiveSchedule_NeverFires()
    {
        // Arrange
        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);
        var schedule = new Schedule
        {
            Id = 1, MacroId = 42, ScheduledAt = new DateTime(2026, 9, 15, 9, 0, 0), IsActive = false
        };
        _scheduleRepository.GetAllAsync().Returns(new List<Schedule> { schedule });

        // Act
        await CreateWatcher().CheckSchedulesAsync();

        // Assert
        await _playMacroUseCase.DidNotReceiveWithAnyArgs().PlayAsync(default, default);
    }

    [Fact(DisplayName =
        "Agendamento recorrente dispara quando o horário do cron cai entre a última checagem e agora, e continua ativo")]
    public async Task CheckSchedulesAsync_RecurringDue_FiresAndStaysActive()
    {
        // Arrange — o construtor já usa esse "agora" pra semear a última checagem em 08:59
        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 8, 59, 0, TimeSpan.Zero);
        var watcher = CreateWatcher();

        var schedule = new Schedule { Id = 1, MacroId = 42, RecurrenceRule = "0 9 * * *", IsActive = true };
        _scheduleRepository.GetAllAsync().Returns(new List<Schedule> { schedule });

        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 9, 0, 30, TimeSpan.Zero);

        // Act
        await watcher.CheckSchedulesAsync();

        // Assert
        await _playMacroUseCase.Received(1).PlayAsync(42, Arg.Any<CancellationToken>());
        Assert.True(schedule.IsActive);
        await _scheduleRepository.DidNotReceive().UpdateAsync(Arg.Any<Schedule>());
    }

    [Fact(DisplayName = "Agendamento recorrente não dispara fora do horário do cron")]
    public async Task CheckSchedulesAsync_RecurringNotDue_DoesNotFire()
    {
        // Arrange
        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 10, 0, 0, TimeSpan.Zero);
        var watcher = CreateWatcher();

        var schedule = new Schedule { Id = 1, MacroId = 42, RecurrenceRule = "0 9 * * *", IsActive = true };
        _scheduleRepository.GetAllAsync().Returns(new List<Schedule> { schedule });

        _timeProvider.Now = new DateTimeOffset(2026, 9, 15, 10, 5, 0, TimeSpan.Zero);

        // Act
        await watcher.CheckSchedulesAsync();

        // Assert
        await _playMacroUseCase.DidNotReceiveWithAnyArgs().PlayAsync(default, default);
    }

    // Dublê de TimeProvider — deixa o teste controlar "agora" sem esperar tempo real passar.
    private sealed class FixedTimeProvider : TimeProvider
    {
        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }
}

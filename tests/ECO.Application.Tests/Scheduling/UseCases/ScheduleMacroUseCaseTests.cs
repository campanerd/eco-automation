using ECO.Application.Scheduling.Abstractions;
using ECO.Application.Scheduling.UseCases;
using ECO.Domain.Scheduling;
using NSubstitute;

namespace ECO.Application.Tests.Scheduling.UseCases;

public class ScheduleMacroUseCaseTests
{
    private readonly IScheduleRepository _repository = Substitute.For<IScheduleRepository>();
    private readonly ScheduleMacroUseCase _useCase;

    public ScheduleMacroUseCaseTests()
    {
        _useCase = new ScheduleMacroUseCase(_repository);
    }

    [Fact(DisplayName = "Agendamento com data única salva o ScheduledAt")]
    public async Task ScheduleAsync_OneTime_SavesScheduledAt()
    {
        // Arrange
        var scheduledAt = new DateTime(2026, 9, 15, 14, 0, 0);

        // Act
        var result = await _useCase.ScheduleAsync(macroId: 5, scheduledAt: scheduledAt, recurrenceRule: null);

        // Assert
        Assert.Equal(5, result.MacroId);
        Assert.Equal(scheduledAt, result.ScheduledAt);
        Assert.Null(result.RecurrenceRule);
        await _repository.Received(1).AddAsync(Arg.Is<Schedule>(s => s.MacroId == 5));
    }

    [Fact(DisplayName = "Agendamento recorrente salva a RecurrenceRule")]
    public async Task ScheduleAsync_Recurring_SavesRecurrenceRule()
    {
        // Act
        var result = await _useCase.ScheduleAsync(macroId: 5, scheduledAt: null, recurrenceRule: "0 9 * * MON");

        // Assert
        Assert.Equal("0 9 * * MON", result.RecurrenceRule);
        Assert.Null(result.ScheduledAt);
    }

    [Fact(DisplayName = "Novo agendamento nasce ativo por padrão")]
    public async Task ScheduleAsync_DefaultsToActive()
    {
        // Act
        var result = await _useCase.ScheduleAsync(macroId: 1, scheduledAt: null, recurrenceRule: "* * * * *");

        // Assert
        Assert.True(result.IsActive);
    }
}

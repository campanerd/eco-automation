using ECO.Domain.Scheduling;

namespace ECO.Domain.Tests.Scheduling;

public class ScheduleTests
{
    [Fact(DisplayName = "Um novo Schedule nasce ativo por padrão")]
    public void NewSchedule_IsActiveByDefault()
    {
        // Act
        var schedule = new Schedule { MacroId = 1 };

        // Assert
        Assert.True(schedule.IsActive);
    }

    [Fact(DisplayName = "Schedule aceita ScheduledAt sem RecurrenceRule (execução única)")]
    public void Schedule_AllowsOneTimeExecution()
    {
        // Arrange
        var when = new DateTime(2026, 9, 20, 10, 0, 0);

        // Act
        var schedule = new Schedule { MacroId = 1, ScheduledAt = when };

        // Assert
        Assert.Equal(when, schedule.ScheduledAt);
        Assert.Null(schedule.RecurrenceRule);
    }

    [Fact(DisplayName = "Schedule aceita RecurrenceRule sem ScheduledAt (execução recorrente)")]
    public void Schedule_AllowsRecurringExecution()
    {
        // Act
        var schedule = new Schedule { MacroId = 1, RecurrenceRule = "0 9 * * MON" };

        // Assert
        Assert.Equal("0 9 * * MON", schedule.RecurrenceRule);
        Assert.Null(schedule.ScheduledAt);
    }
}

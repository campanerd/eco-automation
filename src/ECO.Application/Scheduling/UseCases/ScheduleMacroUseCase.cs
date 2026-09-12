using ECO.Application.Scheduling.Abstractions;
using ECO.Domain.Scheduling;

namespace ECO.Application.Scheduling.UseCases;

public class ScheduleMacroUseCase(IScheduleRepository repository)
{
    public async Task<Schedule> ScheduleAsync(int macroId, DateTime? scheduledAt, string? recurrenceRule)
    {
        var schedule = new Schedule
        {
            MacroId = macroId,
            ScheduledAt = scheduledAt,
            RecurrenceRule = recurrenceRule,
        };

        await repository.AddAsync(schedule);

        return schedule;
    }
}

using ECO.Domain.Common;

namespace ECO.Domain.Scheduling;

public class Schedule : BaseEntity
{
    public required int MacroId { get; set; }
    public DateTime? ScheduledAt { get; set; }
    public string? RecurrenceRule { get; set; }
    public bool IsActive { get; set; } = true;
}

using ECO.Domain.Common;

namespace ECO.Domain.Macros.Steps;

public abstract class MacroStep : BaseEntity
{
    public int Order { get; set; }
    public int DelayBeforeMs { get; set; }
}

using ECO.Domain.Common;
using ECO.Domain.Macros.Steps;

namespace ECO.Domain.Macros;

public class Macro : BaseEntity
{
    public required string Name { get; set; }
    public List<MacroStep> Steps { get; set; } = [];
}

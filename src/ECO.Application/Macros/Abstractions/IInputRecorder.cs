using ECO.Domain.Macros.Steps;

namespace ECO.Application.Macros.Abstractions;

public interface IInputRecorder
{
    void Start();
    IReadOnlyList<MacroStep> Stop();
}

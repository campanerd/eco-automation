using ECO.Domain.Macros.Steps;

namespace ECO.Application.Macros.Abstractions;

public interface IInputPlayer
{
    Task PlayAsync(IReadOnlyList<MacroStep> steps, CancellationToken cancellationToken = default);
    void Pause();
    void Resume();
}

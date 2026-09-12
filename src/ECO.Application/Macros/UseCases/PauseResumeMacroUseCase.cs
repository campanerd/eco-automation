using ECO.Application.Macros.Abstractions;

namespace ECO.Application.Macros.UseCases;

public class PauseResumeMacroUseCase(IInputPlayer player)
{
    public void Pause() => player.Pause();

    public void Resume() => player.Resume();
}

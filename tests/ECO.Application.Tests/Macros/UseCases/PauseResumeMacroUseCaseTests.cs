using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;

using NSubstitute;

namespace ECO.Application.Tests.Macros.UseCases;

public class PauseResumeMacroUseCaseTests
{
    private readonly IInputPlayer _player = Substitute.For<IInputPlayer>();
    private readonly PauseResumeMacroUseCase _useCase;

    public PauseResumeMacroUseCaseTests()
    {
        _useCase = new PauseResumeMacroUseCase(_player);
    }

    [Fact(DisplayName = "Pause repassa o pedido pro Player")]
    public void Pause_CallsPlayerPause()
    {
        // Act
        _useCase.Pause();

        // Assert
        _player.Received(1).Pause();
    }

    [Fact(DisplayName = "Resume repassa o pedido pro Player")]
    public void Resume_CallsPlayerResume()
    {
        // Act
        _useCase.Resume();

        // Assert
        _player.Received(1).Resume();
    }
}

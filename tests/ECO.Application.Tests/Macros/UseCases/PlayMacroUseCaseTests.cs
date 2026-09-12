using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;
using ECO.Domain.Macros;
using NSubstitute;

namespace ECO.Application.Tests.Macros.UseCases;

public class PlayMacroUseCaseTests
{
    private readonly IMacroRepository _repository = Substitute.For<IMacroRepository>();
    private readonly IInputPlayer _player = Substitute.For<IInputPlayer>();
    private readonly PlayMacroUseCase _useCase;

    public PlayMacroUseCaseTests()
    {
        _useCase = new PlayMacroUseCase(_repository, _player);
    }

    [Fact(DisplayName = "Macro existente é reproduzida com os passos salvos")]
    public async Task PlayAsync_WhenMacroExists_PlaysItsSteps()
    {
        // Arrange
        var macro = new Macro { Id = 1, Name = "Teste", Steps = [] };
        _repository.GetByIdAsync(1).Returns(macro);

        // Act
        await _useCase.PlayAsync(1);

        // Assert
        await _player.Received(1).PlayAsync(macro.Steps, Arg.Any<CancellationToken>());
    }

    [Fact(DisplayName = "Macro inexistente lança exceção e não chama o Player")]
    public async Task PlayAsync_WhenMacroDoesNotExist_Throws()
    {
        // Arrange
        _repository.GetByIdAsync(99).Returns((Macro?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => _useCase.PlayAsync(99));
        await _player.DidNotReceiveWithAnyArgs().PlayAsync(default!, default);
    }

    [Fact(DisplayName = "CancellationToken é propagado até o Player")]
    public async Task PlayAsync_PropagatesCancellationToken()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        var macro = new Macro { Id = 1, Name = "Teste", Steps = [] };
        _repository.GetByIdAsync(1).Returns(macro);

        // Act
        await _useCase.PlayAsync(1, cts.Token);

        // Assert
        await _player.Received(1).PlayAsync(macro.Steps, cts.Token);
    }
}

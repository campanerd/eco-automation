using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;
using ECO.Domain.Macros;
using ECO.Domain.Macros.Steps;

using NSubstitute;

namespace ECO.Application.Tests.Macros.UseCases;

public class EditMacroStepUseCaseTests
{
    private readonly IMacroRepository _repository = Substitute.For<IMacroRepository>();
    private readonly EditMacroStepUseCase _useCase;

    public EditMacroStepUseCaseTests()
    {
        _useCase = new EditMacroStepUseCase(_repository);
    }

    [Fact(DisplayName = "Delay do passo é atualizado e a Macro é salva")]
    public async Task UpdateDelayAsync_WhenStepExists_UpdatesDelayAndSaves()
    {
        // Arrange
        var step = new WaitStep { Id = 10, Order = 1, DelayBeforeMs = 5000, DurationMs = 1000 };
        var macro = new Macro { Id = 1, Name = "Teste", Steps = [step] };
        _repository.GetByIdAsync(1).Returns(macro);

        // Act
        await _useCase.UpdateDelayAsync(macroId: 1, stepId: 10, newDelayBeforeMs: 200);

        // Assert
        Assert.Equal(200, step.DelayBeforeMs);
        await _repository.Received(1).UpdateAsync(macro);
    }

    [Fact(DisplayName = "Macro inexistente lança exceção")]
    public async Task UpdateDelayAsync_WhenMacroDoesNotExist_Throws()
    {
        // Arrange
        _repository.GetByIdAsync(99).Returns((Macro?)null);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _useCase.UpdateDelayAsync(macroId: 99, stepId: 1, newDelayBeforeMs: 0));
    }

    [Fact(DisplayName = "Passo inexistente na macro lança exceção e não salva")]
    public async Task UpdateDelayAsync_WhenStepDoesNotExist_ThrowsAndDoesNotSave()
    {
        // Arrange
        var macro = new Macro { Id = 1, Name = "Teste", Steps = [] };
        _repository.GetByIdAsync(1).Returns(macro);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _useCase.UpdateDelayAsync(macroId: 1, stepId: 999, newDelayBeforeMs: 0));
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Macro>());
    }
}

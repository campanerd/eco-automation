using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;
using ECO.Domain.Macros;
using ECO.Domain.Macros.Steps;
using NSubstitute;

namespace ECO.Application.Tests.Macros.UseCases;

public class RecordMacroUseCaseTests
{
    private readonly IInputRecorder _recorder = Substitute.For<IInputRecorder>();
    private readonly IMacroRepository _repository = Substitute.For<IMacroRepository>();
    private readonly RecordMacroUseCase _useCase;

    public RecordMacroUseCaseTests()
    {
        _useCase = new RecordMacroUseCase(_recorder, _repository);
    }

    [Fact(DisplayName = "StartRecording aciona o Recorder")]
    public void StartRecording_CallsRecorderStart()
    {
        // Act
        _useCase.StartRecording();

        // Assert
        _recorder.Received(1).Start();
    }

    [Fact(DisplayName = "StopRecordingAsync monta a Macro com os passos capturados e salva")]
    public async Task StopRecordingAsync_SavesMacroWithCapturedSteps()
    {
        // Arrange
        List<MacroStep> capturedSteps = [new WaitStep { Order = 1, DelayBeforeMs = 0, DurationMs = 500 }];
        _recorder.Stop().Returns(capturedSteps);

        // Act
        var result = await _useCase.StopRecordingAsync("Minha macro");

        // Assert
        Assert.Equal("Minha macro", result.Name);
        Assert.Same(capturedSteps[0], Assert.Single(result.Steps));
        await _repository.Received(1).AddAsync(
            Arg.Is<Macro>(m => m.Name == "Minha macro" && m.Steps.Count == 1));
    }
}

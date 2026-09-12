using ECO.Application.Macros.Abstractions;
using ECO.Domain.Macros;

namespace ECO.Application.Macros.UseCases;

public class RecordMacroUseCase(IInputRecorder recorder, IMacroRepository repository)
{
    public void StartRecording() => recorder.Start();

    public async Task<Macro> StopRecordingAsync(string macroName)
    {
        var steps = recorder.Stop();

        var macro = new Macro
        {
            Name = macroName,
            Steps = [.. steps],
        };

        await repository.AddAsync(macro);

        return macro;
    }
}

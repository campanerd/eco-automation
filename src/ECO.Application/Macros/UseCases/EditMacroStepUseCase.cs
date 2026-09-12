using ECO.Application.Macros.Abstractions;

namespace ECO.Application.Macros.UseCases;

public class EditMacroStepUseCase(IMacroRepository repository)
{
    public async Task UpdateDelayAsync(int macroId, int stepId, int newDelayBeforeMs)
    {
        var macro = await repository.GetByIdAsync(macroId)
            ?? throw new InvalidOperationException($"Macro {macroId} not found.");

        var step = macro.Steps.FirstOrDefault(s => s.Id == stepId)
            ?? throw new InvalidOperationException($"Step {stepId} not found in macro {macroId}.");

        step.DelayBeforeMs = newDelayBeforeMs;

        await repository.UpdateAsync(macro);
    }
}

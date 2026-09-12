using ECO.Application.Macros.Abstractions;

namespace ECO.Application.Macros.UseCases;

public class PlayMacroUseCase(IMacroRepository repository, IInputPlayer player)
{
    public async Task PlayAsync(int macroId, CancellationToken cancellationToken = default)
    {
        var macro = await repository.GetByIdAsync(macroId)
            ?? throw new InvalidOperationException($"Macro {macroId} not found.");

        await player.PlayAsync(macro.Steps, cancellationToken);
    }
}

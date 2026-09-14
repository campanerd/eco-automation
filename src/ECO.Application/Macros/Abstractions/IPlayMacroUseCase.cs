namespace ECO.Application.Macros.Abstractions;

public interface IPlayMacroUseCase
{
    Task PlayAsync(int macroId, CancellationToken cancellationToken = default);
}

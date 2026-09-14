using ECO.Application.Macros.Abstractions;
using ECO.Domain.Macros;

namespace ECO.Application.Macros.UseCases;

public class ListMacrosUseCase(IMacroRepository repository)
{
    public Task<IReadOnlyList<Macro>> ListAsync() => repository.GetAllAsync();
}

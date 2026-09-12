using ECO.Domain.Macros;

namespace ECO.Application.Macros.Abstractions;

public interface IMacroRepository
{
    Task<Macro?> GetByIdAsync(int id);
    Task<IReadOnlyList<Macro>> GetAllAsync();
    Task AddAsync(Macro macro);
    Task UpdateAsync(Macro macro);
    Task DeleteAsync(int id);
}

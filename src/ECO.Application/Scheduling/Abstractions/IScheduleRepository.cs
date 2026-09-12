using ECO.Domain.Scheduling;

namespace ECO.Application.Scheduling.Abstractions;

public interface IScheduleRepository
{
    Task<Schedule?> GetByIdAsync(int id);
    Task<IReadOnlyList<Schedule>> GetAllAsync();
    Task AddAsync(Schedule schedule);
    Task UpdateAsync(Schedule schedule);
    Task DeleteAsync(int id);
}

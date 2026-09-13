using Dapper;

using ECO.Application.Scheduling.Abstractions;
using ECO.Domain.Scheduling;

using Microsoft.Data.Sqlite;

namespace ECO.Infrastructure.Persistence;

public class SqliteScheduleRepository(string connectionString) : IScheduleRepository
{
    private const string SelectSql =
        """
        SELECT id, macro_id AS MacroId, scheduled_at AS ScheduledAt, recurrence_rule AS RecurrenceRule, is_active AS IsActive
        FROM schedule
        """;

    public async Task<Schedule?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(connectionString);

        return await connection.QuerySingleOrDefaultAsync<Schedule>(
            $"{SelectSql} WHERE id = @Id", new { Id = id });
    }

    public async Task<IReadOnlyList<Schedule>> GetAllAsync()
    {
        using var connection = new SqliteConnection(connectionString);

        var rows = await connection.QueryAsync<Schedule>(SelectSql);

        return [.. rows];
    }

    public async Task AddAsync(Schedule schedule)
    {
        using var connection = new SqliteConnection(connectionString);

        schedule.Id = await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO schedule (macro_id, scheduled_at, recurrence_rule, is_active)
            VALUES (@MacroId, @ScheduledAt, @RecurrenceRule, @IsActive);
            SELECT last_insert_rowid();
            """,
            new { schedule.MacroId, schedule.ScheduledAt, schedule.RecurrenceRule, schedule.IsActive });
    }

    public async Task UpdateAsync(Schedule schedule)
    {
        using var connection = new SqliteConnection(connectionString);

        await connection.ExecuteAsync(
            """
            UPDATE schedule
            SET macro_id = @MacroId, scheduled_at = @ScheduledAt, recurrence_rule = @RecurrenceRule, is_active = @IsActive
            WHERE id = @Id
            """,
            schedule);
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(connectionString);

        await connection.ExecuteAsync("DELETE FROM schedule WHERE id = @Id", new { Id = id });
    }
}

using System.Data;

using Dapper;

using ECO.Application.Macros.Abstractions;
using ECO.Domain.Macros;
using ECO.Domain.Macros.Steps;

using Microsoft.Data.Sqlite;

namespace ECO.Infrastructure.Persistence;

public class SqliteMacroRepository(string connectionString) : IMacroRepository
{
    private const string StepSelectSql =
        """
        SELECT id, macro_id AS MacroId, step_order AS StepOrder, delay_before_ms AS DelayBeforeMs,
               step_type AS StepType, x, y, key_name AS KeyName, text, duration_ms AS DurationMs
        FROM macro_step
        """;

    public async Task<Macro?> GetByIdAsync(int id)
    {
        using var connection = new SqliteConnection(connectionString);

        var macroRow = await connection.QuerySingleOrDefaultAsync<MacroRow>(
            "SELECT id, name FROM macro WHERE id = @Id", new { Id = id });

        if (macroRow is null)
            return null;

        var stepRows = await connection.QueryAsync<MacroStepRow>(
            $"{StepSelectSql} WHERE macro_id = @MacroId ORDER BY step_order", new { MacroId = id });

        return MapMacro(macroRow, stepRows);
    }

    public async Task<IReadOnlyList<Macro>> GetAllAsync()
    {
        using var connection = new SqliteConnection(connectionString);

        var macroRows = await connection.QueryAsync<MacroRow>("SELECT id, name FROM macro");
        var stepRows = (await connection.QueryAsync<MacroStepRow>($"{StepSelectSql} ORDER BY macro_id, step_order")).ToList();

        return [.. macroRows.Select(macroRow => MapMacro(macroRow, stepRows.Where(s => s.MacroId == macroRow.Id)))];
    }

    public async Task AddAsync(Macro macro)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        macro.Id = await connection.ExecuteScalarAsync<int>(
            "INSERT INTO macro (name) VALUES (@Name); SELECT last_insert_rowid();",
            new { macro.Name },
            transaction);

        await InsertStepsAsync(connection, transaction, macro.Id, macro.Steps);

        transaction.Commit();
    }

    public async Task UpdateAsync(Macro macro)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync(
            "UPDATE macro SET name = @Name WHERE id = @Id", new { macro.Name, macro.Id }, transaction);

        await connection.ExecuteAsync(
            "DELETE FROM macro_step WHERE macro_id = @MacroId", new { MacroId = macro.Id }, transaction);

        await InsertStepsAsync(connection, transaction, macro.Id, macro.Steps);

        transaction.Commit();
    }

    public async Task DeleteAsync(int id)
    {
        using var connection = new SqliteConnection(connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction();

        await connection.ExecuteAsync("DELETE FROM macro_step WHERE macro_id = @Id", new { Id = id }, transaction);
        await connection.ExecuteAsync("DELETE FROM macro WHERE id = @Id", new { Id = id }, transaction);

        transaction.Commit();
    }

    private static async Task InsertStepsAsync(
        SqliteConnection connection, IDbTransaction transaction, int macroId, IReadOnlyList<MacroStep> steps)
    {
        const string sql =
            """
            INSERT INTO macro_step (macro_id, step_order, delay_before_ms, step_type, x, y, key_name, text, duration_ms)
            VALUES (@MacroId, @StepOrder, @DelayBeforeMs, @StepType, @X, @Y, @KeyName, @Text, @DurationMs)
            """;

        foreach (var step in steps)
        {
            await connection.ExecuteAsync(sql, ToParameters(step, macroId), transaction);
        }
    }

    private static Macro MapMacro(MacroRow macroRow, IEnumerable<MacroStepRow> stepRows) => new()
    {
        Id = macroRow.Id,
        Name = macroRow.Name,
        Steps = [.. stepRows.Select(MapStep)],
    };

    private static MacroStep MapStep(MacroStepRow row) => row.StepType switch
    {
        "Click" => new ClickStep { Id = row.Id, Order = row.StepOrder, DelayBeforeMs = row.DelayBeforeMs, X = row.X!.Value, Y = row.Y!.Value },
        "KeyPress" => new KeyPressStep { Id = row.Id, Order = row.StepOrder, DelayBeforeMs = row.DelayBeforeMs, Key = row.KeyName! },
        "TypeText" => new TypeTextStep { Id = row.Id, Order = row.StepOrder, DelayBeforeMs = row.DelayBeforeMs, Text = row.Text! },
        "TypeVariableText" => new TypeVariableTextStep { Id = row.Id, Order = row.StepOrder, DelayBeforeMs = row.DelayBeforeMs, Text = row.Text! },
        "Wait" => new WaitStep { Id = row.Id, Order = row.StepOrder, DelayBeforeMs = row.DelayBeforeMs, DurationMs = row.DurationMs!.Value },
        _ => throw new InvalidOperationException($"Tipo de step desconhecido: '{row.StepType}'."),
    };

    private static object ToParameters(MacroStep step, int macroId) => step switch
    {
        ClickStep s => new { MacroId = macroId, StepOrder = s.Order, s.DelayBeforeMs, StepType = "Click", s.X, s.Y, KeyName = (string?)null, Text = (string?)null, DurationMs = (int?)null },
        KeyPressStep s => new { MacroId = macroId, StepOrder = s.Order, s.DelayBeforeMs, StepType = "KeyPress", X = (int?)null, Y = (int?)null, KeyName = s.Key, Text = (string?)null, DurationMs = (int?)null },
        TypeTextStep s => new { MacroId = macroId, StepOrder = s.Order, s.DelayBeforeMs, StepType = "TypeText", X = (int?)null, Y = (int?)null, KeyName = (string?)null, s.Text, DurationMs = (int?)null },
        TypeVariableTextStep s => new { MacroId = macroId, StepOrder = s.Order, s.DelayBeforeMs, StepType = "TypeVariableText", X = (int?)null, Y = (int?)null, KeyName = (string?)null, s.Text, DurationMs = (int?)null },
        WaitStep s => new { MacroId = macroId, StepOrder = s.Order, s.DelayBeforeMs, StepType = "Wait", X = (int?)null, Y = (int?)null, KeyName = (string?)null, Text = (string?)null, s.DurationMs },
        _ => throw new InvalidOperationException($"Tipo de MacroStep desconhecido: {step.GetType().Name}."),
    };

    private sealed class MacroRow
    {
        public int Id { get; init; }
        public required string Name { get; init; }
    }

    private sealed class MacroStepRow
    {
        public int Id { get; init; }
        public int MacroId { get; init; }
        public int StepOrder { get; init; }
        public int DelayBeforeMs { get; init; }
        public required string StepType { get; init; }
        public int? X { get; init; }
        public int? Y { get; init; }
        public string? KeyName { get; init; }
        public string? Text { get; init; }
        public int? DurationMs { get; init; }
    }
}

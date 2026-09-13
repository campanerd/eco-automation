using ECO.Domain.Macros;
using ECO.Domain.Scheduling;
using ECO.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;

namespace ECO.Infrastructure.Tests.Persistence;

public class SqliteScheduleRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"eco-tests-{Guid.NewGuid()}.db");
    private readonly SqliteMacroRepository _macroRepository;
    private readonly SqliteScheduleRepository _repository;

    public SqliteScheduleRepositoryTests()
    {
        var connectionString = $"Data Source={_dbPath}";
        SqliteSchema.EnsureCreated(connectionString);
        _macroRepository = new SqliteMacroRepository(connectionString);
        _repository = new SqliteScheduleRepository(connectionString);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite mantém conexões em um pool nativo mesmo após o Dispose,
        // o que prende o arquivo. Limpar o pool antes de apagar libera o arquivo de verdade.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    // A tabela schedule tem uma foreign key pra macro — precisa existir uma Macro real
    // antes de qualquer Schedule que a referencie, senão o SQLite recusa a inserção.
    private async Task<int> CreateMacroAsync()
    {
        var macro = new Macro { Name = "Macro de teste" };
        await _macroRepository.AddAsync(macro);
        return macro.Id;
    }

    [Fact(DisplayName = "AddAsync salva um agendamento de execução única")]
    public async Task AddAsync_SavesOneTimeSchedule()
    {
        // Arrange
        var macroId = await CreateMacroAsync();
        var scheduledAt = new DateTime(2026, 9, 20, 14, 0, 0);
        var schedule = new Schedule { MacroId = macroId, ScheduledAt = scheduledAt };

        // Act
        await _repository.AddAsync(schedule);
        var loaded = await _repository.GetByIdAsync(schedule.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal(macroId, loaded.MacroId);
        Assert.Equal(scheduledAt, loaded.ScheduledAt);
        Assert.Null(loaded.RecurrenceRule);
        Assert.True(loaded.IsActive);
    }

    [Fact(DisplayName = "AddAsync salva um agendamento recorrente")]
    public async Task AddAsync_SavesRecurringSchedule()
    {
        // Arrange
        var macroId = await CreateMacroAsync();
        var schedule = new Schedule { MacroId = macroId, RecurrenceRule = "0 9 * * MON" };

        // Act
        await _repository.AddAsync(schedule);
        var loaded = await _repository.GetByIdAsync(schedule.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("0 9 * * MON", loaded.RecurrenceRule);
        Assert.Null(loaded.ScheduledAt);
    }

    [Fact(DisplayName = "UpdateAsync altera IsActive")]
    public async Task UpdateAsync_ChangesIsActive()
    {
        // Arrange
        var macroId = await CreateMacroAsync();
        var schedule = new Schedule { MacroId = macroId, RecurrenceRule = "* * * * *" };
        await _repository.AddAsync(schedule);
        schedule.IsActive = false;

        // Act
        await _repository.UpdateAsync(schedule);
        var loaded = await _repository.GetByIdAsync(schedule.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.False(loaded.IsActive);
    }

    [Fact(DisplayName = "DeleteAsync remove o agendamento")]
    public async Task DeleteAsync_RemovesSchedule()
    {
        // Arrange
        var macroId = await CreateMacroAsync();
        var schedule = new Schedule { MacroId = macroId, RecurrenceRule = "* * * * *" };
        await _repository.AddAsync(schedule);

        // Act
        await _repository.DeleteAsync(schedule.Id);
        var loaded = await _repository.GetByIdAsync(schedule.Id);

        // Assert
        Assert.Null(loaded);
    }

    [Fact(DisplayName = "GetAllAsync devolve todos os agendamentos salvos")]
    public async Task GetAllAsync_ReturnsAllSchedules()
    {
        // Arrange
        var macroId = await CreateMacroAsync();
        await _repository.AddAsync(new Schedule { MacroId = macroId, RecurrenceRule = "* * * * *" });
        await _repository.AddAsync(new Schedule { MacroId = macroId, ScheduledAt = DateTime.UtcNow });

        // Act
        var all = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, all.Count);
    }
}

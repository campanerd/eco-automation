using ECO.Domain.Macros;
using ECO.Domain.Macros.Steps;
using ECO.Infrastructure.Persistence;

using Microsoft.Data.Sqlite;

namespace ECO.Infrastructure.Tests.Persistence;

public class SqliteMacroRepositoryTests : IDisposable
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"eco-tests-{Guid.NewGuid()}.db");
    private readonly SqliteMacroRepository _repository;

    public SqliteMacroRepositoryTests()
    {
        var connectionString = $"Data Source={_dbPath}";
        SqliteSchema.EnsureCreated(connectionString);
        _repository = new SqliteMacroRepository(connectionString);
    }

    public void Dispose()
    {
        // Microsoft.Data.Sqlite mantém conexões em um pool nativo mesmo após o Dispose,
        // o que prende o arquivo. Limpar o pool antes de apagar libera o arquivo de verdade.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }

    [Fact(DisplayName = "AddAsync salva a Macro e todos os tipos de MacroStep, preservando os dados de cada um")]
    public async Task AddAsync_SavesAllStepTypesWithTheirData()
    {
        // Arrange
        var macro = new Macro
        {
            Name = "Preencher relatório",
            Steps =
            [
                new ClickStep { Order = 1, DelayBeforeMs = 0, X = 100, Y = 50 },
                new KeyPressStep { Order = 2, DelayBeforeMs = 100, Key = "Enter" },
                new TypeTextStep { Order = 3, DelayBeforeMs = 200, Text = "google.com" },
                new TypeVariableTextStep { Order = 4, DelayBeforeMs = 0, Text = "\"\"\"dia_atual\"\"\"" },
                new WaitStep { Order = 5, DelayBeforeMs = 0, DurationMs = 2000 },
            ],
        };

        // Act
        await _repository.AddAsync(macro);
        var loaded = await _repository.GetByIdAsync(macro.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.NotEqual(0, macro.Id);
        Assert.Equal("Preencher relatório", loaded.Name);
        Assert.Equal(5, loaded.Steps.Count);

        var click = Assert.IsType<ClickStep>(loaded.Steps[0]);
        Assert.Equal(100, click.X);
        Assert.Equal(50, click.Y);

        var keyPress = Assert.IsType<KeyPressStep>(loaded.Steps[1]);
        Assert.Equal("Enter", keyPress.Key);

        var typeText = Assert.IsType<TypeTextStep>(loaded.Steps[2]);
        Assert.Equal("google.com", typeText.Text);

        var typeVariableText = Assert.IsType<TypeVariableTextStep>(loaded.Steps[3]);
        Assert.Equal("\"\"\"dia_atual\"\"\"", typeVariableText.Text);

        var wait = Assert.IsType<WaitStep>(loaded.Steps[4]);
        Assert.Equal(2000, wait.DurationMs);
    }

    [Fact(DisplayName = "GetByIdAsync devolve null quando a Macro não existe")]
    public async Task GetByIdAsync_WhenMacroDoesNotExist_ReturnsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);
    }

    [Fact(DisplayName = "Steps saem na ordem correta (Order), mesmo inseridos fora de ordem")]
    public async Task GetByIdAsync_ReturnsStepsInOrder()
    {
        // Arrange
        var macro = new Macro
        {
            Name = "Teste de ordem",
            Steps =
            [
                new WaitStep { Order = 3, DelayBeforeMs = 0, DurationMs = 100 },
                new WaitStep { Order = 1, DelayBeforeMs = 0, DurationMs = 200 },
                new WaitStep { Order = 2, DelayBeforeMs = 0, DurationMs = 300 },
            ],
        };
        await _repository.AddAsync(macro);

        // Act
        var loaded = await _repository.GetByIdAsync(macro.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal([1, 2, 3], loaded.Steps.Select(s => s.Order));
    }

    [Fact(DisplayName = "UpdateAsync substitui os Steps antigos pelos novos")]
    public async Task UpdateAsync_ReplacesSteps()
    {
        // Arrange
        var macro = new Macro
        {
            Name = "Original",
            Steps = [new WaitStep { Order = 1, DelayBeforeMs = 5000, DurationMs = 1000 }],
        };
        await _repository.AddAsync(macro);

        macro.Name = "Editado";
        macro.Steps = [new ClickStep { Order = 1, DelayBeforeMs = 100, X = 1, Y = 2 }];

        // Act
        await _repository.UpdateAsync(macro);
        var loaded = await _repository.GetByIdAsync(macro.Id);

        // Assert
        Assert.NotNull(loaded);
        Assert.Equal("Editado", loaded.Name);
        Assert.IsType<ClickStep>(Assert.Single(loaded.Steps));
    }

    [Fact(DisplayName = "DeleteAsync remove a Macro e os Steps junto")]
    public async Task DeleteAsync_RemovesMacroAndSteps()
    {
        // Arrange
        var macro = new Macro
        {
            Name = "Pra apagar",
            Steps = [new WaitStep { Order = 1, DelayBeforeMs = 0, DurationMs = 100 }],
        };
        await _repository.AddAsync(macro);

        // Act
        await _repository.DeleteAsync(macro.Id);
        var loaded = await _repository.GetByIdAsync(macro.Id);

        // Assert
        Assert.Null(loaded);
    }

    [Fact(DisplayName = "GetAllAsync devolve todas as macros salvas, cada uma com seus próprios steps")]
    public async Task GetAllAsync_ReturnsAllMacrosWithTheirOwnSteps()
    {
        // Arrange
        var macroA = new Macro { Name = "A", Steps = [new WaitStep { Order = 1, DelayBeforeMs = 0, DurationMs = 100 }] };
        var macroB = new Macro { Name = "B", Steps = [new ClickStep { Order = 1, DelayBeforeMs = 0, X = 1, Y = 1 }] };
        await _repository.AddAsync(macroA);
        await _repository.AddAsync(macroB);

        // Act
        var all = await _repository.GetAllAsync();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Single(all.Single(m => m.Name == "A").Steps);
        Assert.IsType<ClickStep>(Assert.Single(all.Single(m => m.Name == "B").Steps));
    }
}

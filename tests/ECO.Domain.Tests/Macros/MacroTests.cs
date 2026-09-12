using ECO.Domain.Macros;
using ECO.Domain.Macros.Steps;

namespace ECO.Domain.Tests.Macros;

public class MacroTests
{
    [Fact(DisplayName = "Uma nova Macro começa com a lista de Steps vazia, nunca nula")]
    public void NewMacro_StartsWithEmptySteps()
    {
        // Act
        var macro = new Macro { Name = "Minha macro" };

        // Assert
        Assert.NotNull(macro.Steps);
        Assert.Empty(macro.Steps);
    }

    [Fact(DisplayName = "Steps aceita diferentes tipos de MacroStep na mesma lista")]
    public void Steps_CanHoldDifferentStepTypes()
    {
        // Arrange
        var macro = new Macro
        {
            Name = "Preencher relatório",
            Steps =
            [
                new ClickStep { Order = 1, DelayBeforeMs = 0, X = 100, Y = 50 },
                new TypeTextStep { Order = 2, DelayBeforeMs = 200, Text = "google.com" },
                new KeyPressStep { Order = 3, DelayBeforeMs = 100, Key = "Enter" },
                new WaitStep { Order = 4, DelayBeforeMs = 0, DurationMs = 2000 },
            ],
        };

        // Assert
        Assert.Equal(4, macro.Steps.Count);
        Assert.IsType<ClickStep>(macro.Steps[0]);
        Assert.IsType<TypeTextStep>(macro.Steps[1]);
        Assert.IsType<KeyPressStep>(macro.Steps[2]);
        Assert.IsType<WaitStep>(macro.Steps[3]);
    }

    [Fact(DisplayName = "MacroStep herda Id de BaseEntity")]
    public void MacroStep_InheritsIdFromBaseEntity()
    {
        // Arrange
        var step = new WaitStep { Order = 1, DelayBeforeMs = 0, DurationMs = 1000 };

        // Act
        step.Id = 42;

        // Assert
        Assert.Equal(42, step.Id);
    }
}

using ECO.Domain.Macros.Steps;
using ECO.Infrastructure.Input;

namespace ECO.Infrastructure.Tests.Input;

public class WindowsInputPlayerTests
{
    private readonly WindowsInputPlayer _player = new();

    // Este é o único teste automatizado seguro pro WindowsInputPlayer: valida a checagem de
    // tecla desconhecida, que lança a exceção ANTES de qualquer SendInput real ser chamado.
    // Testar clique/digitação de verdade exigiria mexer no mouse/teclado real da máquina que
    // roda os testes — isso fica pra verificação manual, não para a suíte automatizada.
    [Fact(DisplayName = "PlayAsync lança exceção para tecla nomeada desconhecida, sem tocar no mouse/teclado real")]
    public async Task PlayAsync_WithUnsupportedNamedKey_Throws()
    {
        // Arrange
        MacroStep[] steps = [new KeyPressStep { Order = 1, DelayBeforeMs = 0, Key = "TeclaQueNaoExiste" }];

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => _player.PlayAsync(steps));
    }
}

using System.Runtime.InteropServices;

using ECO.Application.Macros.Abstractions;
using ECO.Domain.Macros.Steps;

using static ECO.Infrastructure.Input.NativeMethods;

namespace ECO.Infrastructure.Input;

public class WindowsInputPlayer : IInputPlayer
{
    private const int PauseCheckIntervalMs = 100;

    // Sem essa pausa, eventos de teclado saem rápido demais e o programa de destino descarta
    // parte deles silenciosamente (texto cortado no Bloco de Notas, Ctrl+V ignorado num navegador) —
    // SendInput não avisa quando isso acontece.
    private const int KeyEventDelayMs = 15;

    private volatile bool _isPaused;

    public async Task PlayAsync(IReadOnlyList<MacroStep> steps, CancellationToken cancellationToken = default)
    {
        foreach (var step in steps)
        {
            await DelayRespectingPauseAsync(step.DelayBeforeMs, cancellationToken);

            switch (step)
            {
                case ClickStep click:
                    MoveCursor(click.X, click.Y);
                    ClickLeftButton();
                    break;

                case KeyPressStep keyPress:
                    await PressNamedKeyAsync(keyPress.Key, cancellationToken);
                    break;

                case TypeTextStep typeText:
                    await TypeTextAsync(typeText.Text, cancellationToken);
                    break;

                case TypeVariableTextStep typeVariableText:
                    // A substituição da variável (ex.: """dia_atual""") ainda não foi implementada —
                    // isso é responsabilidade do Application, antes de chegar até aqui. Por enquanto,
                    // o texto é digitado literalmente, igual ao TypeTextStep.
                    await TypeTextAsync(typeVariableText.Text, cancellationToken);
                    break;

                case WaitStep wait:
                    await DelayRespectingPauseAsync(wait.DurationMs, cancellationToken);
                    break;

                default:
                    throw new InvalidOperationException($"Tipo de MacroStep desconhecido: {step.GetType().Name}.");
            }
        }
    }

    public void Pause() => _isPaused = true;

    public void Resume() => _isPaused = false;

    private async Task DelayRespectingPauseAsync(int milliseconds, CancellationToken cancellationToken)
    {
        var remaining = milliseconds;

        while (remaining > 0)
        {
            while (_isPaused)
                await Task.Delay(PauseCheckIntervalMs, cancellationToken);

            var chunk = Math.Min(PauseCheckIntervalMs, remaining);
            await Task.Delay(chunk, cancellationToken);
            remaining -= chunk;
        }
    }

    private static void MoveCursor(int x, int y)
    {
        var screenWidth = GetSystemMetrics(SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SM_CYSCREEN);

        var input = new INPUT
        {
            type = INPUT_MOUSE,
            U = new InputUnion
            {
                mi = new MOUSEINPUT
                {
                    dx = x * 65536 / screenWidth,
                    dy = y * 65536 / screenHeight,
                    dwFlags = MOUSEEVENTF_MOVE | MOUSEEVENTF_ABSOLUTE,
                },
            },
        };

        SendInput(1, [input], Marshal.SizeOf<INPUT>());
    }

    private static void ClickLeftButton()
    {
        INPUT[] inputs =
        [
            new INPUT
            {
                type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } }
            },
            new INPUT
            {
                type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } }
            },
        ];

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    // Suporta tanto uma tecla nomeada sozinha ("Enter") quanto um atalho ("Ctrl+S", "Ctrl+Shift+Z"):
    // pressiona cada parte em ordem e solta na ordem inversa, como um usuário faria. Um pequeno
    // delay entre cada evento evita que o atalho seja perdido no app de destino (ex.: Ctrl+V num navegador).
    private static async Task PressNamedKeyAsync(string key, CancellationToken cancellationToken)
    {
        var codes = key.Split('+').Select(ResolveVirtualKey).ToArray();

        foreach (var code in codes)
        {
            SendInput(1, [KeyInput(code, isKeyUp: false)], Marshal.SizeOf<INPUT>());
            await Task.Delay(KeyEventDelayMs, cancellationToken);
        }

        foreach (var code in codes.Reverse())
        {
            SendInput(1, [KeyInput(code, isKeyUp: true)], Marshal.SizeOf<INPUT>());
            await Task.Delay(KeyEventDelayMs, cancellationToken);
        }
    }

    private static ushort ResolveVirtualKey(string name)
    {
        if (name.Equals("Ctrl", StringComparison.OrdinalIgnoreCase))
            return VK_CONTROL;

        if (name.Equals("Shift", StringComparison.OrdinalIgnoreCase))
            return VK_SHIFT;

        if (name.Equals("Alt", StringComparison.OrdinalIgnoreCase))
            return VK_MENU;

        if (NamedVirtualKeys.ByName.TryGetValue(name, out var code))
            return code;

        // VK_0-VK_9 e VK_A-VK_Z coincidem com os códigos ASCII de '0'-'9' e 'A'-'Z'.
        if (name.Length == 1)
            return char.ToUpperInvariant(name[0]);

        throw new NotSupportedException($"Tecla não suportada: '{name}'.");
    }

    // Setas, Home/End, Delete e as teclas Windows são "extended keys" — sem essa flag, o Windows
    // as trata como as equivalentes do teclado numérico, e Shift+seta não estende seleção nenhuma.
    private static readonly HashSet<ushort> ExtendedKeys =
    [
        NamedVirtualKeys.ByName["Up"], NamedVirtualKeys.ByName["Down"],
        NamedVirtualKeys.ByName["Left"], NamedVirtualKeys.ByName["Right"],
        NamedVirtualKeys.ByName["Home"], NamedVirtualKeys.ByName["End"],
        NamedVirtualKeys.ByName["Delete"],
        NamedVirtualKeys.ByName["WindowsL"], NamedVirtualKeys.ByName["WindowsR"],
    ];

    private static INPUT KeyInput(ushort virtualKeyCode, bool isKeyUp)
    {
        var flags = isKeyUp ? KEYEVENTF_KEYUP : 0;
        if (ExtendedKeys.Contains(virtualKeyCode))
            flags |= KEYEVENTF_EXTENDEDKEY;

        return new INPUT
        {
            type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKeyCode, dwFlags = flags } }
        };
    }

    private static async Task TypeTextAsync(string text, CancellationToken cancellationToken)
    {
        foreach (var character in text)
        {
            INPUT[] inputs =
            [
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE } }
                },
                new INPUT
                {
                    type = INPUT_KEYBOARD,
                    U = new InputUnion
                    {
                        ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP }
                    }
                },
            ];

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());

            await Task.Delay(KeyEventDelayMs, cancellationToken);
        }
    }
}

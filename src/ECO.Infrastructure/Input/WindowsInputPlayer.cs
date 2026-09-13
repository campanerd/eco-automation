using System.Runtime.InteropServices;

using ECO.Application.Macros.Abstractions;
using ECO.Domain.Macros.Steps;

using static ECO.Infrastructure.Input.NativeMethods;

namespace ECO.Infrastructure.Input;

public class WindowsInputPlayer : IInputPlayer
{
    private const int PauseCheckIntervalMs = 100;

    private static readonly Dictionary<string, ushort> NamedKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Enter"] = 0x0D,
        ["Tab"] = 0x09,
        ["Escape"] = 0x1B,
        ["Backspace"] = 0x08,
        ["Delete"] = 0x2E,
        ["Space"] = 0x20,
        ["Up"] = 0x26,
        ["Down"] = 0x28,
        ["Left"] = 0x25,
        ["Right"] = 0x27,
        ["Home"] = 0x24,
        ["End"] = 0x23,
    };

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
                    PressNamedKey(keyPress.Key);
                    break;

                case TypeTextStep typeText:
                    TypeText(typeText.Text);
                    break;

                case TypeVariableTextStep typeVariableText:
                    // A substituição da variável (ex.: """dia_atual""") ainda não foi implementada —
                    // isso é responsabilidade do Application, antes de chegar até aqui. Por enquanto,
                    // o texto é digitado literalmente, igual ao TypeTextStep.
                    TypeText(typeVariableText.Text);
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
            new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTDOWN } } },
            new INPUT { type = INPUT_MOUSE, U = new InputUnion { mi = new MOUSEINPUT { dwFlags = MOUSEEVENTF_LEFTUP } } },
        ];

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void PressNamedKey(string key)
    {
        if (!NamedKeys.TryGetValue(key, out var virtualKeyCode))
            throw new NotSupportedException($"Tecla não suportada: '{key}'.");

        INPUT[] inputs =
        [
            new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKeyCode } } },
            new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wVk = virtualKeyCode, dwFlags = KEYEVENTF_KEYUP } } },
        ];

        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }

    private static void TypeText(string text)
    {
        foreach (var character in text)
        {
            INPUT[] inputs =
            [
                new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE } } },
                new INPUT { type = INPUT_KEYBOARD, U = new InputUnion { ki = new KEYBDINPUT { wScan = character, dwFlags = KEYEVENTF_UNICODE | KEYEVENTF_KEYUP } } },
            ];

            SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
        }
    }
}

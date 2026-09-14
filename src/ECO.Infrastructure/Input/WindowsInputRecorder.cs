using System.Runtime.InteropServices;
using System.Text;

using ECO.Application.Macros.Abstractions;
using ECO.Domain.Macros.Steps;

using static ECO.Infrastructure.Input.NativeMethods;

namespace ECO.Infrastructure.Input;

public class WindowsInputRecorder : IInputRecorder
{
    private readonly List<MacroStep> _steps = [];
    private readonly StringBuilder _textBuffer = new();

    private HookProc? _keyboardProc;
    private HookProc? _mouseProc;
    private nint _keyboardHookHandle;
    private nint _mouseHookHandle;
    private long _lastEventTimestamp;

    public void Start()
    {
        _steps.Clear();
        _textBuffer.Clear();
        _lastEventTimestamp = Environment.TickCount64;

        // As referências aos delegates precisam ficar guardadas em campos — se o coletor de lixo
        // recolher o delegate enquanto o hook nativo ainda está instalado, o callback vira um
        // ponteiro inválido e o processo quebra na próxima tecla/clique do usuário.
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        _keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc, GetModuleHandle(null), 0);
        _mouseHookHandle = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc, GetModuleHandle(null), 0);
    }

    public IReadOnlyList<MacroStep> Stop()
    {
        if (_keyboardHookHandle != 0)
        {
            UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = 0;
        }

        if (_mouseHookHandle != 0)
        {
            UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = 0;
        }

        FlushTextBuffer();

        return [.. _steps];
    }

    private nint KeyboardHookCallback(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN))
        {
            var data = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            HandleKeyDown(data.vkCode);
        }

        return CallNextHookEx(0, code, wParam, lParam);
    }

    private nint MouseHookCallback(int code, nint wParam, nint lParam)
    {
        if (code >= 0 && wParam == WM_LBUTTONDOWN)
        {
            var data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            FlushTextBuffer();
            AddStep(new ClickStep { X = data.pt.x, Y = data.pt.y });
        }

        return CallNextHookEx(0, code, wParam, lParam);
    }

    private void HandleKeyDown(uint virtualKeyCode)
    {
        // Teclas nomeadas (Enter, Tab, setas...) não têm caractere visível — viram um
        // KeyPressStep próprio, e primeiro fecham qualquer texto que estivesse sendo acumulado.
        if (NamedVirtualKeys.ByCode.TryGetValue((ushort)virtualKeyCode, out var name))
        {
            FlushTextBuffer();
            AddStep(new KeyPressStep { Key = name });
            return;
        }

        // O resto (letras, números, pontuação, espaço) vai se acumulando num buffer de texto,
        // em vez de virar um KeyPressStep por tecla — só quando uma tecla nomeada aparece,
        // ou a gravação para, esse buffer inteiro vira um único TypeTextStep.
        var character = TranslateToChar(virtualKeyCode);
        if (character is not null && !char.IsControl(character.Value))
            _textBuffer.Append(character.Value);
    }

    private const int VK_CONTROL = 0x11;
    private const int VK_MENU = 0x12; // Alt

    private static char? TranslateToChar(uint virtualKeyCode)
    {
        // Ctrl/Alt pressionados = atalho, nunca texto (GetAsyncKeyState, não GetKeyboardState,
        // que fica desatualizado dentro de um hook de baixo nível).
        var isControlDown = GetAsyncKeyState(VK_CONTROL) < 0;
        var isAltDown = GetAsyncKeyState(VK_MENU) < 0;

        if (isControlDown || isAltDown)
            return null;

        var keyboardState = new byte[256];
        if (!GetKeyboardState(keyboardState))
            return null;

        var scanCode = MapVirtualKey(virtualKeyCode, MAPVK_VK_TO_VSC);
        var buffer = new StringBuilder(2);
        var result = ToUnicode(virtualKeyCode, scanCode, keyboardState, buffer, buffer.Capacity, 0);

        // ToUnicode devolve 1 quando a tecla (já considerando Shift/CapsLock) vira exatamente
        // um caractere. Valores diferentes (0 = sem caractere, negativo = tecla morta/acento,
        // >1 = combinação) não são tratados aqui e a tecla é simplesmente ignorada.
        return result == 1 ? buffer[0] : null;
    }

    private void FlushTextBuffer()
    {
        if (_textBuffer.Length == 0)
            return;

        AddStep(new TypeTextStep { Text = _textBuffer.ToString() });
        _textBuffer.Clear();
    }

    private void AddStep(MacroStep step)
    {
        var now = Environment.TickCount64;

        step.Order = _steps.Count + 1;
        step.DelayBeforeMs = (int)(now - _lastEventTimestamp);

        _lastEventTimestamp = now;
        _steps.Add(step);
    }
}

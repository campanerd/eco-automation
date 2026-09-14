using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace ECO.Presentation.WPF;

// Atalho global do Windows: dispara mesmo com o app minimizado ou sem foco. É o que permite
// parar a gravação sem clicar em nada — um clique seria capturado pelo Recorder como um passo.
public sealed class GlobalHotkey : IDisposable
{
    public const uint ModAlt = 0x0001;
    public const uint ModControl = 0x0002;

    private const int WM_HOTKEY = 0x0312;

    private readonly int _id;
    private readonly nint _handle;
    private readonly HwndSource _source;
    private readonly Action _onPressed;

    public GlobalHotkey(Window window, int id, uint modifiers, uint virtualKey, Action onPressed)
    {
        _id = id;
        _onPressed = onPressed;

        _handle = new WindowInteropHelper(window).EnsureHandle();
        _source = HwndSource.FromHwnd(_handle)!;
        _source.AddHook(OnWindowMessage);

        // Falha quando outro programa já registrou a mesma combinação — quem chama precisa
        // saber disso, senão o usuário aperta o atalho, nada acontece e não há explicação.
        IsRegistered = RegisterHotKey(_handle, _id, modifiers, virtualKey);
    }

    public bool IsRegistered { get; }

    public void Dispose()
    {
        _source.RemoveHook(OnWindowMessage);
        UnregisterHotKey(_handle, _id);
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool RegisterHotKey(nint hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnregisterHotKey(nint hWnd, int id);

    private nint OnWindowMessage(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == WM_HOTKEY && wParam.ToInt32() == _id)
        {
            _onPressed();
            handled = true;
        }

        return nint.Zero;
    }
}

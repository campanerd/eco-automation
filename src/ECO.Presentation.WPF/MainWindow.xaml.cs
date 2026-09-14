using System.ComponentModel;
using System.Windows;

using ECO.Presentation.WPF.ViewModels;

namespace ECO.Presentation.WPF;

public partial class MainWindow : Window
{
    private const uint VirtualKeyR = 0x52;
    private const uint VirtualKeyP = 0x50;

    private readonly MainWindowViewModel _viewModel;

    private GlobalHotkey? _stopRecordingHotkey;
    private GlobalHotkey? _togglePauseHotkey;

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Loaded += async (_, _) =>
        {
            _stopRecordingHotkey = new GlobalHotkey(
                this,
                id: 1,
                GlobalHotkey.ModControl | GlobalHotkey.ModAlt,
                VirtualKeyR,
                () => _viewModel.StopRecordingCommand.Execute(null));

            _togglePauseHotkey = new GlobalHotkey(
                this,
                id: 2,
                GlobalHotkey.ModControl | GlobalHotkey.ModAlt,
                VirtualKeyP,
                _viewModel.TogglePause);

            if (!_stopRecordingHotkey.IsRegistered)
                viewModel.StatusMessage =
                    "Atenção: não consegui registrar Ctrl+Alt+R (outro programa já usa esse atalho).";
            else if (!_togglePauseHotkey.IsRegistered)
                viewModel.StatusMessage =
                    "Atenção: não consegui registrar Ctrl+Alt+P (outro programa já usa esse atalho).";

            await viewModel.LoadMacrosCommand.ExecuteAsync(null);
        };

        Closed += (_, _) =>
        {
            _stopRecordingHotkey?.Dispose();
            _togglePauseHotkey?.Dispose();
        };
    }

    // A janela sai da frente durante gravação e reprodução: se ficasse visível, um clique nela
    // entraria na macro como passo gravado, e na reprodução ela taparia o programa alvo.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is not (nameof(MainWindowViewModel.IsRecording) or nameof(MainWindowViewModel.IsPlaying)))
            return;

        if (_viewModel.IsRecording || _viewModel.IsPlaying)
        {
            WindowState = WindowState.Minimized;
            return;
        }

        WindowState = WindowState.Normal;
        Activate();
    }
}

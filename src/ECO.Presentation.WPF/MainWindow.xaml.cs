using System.ComponentModel;
using System.Windows;

using ECO.Presentation.WPF.ViewModels;

namespace ECO.Presentation.WPF;

public partial class MainWindow : Window
{
    private const uint VirtualKeyR = 0x52;

    private readonly MainWindowViewModel _viewModel;

    private GlobalHotkey? _stopRecordingHotkey;

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

            if (!_stopRecordingHotkey.IsRegistered)
                viewModel.StatusMessage = "Atenção: não consegui registrar Ctrl+Alt+R (outro programa já usa esse atalho).";

            await viewModel.LoadMacrosCommand.ExecuteAsync(null);
        };

        Closed += (_, _) => _stopRecordingHotkey?.Dispose();
    }

    // A janela some da tela durante a gravação: se ela ficasse visível, qualquer clique nela
    // (inclusive num botão de parar) entraria na macro como um passo gravado.
    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(MainWindowViewModel.IsRecording))
            return;

        if (_viewModel.IsRecording)
        {
            WindowState = WindowState.Minimized;
            return;
        }

        WindowState = WindowState.Normal;
        Activate();
    }
}

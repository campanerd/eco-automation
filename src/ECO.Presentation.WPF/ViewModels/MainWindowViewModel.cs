using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ECO.Application.Macros.UseCases;

namespace ECO.Presentation.WPF.ViewModels;

public partial class MainWindowViewModel(
    ListMacrosUseCase listMacrosUseCase,
    RecordMacroUseCase recordMacroUseCase) : ObservableObject
{
    [ObservableProperty]
    private string _newMacroName = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    private bool _isRecording;

    [ObservableProperty]
    private string _statusMessage = "";

    public ObservableCollection<MacroListItem> Macros { get; } = [];

    [RelayCommand]
    private async Task LoadMacrosAsync()
    {
        var macros = await listMacrosUseCase.ListAsync();

        Macros.Clear();

        foreach (var macro in macros)
            Macros.Add(new MacroListItem(macro.Id, macro.Name, macro.Steps.Count));
    }

    [RelayCommand(CanExecute = nameof(CanStartRecording))]
    private void StartRecording()
    {
        if (string.IsNullOrWhiteSpace(NewMacroName))
        {
            StatusMessage = "Dê um nome para a macro antes de gravar.";
            return;
        }

        recordMacroUseCase.StartRecording();

        IsRecording = true;
        StatusMessage = "Gravando... aperte Ctrl+Alt+R para parar.";
    }

    private bool CanStartRecording() => !IsRecording;

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        if (!IsRecording)
            return;

        var macro = await recordMacroUseCase.StopRecordingAsync(NewMacroName);

        IsRecording = false;
        StatusMessage = $"Macro \"{macro.Name}\" salva com {macro.Steps.Count} passos.";
        NewMacroName = "";

        await LoadMacrosAsync();
    }
}

public record MacroListItem(int Id, string Name, int StepCount);

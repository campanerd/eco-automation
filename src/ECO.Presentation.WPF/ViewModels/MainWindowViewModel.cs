using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ECO.Application.Macros.Abstractions;
using ECO.Application.Macros.UseCases;

namespace ECO.Presentation.WPF.ViewModels;

public partial class MainWindowViewModel(
    ListMacrosUseCase listMacrosUseCase,
    RecordMacroUseCase recordMacroUseCase,
    IPlayMacroUseCase playMacroUseCase,
    PauseResumeMacroUseCase pauseResumeMacroUseCase) : ObservableObject
{
    [ObservableProperty] private string _newMacroName = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayMacroCommand))]
    private bool _isRecording;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartRecordingCommand))]
    [NotifyCanExecuteChangedFor(nameof(PlayMacroCommand))]
    private bool _isPlaying;

    [ObservableProperty] private bool _isPaused;

    [ObservableProperty] [NotifyCanExecuteChangedFor(nameof(PlayMacroCommand))]
    private MacroListItem? _selectedMacro;

    [ObservableProperty] private string _statusMessage = "";

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

    private bool CanStartRecording() => !IsRecording && !IsPlaying;

    [RelayCommand]
    private async Task StopRecordingAsync()
    {
        if (!IsRecording)
            return;

        try
        {
            var macro = await recordMacroUseCase.StopRecordingAsync(NewMacroName);

            StatusMessage = $"Macro \"{macro.Name}\" salva com {macro.Steps.Count} passos.";
            NewMacroName = "";

            await LoadMacrosAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao salvar a gravação: {ex.Message}";
        }
        finally
        {
            IsRecording = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanPlayMacro))]
    private async Task PlayMacroAsync()
    {
        if (SelectedMacro is null)
            return;

        var macroName = SelectedMacro.Name;

        IsPlaying = true;
        IsPaused = false;
        StatusMessage = $"Reproduzindo \"{macroName}\"... Ctrl+Alt+P pausa e retoma.";

        try
        {
            await playMacroUseCase.PlayAsync(SelectedMacro.Id);

            StatusMessage = $"Macro \"{macroName}\" concluída.";
        }
        catch (Exception ex)
        {
            // A documentação do projeto define que erro de reprodução vira aviso no app,
            // sem retry automático nem pular passo — por isso o catch amplo aqui.
            StatusMessage = $"Erro ao reproduzir \"{macroName}\": {ex.Message}";
        }
        finally
        {
            IsPlaying = false;
            IsPaused = false;
        }
    }

    private bool CanPlayMacro() => SelectedMacro is not null && !IsPlaying && !IsRecording;

    // Chamado pelo atalho global Ctrl+Alt+P enquanto uma macro está rodando.
    public void TogglePause()
    {
        if (!IsPlaying)
            return;

        if (IsPaused)
        {
            pauseResumeMacroUseCase.Resume();
            IsPaused = false;
            StatusMessage = "Retomando a reprodução...";
            return;
        }

        pauseResumeMacroUseCase.Pause();
        IsPaused = true;
        StatusMessage = "Pausado — aperte Ctrl+Alt+P para retomar.";
    }
}

public record MacroListItem(int Id, string Name, int StepCount);

using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using ECO.Application.Macros.UseCases;

namespace ECO.Presentation.WPF.ViewModels;

public partial class MainWindowViewModel(ListMacrosUseCase listMacrosUseCase) : ObservableObject
{
    public ObservableCollection<MacroListItem> Macros { get; } = [];

    [RelayCommand]
    private async Task LoadMacrosAsync()
    {
        var macros = await listMacrosUseCase.ListAsync();

        Macros.Clear();

        foreach (var macro in macros)
            Macros.Add(new MacroListItem(macro.Id, macro.Name, macro.Steps.Count));
    }
}

public record MacroListItem(int Id, string Name, int StepCount);

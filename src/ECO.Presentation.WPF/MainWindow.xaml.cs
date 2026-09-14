using System.Windows;

using ECO.Presentation.WPF.ViewModels;

namespace ECO.Presentation.WPF;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();

        DataContext = viewModel;

        Loaded += async (_, _) => await viewModel.LoadMacrosCommand.ExecuteAsync(null);
    }
}

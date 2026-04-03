using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.ViewModels;

namespace Purrfolio.App.Views;

public sealed partial class ManualEntryPage : Page
{
    public ManualEntryViewModel ViewModel { get; }

    public ManualEntryPage(ManualEntryViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.ViewModels;

namespace Purrfolio.App.Views;

public sealed partial class ManualEntryPage : Page
{
    private bool _isInitialized;

    public ManualEntryViewModel ViewModel { get; }

    public ManualEntryPage(ManualEntryViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private async void ManualEntryPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await ViewModel.LoadRecordsAsync();
    }
}

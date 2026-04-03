using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.ViewModels;

namespace Purrfolio.App.Views;

public sealed partial class HomePage : Page
{
    private bool _isInitialized;

    public AssetViewModel ViewModel { get; }

    public HomePage(AssetViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private async void HomePage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await ViewModel.LoadAsync();
    }
}

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.Interfaces;
using Purrfolio.App.ViewModels;

namespace Purrfolio.App.Views;

public sealed partial class FixedIncomePage : Page, IConnectedAnimationPage
{
    private bool _isInitialized;

    public FixedIncomeViewModel ViewModel { get; }
    public UIElement AnimationTarget => PageTitleText;

    public FixedIncomePage(FixedIncomeViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private async void FixedIncomePage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await ViewModel.LoadAsync();
    }
}

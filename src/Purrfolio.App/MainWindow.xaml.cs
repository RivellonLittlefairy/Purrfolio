using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Purrfolio.App.Views;

namespace Purrfolio.App;

public sealed partial class MainWindow : Window
{
    private readonly HomePage _homePage;
    private readonly ManualEntryPage _manualEntryPage;
    private readonly FixedIncomePage _fixedIncomePage;
    private readonly ProjectionPage _projectionPage;

    public MainWindow(
        HomePage homePage,
        ManualEntryPage manualEntryPage,
        FixedIncomePage fixedIncomePage,
        ProjectionPage projectionPage)
    {
        InitializeComponent();

        _homePage = homePage;
        _manualEntryPage = manualEntryPage;
        _fixedIncomePage = fixedIncomePage;
        _projectionPage = projectionPage;

        TryApplyMicaBackdrop();

        RootNavigationView.SelectedItem = RootNavigationView.MenuItems[0];
        RootFrame.Content = _homePage;
    }

    private void TryApplyMicaBackdrop()
    {
        SystemBackdrop = new MicaBackdrop
        {
            Kind = MicaKind.BaseAlt
        };
    }

    private void RootNavigationView_OnSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItemContainer?.Tag is not string tag)
        {
            return;
        }

        RootFrame.Content = tag switch
        {
            "fixed-income" => _fixedIncomePage,
            "manual-entry" => _manualEntryPage,
            "projection" => _projectionPage,
            _ => _homePage
        };
    }
}

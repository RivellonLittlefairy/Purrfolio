using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Purrfolio.App.Interfaces;
using Microsoft.UI.Xaml.Media;
using Purrfolio.App.Views;

namespace Purrfolio.App;

public sealed partial class MainWindow : Window
{
    private const string PageTitleAnimationKey = "Purrfolio.PageTitleTransition";

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

        var nextPage = tag switch
        {
            "fixed-income" => _fixedIncomePage,
            "manual-entry" => _manualEntryPage,
            "projection" => _projectionPage,
            _ => _homePage
        };

        if (ReferenceEquals(RootFrame.Content, nextPage))
        {
            return;
        }

        var animationService = ConnectedAnimationService.GetForCurrentView();
        if (RootFrame.Content is IConnectedAnimationPage sourcePage)
        {
            animationService.PrepareToAnimate(PageTitleAnimationKey, sourcePage.AnimationTarget);
        }

        RootFrame.Content = nextPage;

        if (nextPage is IConnectedAnimationPage targetPage)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                animationService.GetAnimation(PageTitleAnimationKey)?.TryStart(targetPage.AnimationTarget);
            });
        }
    }
}

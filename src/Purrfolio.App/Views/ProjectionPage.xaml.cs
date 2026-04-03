using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.Interfaces;
using Purrfolio.App.ViewModels;

namespace Purrfolio.App.Views;

public sealed partial class ProjectionPage : Page, IConnectedAnimationPage
{
    public ProjectionViewModel ViewModel { get; }
    public UIElement AnimationTarget => PageTitleText;

    public ProjectionPage(ProjectionViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private void ProjectionPage_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel.ProjectionPoints.Count == 0)
        {
            ViewModel.SimulateCommand.Execute(null);
        }
    }
}

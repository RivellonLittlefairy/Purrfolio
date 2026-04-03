using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Purrfolio.App.Interfaces;
using Purrfolio.App.ViewModels;
using Windows.Storage.Pickers;

namespace Purrfolio.App.Views;

public sealed partial class OcrImportPage : Page, IConnectedAnimationPage
{
    public OcrImportViewModel ViewModel { get; }

    public UIElement AnimationTarget => PageTitleText;

    public OcrImportPage(OcrImportViewModel viewModel)
    {
        InitializeComponent();
        ViewModel = viewModel;
    }

    private async void BrowseImageButton_OnClick(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".png");
        picker.FileTypeFilter.Add(".jpg");
        picker.FileTypeFilter.Add(".jpeg");
        picker.FileTypeFilter.Add(".webp");
        picker.FileTypeFilter.Add(".bmp");

        var window = App.Current.Services.GetRequiredService<Purrfolio.App.MainWindow>();
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            ViewModel.ImagePath = file.Path;
        }
    }
}

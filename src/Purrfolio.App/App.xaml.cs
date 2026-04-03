using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Purrfolio.App.Services;
using Purrfolio.App.ViewModels;
using Purrfolio.App.Views;
using Purrfolio.Core.Services;
using Purrfolio.Infrastructure.Data;
using Purrfolio.Infrastructure.Repositories;

namespace Purrfolio.App;

public partial class App : Application
{
    private Window? _window;

    public static new App Current => (App)Application.Current;

    public IServiceProvider Services { get; }

    public App()
    {
        InitializeComponent();
        Services = ConfigureServices();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await InitializeDatabaseAsync();

        _window = Services.GetRequiredService<MainWindow>();
        _window.Activate();
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Purrfolio");
        Directory.CreateDirectory(appDataFolder);

        var connectionString = $"Data Source={Path.Combine(appDataFolder, "purrfolio.db")}";

        services.AddDbContextFactory<AppDbContext>(options => options.UseSqlite(connectionString));

        services.AddSingleton<MainWindow>();
        services.AddTransient<HomePage>();
        services.AddTransient<ManualEntryPage>();
        services.AddTransient<FixedIncomePage>();
        services.AddTransient<ProjectionPage>();

        services.AddTransient<AssetViewModel>();
        services.AddTransient<ManualEntryViewModel>();
        services.AddTransient<FixedIncomeViewModel>();
        services.AddTransient<ProjectionViewModel>();

        services.AddTransient<IInvestmentRepository, SqliteInvestmentRepository>();
        services.AddSingleton<INotificationService, WindowsNotificationService>();

        return services.BuildServiceProvider();
    }

    private async Task InitializeDatabaseAsync()
    {
        var dbContextFactory = Services.GetRequiredService<IDbContextFactory<AppDbContext>>();

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();
        await dbContext.Database.EnsureCreatedAsync();
        await DbSeeder.SeedAsync(dbContext);
    }
}

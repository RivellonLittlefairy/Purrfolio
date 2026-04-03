using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;

namespace Purrfolio.App.Services;

public sealed class WindowsNotificationService : INotificationService
{
    public Task NotifyAsync(string title, string body)
    {
        try
        {
            var payload = new AppNotificationBuilder()
                .AddText(title)
                .AddText(body)
                .BuildNotification();

            AppNotificationManager.Default.Show(payload);
        }
        catch
        {
            // Ignore if app notification is unavailable in current runtime mode.
        }

        return Task.CompletedTask;
    }
}

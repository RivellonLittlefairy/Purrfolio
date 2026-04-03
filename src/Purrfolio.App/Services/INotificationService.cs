namespace Purrfolio.App.Services;

public interface INotificationService
{
    Task NotifyAsync(string title, string body);
}

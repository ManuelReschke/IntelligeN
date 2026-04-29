namespace IntelligeN.App.Contracts.Shell;

public interface IUserNotificationService
{
    ValueTask ShowInfoAsync(string message, CancellationToken cancellationToken = default);
}

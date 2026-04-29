using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using IntelligeN.Core.Hosting;
using IntelligeN.Infrastructure.Storage;

namespace IntelligeN.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var directories = AppRuntime.BuildDirectories(
                Path.Combine(AppContext.BaseDirectory, "runtime"));

            DirectoryBootstrapper.EnsureCreated(directories);

            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}

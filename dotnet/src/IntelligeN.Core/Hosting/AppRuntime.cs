using IntelligeN.Core.Abstractions.Runtime;

namespace IntelligeN.Core.Hosting;

public static class AppRuntime
{
    public static AppDirectories BuildDirectories(string rootDirectory)
    {
        var normalizedRoot = Path.GetFullPath(rootDirectory);

        return new AppDirectories(
            RootDirectory: normalizedRoot,
            DataDirectory: Path.Combine(normalizedRoot, "data"),
            PluginDirectory: Path.Combine(normalizedRoot, "plugins"),
            CacheDirectory: Path.Combine(normalizedRoot, "cache"));
    }

    public static string BuildStatusMessage(IClock clock) =>
        $"Scaffold active - UTC {clock.UtcNow:yyyy-MM-dd HH:mm:ss}.";
}

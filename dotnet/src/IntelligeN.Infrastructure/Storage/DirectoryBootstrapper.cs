using IntelligeN.Core.Hosting;

namespace IntelligeN.Infrastructure.Storage;

public static class DirectoryBootstrapper
{
    public static void EnsureCreated(AppDirectories directories)
    {
        Directory.CreateDirectory(directories.RootDirectory);
        Directory.CreateDirectory(directories.DataDirectory);
        Directory.CreateDirectory(directories.PluginDirectory);
        Directory.CreateDirectory(directories.CacheDirectory);
    }
}

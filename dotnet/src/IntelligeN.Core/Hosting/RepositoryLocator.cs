namespace IntelligeN.Core.Hosting;

public static class RepositoryLocator
{
    public static RepositoryLayout? TryLocateFrom(string startDirectory)
    {
        var current = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (current is not null)
        {
            var root = current.FullName;
            var dotnetDirectory = Path.Combine(root, "dotnet");
            var legacyBinDirectory = Path.Combine(root, "bin");
            var legacyConfigurationDirectory = Path.Combine(legacyBinDirectory, "configuration");
            var pluginSourceDirectory = Path.Combine(dotnetDirectory, "plugins");

            if (File.Exists(Path.Combine(root, "AGENTS.md")) &&
                Directory.Exists(dotnetDirectory) &&
                Directory.Exists(legacyBinDirectory))
            {
                return new RepositoryLayout(
                    RootDirectory: root,
                    DotnetDirectory: dotnetDirectory,
                    LegacyBinDirectory: legacyBinDirectory,
                    LegacyConfigurationDirectory: legacyConfigurationDirectory,
                    PluginSourceDirectory: pluginSourceDirectory);
            }

            current = current.Parent;
        }

        return null;
    }
}

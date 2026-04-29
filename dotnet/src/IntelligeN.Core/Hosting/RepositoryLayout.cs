namespace IntelligeN.Core.Hosting;

public sealed record RepositoryLayout(
    string RootDirectory,
    string DotnetDirectory,
    string LegacyBinDirectory,
    string LegacyConfigurationDirectory,
    string PluginSourceDirectory);

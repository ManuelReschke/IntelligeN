namespace IntelligeN.Core.Hosting;

public sealed record AppDirectories(
    string RootDirectory,
    string DataDirectory,
    string PluginDirectory,
    string CacheDirectory);

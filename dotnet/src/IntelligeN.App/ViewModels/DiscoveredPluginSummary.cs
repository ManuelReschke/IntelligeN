namespace IntelligeN.App.ViewModels;

public sealed record DiscoveredPluginSummary(
    string AssemblyFile,
    string DisplayName,
    string PluginId,
    string Kind,
    string Version);

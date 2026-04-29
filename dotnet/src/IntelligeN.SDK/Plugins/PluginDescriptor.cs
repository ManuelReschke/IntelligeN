namespace IntelligeN.SDK.Plugins;

public sealed record PluginDescriptor(
    string Id,
    string DisplayName,
    PluginKind Kind,
    string Version,
    string Description);

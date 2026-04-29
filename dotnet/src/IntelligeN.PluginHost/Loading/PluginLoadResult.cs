using IntelligeN.SDK.Plugins;

namespace IntelligeN.PluginHost.Loading;

public sealed record PluginLoadResult(
    string AssemblyPath,
    IReadOnlyList<IPlugin> Plugins,
    Exception? Error);

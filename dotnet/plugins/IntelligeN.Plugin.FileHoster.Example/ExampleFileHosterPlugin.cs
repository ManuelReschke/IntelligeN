using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.FileHoster.Example;

public sealed class ExampleFileHosterPlugin : PluginBase
{
    public override PluginDescriptor Descriptor { get; } = new(
        Id: "filehoster.example",
        DisplayName: "Example FileHoster",
        Kind: PluginKind.FileHoster,
        Version: "0.1.0",
        Description: "Reference filehoster plugin for the new plugin loading path.");
}

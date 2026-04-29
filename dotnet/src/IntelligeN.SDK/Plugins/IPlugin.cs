namespace IntelligeN.SDK.Plugins;

public interface IPlugin
{
    PluginDescriptor Descriptor { get; }

    ValueTask InitializeAsync(CancellationToken cancellationToken = default);
}

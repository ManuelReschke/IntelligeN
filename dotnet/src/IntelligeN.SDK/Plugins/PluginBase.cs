namespace IntelligeN.SDK.Plugins;

public abstract class PluginBase : IPlugin
{
    public abstract PluginDescriptor Descriptor { get; }

    public virtual ValueTask InitializeAsync(CancellationToken cancellationToken = default) =>
        ValueTask.CompletedTask;
}

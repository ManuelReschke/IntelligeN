using IntelligeN.SDK.Plugins;

namespace IntelligeN.SDK.Cms;

public interface ICmsPublisherPlugin : IPlugin
{
    CmsPublishProfile Profile { get; }

    ValueTask<CmsPublishResult> PublishAsync(
        CmsPublishRequest request,
        CancellationToken cancellationToken = default);
}

using IntelligeN.SDK.Plugins;

namespace IntelligeN.SDK.Crawlers;

public interface ICrawlerPlugin : IPlugin
{
    CrawlerProfile Profile { get; }

    ValueTask<CrawlerResult> CrawlAsync(
        CrawlerRequest request,
        CancellationToken cancellationToken = default);
}

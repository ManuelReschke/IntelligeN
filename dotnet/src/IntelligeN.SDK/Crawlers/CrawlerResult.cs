namespace IntelligeN.SDK.Crawlers;

public sealed record CrawlerResult(
    string CrawlerId,
    IReadOnlyList<CrawlerSuggestion> Suggestions);

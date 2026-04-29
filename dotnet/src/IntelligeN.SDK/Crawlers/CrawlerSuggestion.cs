namespace IntelligeN.SDK.Crawlers;

public sealed record CrawlerSuggestion(
    string ControlId,
    string Value,
    string Reason,
    double Confidence = 0.5d);

namespace IntelligeN.App.ViewModels;

public sealed record CrawlerSuggestionSummary(
    string CrawlerId,
    string ControlId,
    string Value,
    string Reason,
    double Confidence,
    bool Applied);

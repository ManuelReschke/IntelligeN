namespace IntelligeN.SDK.Crawlers;

public sealed record CrawlerProfile(
    IReadOnlyList<string> SupportedTemplateTypes,
    IReadOnlyList<string> SupportedControlIds,
    string Notes)
{
    public bool Supports(string templateType, string? controlId)
    {
        var templateMatches = SupportedTemplateTypes.Count == 0 ||
                              SupportedTemplateTypes.Any(candidate =>
                                  string.Equals(candidate, templateType, StringComparison.OrdinalIgnoreCase));

        var controlMatches = string.IsNullOrWhiteSpace(controlId) ||
                             SupportedControlIds.Count == 0 ||
                             SupportedControlIds.Any(candidate =>
                                 string.Equals(candidate, controlId, StringComparison.OrdinalIgnoreCase));

        return templateMatches && controlMatches;
    }
}

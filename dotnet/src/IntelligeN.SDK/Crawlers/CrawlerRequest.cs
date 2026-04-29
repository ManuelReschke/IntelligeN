namespace IntelligeN.SDK.Crawlers;

public sealed record CrawlerRequest(
    string TemplateType,
    string ReleaseName,
    string? TargetControlId,
    IReadOnlyList<CrawlerFieldValue> Fields)
{
    public string GetFieldValue(string controlId)
    {
        return Fields.FirstOrDefault(field =>
            string.Equals(field.ControlId, controlId, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }
}

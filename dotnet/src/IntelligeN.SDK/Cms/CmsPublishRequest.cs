namespace IntelligeN.SDK.Cms;

public sealed record CmsPublishRequest(
    string TemplateType,
    CmsPublishCredentials Credentials,
    CmsPublishDestination Destination,
    string Subject,
    string Tags,
    string Message,
    int? ArticleId,
    int? ArticlePathId,
    IReadOnlyList<CmsPublishFieldValue> Fields,
    IReadOnlyList<CmsCustomField> CustomFields)
{
    public string GetFieldValue(string controlId)
    {
        return Fields.FirstOrDefault(field =>
            string.Equals(field.ControlId, controlId, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }
}

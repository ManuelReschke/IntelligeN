namespace IntelligeN.SDK.Cms;

public sealed record CmsPublishProfile(
    CmsPublishTargetKind TargetKind,
    IReadOnlyList<string> SupportedTemplateTypes,
    IReadOnlyList<string> SupportedCapabilities,
    string Notes)
{
    public bool SupportsTemplate(string templateType)
    {
        return SupportedTemplateTypes.Count == 0 ||
               SupportedTemplateTypes.Any(candidate =>
                   string.Equals(candidate, templateType, StringComparison.OrdinalIgnoreCase));
    }

    public bool SupportsCapability(string capability)
    {
        return SupportedCapabilities.Any(candidate =>
            string.Equals(candidate, capability, StringComparison.OrdinalIgnoreCase));
    }
}

namespace IntelligeN.LegacyImport.Configuration;

public sealed record ControlTemplateSummary(
    string TemplateName,
    string Title,
    string Hint,
    string DefaultValue,
    int ListItemCount)
{
    public string DisplayName => string.IsNullOrWhiteSpace(TemplateName) ? "Default" : TemplateName;
}

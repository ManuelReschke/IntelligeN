namespace IntelligeN.Core.Templating;

public sealed record ContentTemplateDefinition(
    string Id,
    string Name,
    string TemplateType,
    string Category,
    string Description,
    string SubjectTemplate,
    string BodyTemplate,
    string TagTemplate)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(TemplateType)
            ? Name
            : $"{Name} [{TemplateType}]";
}

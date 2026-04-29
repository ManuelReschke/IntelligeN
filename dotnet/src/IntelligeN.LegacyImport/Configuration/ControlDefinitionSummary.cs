namespace IntelligeN.LegacyImport.Configuration;

public sealed record ControlDefinitionSummary(
    string ControlName,
    int TemplateCount,
    string DefaultTitle,
    string DefaultHint,
    int TotalListItemCount,
    IReadOnlyList<ControlTemplateSummary> Templates);

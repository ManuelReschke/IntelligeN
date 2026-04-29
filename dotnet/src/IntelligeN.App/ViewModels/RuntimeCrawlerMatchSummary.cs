namespace IntelligeN.App.ViewModels;

public sealed record RuntimeCrawlerMatchSummary(
    string DisplayName,
    string PluginId,
    string Version,
    string SupportedTemplates,
    string SupportedControls,
    string Notes);

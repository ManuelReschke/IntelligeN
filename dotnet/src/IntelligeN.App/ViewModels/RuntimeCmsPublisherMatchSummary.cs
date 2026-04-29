namespace IntelligeN.App.ViewModels;

public sealed record RuntimeCmsPublisherMatchSummary(
    string DisplayName,
    string PluginId,
    string Version,
    string TargetKind,
    string SupportedTemplates,
    string SupportedCapabilities,
    string Notes);

namespace IntelligeN.Core.Publishing;

public sealed record PublishHistoryEntry(
    string Id,
    DateTimeOffset CreatedUtc,
    string WorkspaceFileName,
    string TemplateType,
    string ContentTemplateId,
    string ContentTemplateName,
    string TargetProfileId,
    string TargetProfileName,
    string PluginId,
    string Website,
    string Subject,
    string Tags,
    string Message,
    string ScopeValue,
    string ThreadId,
    string Prefix,
    string Icon,
    bool PostReply,
    IReadOnlyList<PublishFieldSnapshot> Fields,
    string Status,
    string ResultMessage,
    string ArticleUrl,
    int? ArticleId)
{
    public string DisplayName => $"{TargetProfileName} - {Subject}";

    public string CreatedLabel => CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss");

    public bool IsSuccess =>
        string.Equals(Status, "Success", StringComparison.OrdinalIgnoreCase);

    public bool CanRetry => !IsSuccess;

    public string ResultSummary =>
        string.IsNullOrWhiteSpace(ResultMessage)
            ? Status
            : $"{Status}: {ResultMessage}";

    public string WorkspaceSummary =>
        string.IsNullOrWhiteSpace(TemplateType)
            ? WorkspaceFileName
            : $"{WorkspaceFileName} [{TemplateType}]";

    public string FieldSummary =>
        string.Join(", ",
            Fields
                .Where(snapshot => !string.IsNullOrWhiteSpace(snapshot.Value))
                .Take(6)
                .Select(snapshot => $"{snapshot.ControlId}={snapshot.Value}"));
}

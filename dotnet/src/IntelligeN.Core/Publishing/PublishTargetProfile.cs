namespace IntelligeN.Core.Publishing;

public sealed record PublishTargetProfile(
    string Id,
    string Name,
    string PluginId,
    string TemplateType,
    string ContentTemplateId,
    bool IsActive,
    string Website,
    string AccountName,
    string AccountPassword,
    string ScopeValue,
    string ThreadId,
    string Prefix,
    string Icon,
    bool PostReply,
    string Notes)
{
    public string DisplayName
    {
        get
        {
            var normalizedWebsite = Website.Trim();
            return string.IsNullOrWhiteSpace(normalizedWebsite)
                ? Name
                : $"{Name} ({normalizedWebsite})";
        }
    }
}

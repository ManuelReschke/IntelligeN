namespace IntelligeN.SDK.Cms;

public sealed record CmsPublishDestination(
    string Website,
    IReadOnlyList<string> CategoryIds,
    string? ForumId,
    string? ThreadId,
    string? Prefix,
    string? Icon,
    bool PostReply);

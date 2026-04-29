namespace IntelligeN.SDK.Cms;

public sealed record CmsPublishResult(
    string PublisherId,
    CmsPublishStatus Status,
    int? ArticleId,
    string ArticleUrl,
    string Message);

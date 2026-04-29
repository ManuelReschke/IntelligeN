using IntelligeN.SDK.Cms;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Cms.WarezDdl;

public sealed class WarezDdlPublisherPlugin : PluginBase, ICmsPublisherPlugin
{
    public override PluginDescriptor Descriptor { get; } = new(
        Id: "cms.warezddl",
        DisplayName: "warezddl Board",
        Kind: PluginKind.Cms,
        Version: "0.1.0",
        Description: "Board-oriented CMS target for warezDDL-like publish flows in the Avalonia migration.");

    public CmsPublishProfile Profile { get; } = new(
        TargetKind: CmsPublishTargetKind.Board,
        SupportedTemplateTypes: [],
        SupportedCapabilities:
        [
            "forum",
            "thread-reply",
            "prefix",
            "icon",
            "intelligent-posting",
            "bbcode",
            "board"
        ],
        Notes: "First board-style publish target for the migration. The contract follows the legacy TCMSBoardPlugIn/TCMSBoardIPPlugIn shape used by WBB, XenForo, phpBB and similar forum targets. Transport and login flow are not ported yet.");

    public ValueTask<CmsPublishResult> PublishAsync(
        CmsPublishRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Destination.Website))
        {
            return ValidationFailed("Website is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Credentials.AccountName) ||
            string.IsNullOrWhiteSpace(request.Credentials.AccountPassword))
        {
            return ValidationFailed("Account name and password are required.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return ValidationFailed("Subject is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return ValidationFailed("Message is required.");
        }

        if (request.Destination.PostReply)
        {
            if (string.IsNullOrWhiteSpace(request.Destination.ThreadId))
            {
                return ValidationFailed("Thread id is required when reply mode is enabled.");
            }
        }
        else if (string.IsNullOrWhiteSpace(request.Destination.ForumId))
        {
            return ValidationFailed("Forum id is required for a new board post.");
        }

        var targetInfo = request.Destination.PostReply
            ? $"reply to thread {request.Destination.ThreadId}"
            : $"new thread in forum {request.Destination.ForumId}";

        return ValueTask.FromResult(new CmsPublishResult(
            PublisherId: Descriptor.Id,
            Status: CmsPublishStatus.Failed,
            ArticleId: request.ArticleId,
            ArticleUrl: string.Empty,
            Message: $"Validated warezDDL-style publish request for {targetInfo}, but the board transport is not ported yet."));
    }

    private ValueTask<CmsPublishResult> ValidationFailed(string message)
    {
        return ValueTask.FromResult(new CmsPublishResult(
            PublisherId: Descriptor.Id,
            Status: CmsPublishStatus.ValidationFailed,
            ArticleId: null,
            ArticleUrl: string.Empty,
            Message: message));
    }
}

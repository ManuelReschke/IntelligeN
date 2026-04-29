using System.Net;
using System.Text.RegularExpressions;
using IntelligeN.SDK.Cms;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Cms.MyBB;

public sealed class MyBBPublisherPlugin : PluginBase, ICmsPublisherPlugin
{
    public override PluginDescriptor Descriptor { get; } = new(
        Id: "cms.mybb",
        DisplayName: "MyBB",
        Kind: PluginKind.Cms,
        Version: "0.2.0",
        Description: "Best-effort MyBB board publisher for the Avalonia migration.");

    public CmsPublishProfile Profile { get; } = new(
        TargetKind: CmsPublishTargetKind.Board,
        SupportedTemplateTypes: [],
        SupportedCapabilities:
        [
            "forum",
            "thread-reply",
            "prefix",
            "icon",
            "bbcode",
            "login",
            "mybb"
        ],
        Notes: "Ports the existing MyBB workflow as far as it is structurally clear in the Delphi plugin: login, pre-post page, hidden security fields and submit for thread/reply. Edit mode, xthreads automation and helper search are not ported yet.");

    public async ValueTask<CmsPublishResult> PublishAsync(
        CmsPublishRequest request,
        CancellationToken cancellationToken = default)
    {
        var validationError = ValidateRequest(request);
        if (validationError is not null)
        {
            return validationError;
        }

        var websiteBaseUrl = NormalizeWebsiteBaseUrl(request.Destination.Website);

        try
        {
            using var client = CreateHttpClient();

            var loginPage = await PostFormAsync(
                client,
                new Uri(websiteBaseUrl, "member.php"),
                BuildLoginForm(request),
                referer: websiteBaseUrl,
                cancellationToken);

            if (!IsLoginSuccessful(loginPage.Body))
            {
                return new CmsPublishResult(
                    PublisherId: Descriptor.Id,
                    Status: CmsPublishStatus.AuthenticationFailed,
                    ArticleId: null,
                    ArticleUrl: string.Empty,
                    Message: ExtractErrorMessage(loginPage.Body) ?? "MyBB-Login fehlgeschlagen.");
            }

            var prePostUrl = request.Destination.PostReply
                ? new Uri(websiteBaseUrl, $"newreply.php?tid={request.Destination.ThreadId}")
                : new Uri(websiteBaseUrl, $"newthread.php?fid={request.Destination.ForumId}");

            var prePostPage = await GetPageAsync(client, prePostUrl, cancellationToken);
            var hiddenFields = ExtractHiddenFields(prePostPage.Body);
            if (!hiddenFields.ContainsKey("my_post_key"))
            {
                return new CmsPublishResult(
                    PublisherId: Descriptor.Id,
                    Status: CmsPublishStatus.Failed,
                    ArticleId: null,
                    ArticleUrl: string.Empty,
                    Message: "MyBB-Pre-Post-Seite liefert keinen my_post_key.");
            }

            var submitUrl = request.Destination.PostReply
                ? new Uri(websiteBaseUrl, $"newreply.php?tid={request.Destination.ThreadId}&processed=1")
                : new Uri(websiteBaseUrl, $"newthread.php?fid={request.Destination.ForumId}&processed=1");

            var submitPage = await PostFormAsync(
                client,
                submitUrl,
                BuildPostForm(request, hiddenFields),
                referer: prePostUrl,
                cancellationToken);

            var errorMessage = ExtractErrorMessage(submitPage.Body);
            if (!string.IsNullOrWhiteSpace(errorMessage))
            {
                return new CmsPublishResult(
                    PublisherId: Descriptor.Id,
                    Status: CmsPublishStatus.Rejected,
                    ArticleId: null,
                    ArticleUrl: string.Empty,
                    Message: errorMessage);
            }

            var articleUrl = BuildArticleUrl(websiteBaseUrl, request, submitPage);
            return new CmsPublishResult(
                PublisherId: Descriptor.Id,
                Status: CmsPublishStatus.Success,
                ArticleId: TryExtractArticleId(articleUrl),
                ArticleUrl: articleUrl,
                Message: request.Destination.PostReply
                    ? "MyBB-Antwort erfolgreich erstellt."
                    : "MyBB-Thread erfolgreich erstellt.");
        }
        catch (HttpRequestException exception)
        {
            return new CmsPublishResult(
                PublisherId: Descriptor.Id,
                Status: CmsPublishStatus.Failed,
                ArticleId: null,
                ArticleUrl: string.Empty,
                Message: $"HTTP-Fehler beim MyBB-Transport: {exception.Message}");
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate | DecompressionMethods.Brotli,
            CookieContainer = new CookieContainer(),
            UseCookies = true
        };

        var client = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("IntelligeN/0.2");
        return client;
    }

    private static async Task<PageResponse> GetPageAsync(
        HttpClient client,
        Uri uri,
        CancellationToken cancellationToken)
    {
        using var response = await client.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new PageResponse(response.RequestMessage?.RequestUri ?? uri, body);
    }

    private static async Task<PageResponse> PostFormAsync(
        HttpClient client,
        Uri uri,
        IReadOnlyList<KeyValuePair<string, string>> fields,
        Uri referer,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new FormUrlEncodedContent(fields)
        };
        request.Headers.Referrer = referer;

        using var response = await client.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        return new PageResponse(response.RequestMessage?.RequestUri ?? uri, body);
    }

    private static IReadOnlyList<KeyValuePair<string, string>> BuildLoginForm(CmsPublishRequest request)
    {
        return
        [
            new("username", request.Credentials.AccountName),
            new("password", request.Credentials.AccountPassword),
            new("url", string.Empty),
            new("action", "do_login"),
            new("submit", string.Empty)
        ];
    }

    private static IReadOnlyList<KeyValuePair<string, string>> BuildPostForm(
        CmsPublishRequest request,
        IReadOnlyDictionary<string, string> hiddenFields)
    {
        var formFields = new List<KeyValuePair<string, string>>();
        foreach (var pair in hiddenFields)
        {
            if (string.Equals(pair.Key, "action", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key, "subject", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key, "message", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(pair.Key, "submit", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            formFields.Add(new KeyValuePair<string, string>(pair.Key, pair.Value));
        }

        var prefixField = TryReadCustomField(request, "mybb:prefix-field") ?? "threadprefix";
        AddIfPresent(formFields, prefixField, request.Destination.Prefix);
        AddIfPresent(formFields, "icon", request.Destination.Icon);
        AddIfPresent(formFields, "subject", request.Subject);
        AddIfPresent(formFields, "message", request.Message);

        if (TryReadBooleanCustomField(request, "mybb:signature", out var useSignature) ? useSignature : true)
        {
            formFields.Add(new KeyValuePair<string, string>("postoptions[signature]", "1"));
        }

        if (TryReadBooleanCustomField(request, "mybb:disable-smilies", out var disableSmilies) && disableSmilies)
        {
            formFields.Add(new KeyValuePair<string, string>("postoptions[disablesmilies]", "1"));
        }

        foreach (var customField in request.CustomFields.Where(field =>
                     field.Name.StartsWith("mybb:field:", StringComparison.OrdinalIgnoreCase)))
        {
            var fieldName = customField.Name["mybb:field:".Length..];
            if (!string.IsNullOrWhiteSpace(fieldName))
            {
                AddIfPresent(formFields, fieldName, customField.Value);
            }
        }

        formFields.Add(new KeyValuePair<string, string>("submit", string.Empty));
        if (request.Destination.PostReply)
        {
            formFields.Add(new KeyValuePair<string, string>("tid", request.Destination.ThreadId!));
            formFields.Add(new KeyValuePair<string, string>("action", "do_newreply"));
        }
        else
        {
            formFields.Add(new KeyValuePair<string, string>("action", "do_newthread"));
        }

        return formFields;
    }

    private static void AddIfPresent(ICollection<KeyValuePair<string, string>> formFields, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(value))
        {
            formFields.Add(new KeyValuePair<string, string>(name, value));
        }
    }

    private static bool IsLoginSuccessful(string html)
    {
        return html.Contains("member.php?action=logout", StringComparison.OrdinalIgnoreCase) ||
               html.Contains("action=logout", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractErrorMessage(string html)
    {
        var match = Regex.Match(
            html,
            "<div[^>]*class=\"error\"[^>]*>(.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!match.Success)
        {
            match = Regex.Match(
                html,
                "<td[^>]*class=\"trow1\"[^>]*>(.*?)</td>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }

        return match.Success ? StripHtml(match.Groups[1].Value) : null;
    }

    private static IReadOnlyDictionary<string, string> ExtractHiddenFields(string html)
    {
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(
                     html,
                     "<input[^>]*type=[\"']hidden[\"'][^>]*name=[\"']([^\"']+)[\"'][^>]*value=[\"']([^\"']*)[\"'][^>]*>",
                     RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var name = WebUtility.HtmlDecode(match.Groups[1].Value);
            if (name.Length == 0)
            {
                continue;
            }

            fields[name] = WebUtility.HtmlDecode(match.Groups[2].Value);
        }

        return fields;
    }

    private static string BuildArticleUrl(Uri websiteBaseUrl, CmsPublishRequest request, PageResponse submitPage)
    {
        var candidates = new[]
        {
            submitPage.FinalUri.ToString(),
            ExtractArticleLinkFromHtml(submitPage.Body)
        };

        foreach (var candidate in candidates.Where(value => !string.IsNullOrWhiteSpace(value)))
        {
            if (Uri.TryCreate(candidate, UriKind.Absolute, out var absoluteUri))
            {
                return absoluteUri.ToString();
            }

            if (Uri.TryCreate(websiteBaseUrl, candidate, out var relativeUri))
            {
                return relativeUri.ToString();
            }
        }

        if (request.Destination.PostReply && !string.IsNullOrWhiteSpace(request.Destination.ThreadId))
        {
            return new Uri(websiteBaseUrl, $"showthread.php?tid={request.Destination.ThreadId}").ToString();
        }

        return submitPage.FinalUri.ToString();
    }

    private static string ExtractArticleLinkFromHtml(string html)
    {
        var match = Regex.Match(
            html,
            @"showthread\.php\?(?:pid=\d+#pid\d+|tid=\d+)",
            RegexOptions.IgnoreCase);

        return match.Success ? match.Value : string.Empty;
    }

    private static int? TryExtractArticleId(string articleUrl)
    {
        var match = Regex.Match(articleUrl, @"[?&](?:pid|tid)=(\d+)", RegexOptions.IgnoreCase);
        return match.Success && int.TryParse(match.Groups[1].Value, out var articleId)
            ? articleId
            : null;
    }

    private static Uri NormalizeWebsiteBaseUrl(string website)
    {
        var trimmed = website.Trim();
        if (!trimmed.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !trimmed.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = $"https://{trimmed}";
        }

        if (!trimmed.EndsWith("/", StringComparison.Ordinal))
        {
            trimmed += "/";
        }

        return new Uri(trimmed, UriKind.Absolute);
    }

    private static string StripHtml(string value)
    {
        var withoutTags = Regex.Replace(value, "<.*?>", " ", RegexOptions.Singleline);
        return WebUtility.HtmlDecode(Regex.Replace(withoutTags, "\\s+", " ")).Trim();
    }

    private static bool TryReadBooleanCustomField(CmsPublishRequest request, string fieldName, out bool value)
    {
        var rawValue = TryReadCustomField(request, fieldName);
        if (rawValue is null)
        {
            value = false;
            return false;
        }

        if (bool.TryParse(rawValue, out value))
        {
            return true;
        }

        if (string.Equals(rawValue, "1", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawValue, "yes", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(rawValue, "0", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(rawValue, "no", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    private static string? TryReadCustomField(CmsPublishRequest request, string fieldName)
    {
        return request.CustomFields.FirstOrDefault(field =>
            string.Equals(field.Name, fieldName, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    private CmsPublishResult? ValidateRequest(CmsPublishRequest request)
    {
        if (request.ArticleId is not null)
        {
            return ValidationFailed("Bearbeiten bestehender MyBB-Posts ist noch nicht portiert.");
        }

        if (string.IsNullOrWhiteSpace(request.Destination.Website))
        {
            return ValidationFailed("Website ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.Credentials.AccountName) ||
            string.IsNullOrWhiteSpace(request.Credentials.AccountPassword))
        {
            return ValidationFailed("Benutzername und Passwort sind erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            return ValidationFailed("Betreff ist erforderlich.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return ValidationFailed("Nachricht ist erforderlich.");
        }

        if (request.Destination.PostReply)
        {
            if (string.IsNullOrWhiteSpace(request.Destination.ThreadId))
            {
                return ValidationFailed("Thread-ID ist fuer Antworten erforderlich.");
            }
        }
        else if (string.IsNullOrWhiteSpace(request.Destination.ForumId))
        {
            return ValidationFailed("Forum-ID ist fuer neue Threads erforderlich.");
        }

        return null;
    }

    private CmsPublishResult ValidationFailed(string message)
    {
        return new CmsPublishResult(
            PublisherId: Descriptor.Id,
            Status: CmsPublishStatus.ValidationFailed,
            ArticleId: null,
            ArticleUrl: string.Empty,
            Message: message);
    }

    private sealed record PageResponse(Uri FinalUri, string Body);
}

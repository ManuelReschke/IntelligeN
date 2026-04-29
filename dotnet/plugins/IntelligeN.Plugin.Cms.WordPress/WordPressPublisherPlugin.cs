using System.Text;
using System.Xml.Linq;
using IntelligeN.SDK.Cms;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Cms.WordPress;

public sealed class WordPressPublisherPlugin : PluginBase, ICmsPublisherPlugin
{
    private static readonly HttpClient HttpClient = CreateHttpClient();

    public override PluginDescriptor Descriptor { get; } = new(
        Id: "cms.wordpress",
        DisplayName: "WordPress",
        Kind: PluginKind.Cms,
        Version: "0.3.0",
        Description: "WordPress XML-RPC publisher for the Avalonia migration.");

    public CmsPublishProfile Profile { get; } = new(
        TargetKind: CmsPublishTargetKind.Blog,
        SupportedTemplateTypes: [],
        SupportedCapabilities:
        [
            "categories",
            "tags",
            "custom-fields",
            "draft",
            "sticky",
            "comment-status",
            "edit-article",
            "xmlrpc"
        ],
        Notes: "Uses the official WordPress XML-RPC post API as the first real publish transport in the .NET migration. This covers create/edit for standard blog posts without porting the legacy wp-admin form workflow yet.");

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
        var endpoint = BuildXmlRpcEndpoint(websiteBaseUrl);
        var contentStruct = BuildContentStruct(request);

        try
        {
            if (request.ArticleId is int articleId and > 0)
            {
                await InvokeXmlRpcAsync(
                    endpoint,
                    "wp.editPost",
                    [1, request.Credentials.AccountName, request.Credentials.AccountPassword, articleId, contentStruct],
                    cancellationToken);

                return new CmsPublishResult(
                    PublisherId: Descriptor.Id,
                    Status: CmsPublishStatus.Success,
                    ArticleId: articleId,
                    ArticleUrl: BuildArticleUrl(websiteBaseUrl, articleId),
                    Message: $"WordPress-Artikel {articleId} erfolgreich aktualisiert.");
            }

            var newArticleId = await CreatePostAsync(endpoint, request, contentStruct, cancellationToken);
            return new CmsPublishResult(
                PublisherId: Descriptor.Id,
                Status: CmsPublishStatus.Success,
                ArticleId: newArticleId,
                ArticleUrl: BuildArticleUrl(websiteBaseUrl, newArticleId),
                Message: $"WordPress-Artikel {newArticleId} erfolgreich erstellt.");
        }
        catch (WordPressXmlRpcFaultException exception)
        {
            return new CmsPublishResult(
                PublisherId: Descriptor.Id,
                Status: MapFaultStatus(exception.FaultCode),
                ArticleId: request.ArticleId,
                ArticleUrl: string.Empty,
                Message: $"WordPress XML-RPC Fehler {exception.FaultCode}: {exception.Message}");
        }
        catch (HttpRequestException exception)
        {
            return new CmsPublishResult(
                PublisherId: Descriptor.Id,
                Status: CmsPublishStatus.Failed,
                ArticleId: request.ArticleId,
                ArticleUrl: string.Empty,
                Message: $"HTTP-Fehler beim WordPress-Transport: {exception.Message}");
        }
    }

    private static async Task<int> CreatePostAsync(
        Uri endpoint,
        CmsPublishRequest request,
        IReadOnlyDictionary<string, object> contentStruct,
        CancellationToken cancellationToken)
    {
        var responseDocument = await InvokeXmlRpcAsync(
            endpoint,
            "wp.newPost",
            [1, request.Credentials.AccountName, request.Credentials.AccountPassword, contentStruct],
            cancellationToken);

        var valueElement = responseDocument.Root?
            .Element("params")?
            .Element("param")?
            .Element("value");

        if (!TryReadIntValue(valueElement, out var articleId))
        {
            throw new InvalidDataException("WordPress XML-RPC returned no article id.");
        }

        return articleId;
    }

    private static async Task<XDocument> InvokeXmlRpcAsync(
        Uri endpoint,
        string methodName,
        IReadOnlyList<object> args,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(BuildMethodCall(methodName, args), Encoding.UTF8, "text/xml")
        };

        using var response = await HttpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseDocument = XDocument.Parse(responseBody);

        if (TryReadFault(responseDocument, out var faultCode, out var faultMessage))
        {
            throw new WordPressXmlRpcFaultException(faultCode, faultMessage);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        return responseDocument;
    }

    private static string BuildMethodCall(string methodName, IReadOnlyList<object> args)
    {
        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement("methodCall",
                new XElement("methodName", methodName),
                new XElement("params",
                    args.Select(argument =>
                        new XElement("param", BuildValueElement(argument))))));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildValueElement(object? value)
    {
        if (value is null)
        {
            return new XElement("value", new XElement("string", string.Empty));
        }

        return value switch
        {
            string stringValue => new XElement("value", new XElement("string", stringValue)),
            bool boolValue => new XElement("value", new XElement("boolean", boolValue ? "1" : "0")),
            int intValue => new XElement("value", new XElement("int", intValue)),
            long longValue => new XElement("value", new XElement("i4", longValue)),
            IReadOnlyDictionary<string, object> dictionaryValue => new XElement("value",
                new XElement("struct",
                    dictionaryValue.Select(pair =>
                        new XElement("member",
                            new XElement("name", pair.Key),
                            BuildValueElement(pair.Value))))),
            IEnumerable<object> listValue => new XElement("value",
                new XElement("array",
                    new XElement("data", listValue.Select(BuildValueElement)))),
            _ => new XElement("value", new XElement("string", value.ToString() ?? string.Empty))
        };
    }

    private static bool TryReadFault(XDocument responseDocument, out int faultCode, out string faultMessage)
    {
        faultCode = 0;
        faultMessage = string.Empty;

        var faultStruct = responseDocument.Root?
            .Element("fault")?
            .Element("value")?
            .Element("struct");

        if (faultStruct is null)
        {
            return false;
        }

        foreach (var member in faultStruct.Elements("member"))
        {
            var name = member.Element("name")?.Value ?? string.Empty;
            var valueElement = member.Element("value");

            if (string.Equals(name, "faultCode", StringComparison.OrdinalIgnoreCase))
            {
                TryReadIntValue(valueElement, out faultCode);
            }
            else if (string.Equals(name, "faultString", StringComparison.OrdinalIgnoreCase))
            {
                faultMessage = ReadStringValue(valueElement);
            }
        }

        return true;
    }

    private static bool TryReadIntValue(XElement? valueElement, out int result)
    {
        var text = valueElement?.Element("int")?.Value ??
                   valueElement?.Element("i4")?.Value ??
                   valueElement?.Element("string")?.Value ??
                   valueElement?.Value;

        return int.TryParse(text, out result);
    }

    private static string ReadStringValue(XElement? valueElement)
    {
        return valueElement?.Element("string")?.Value ??
               valueElement?.Element("int")?.Value ??
               valueElement?.Element("i4")?.Value ??
               valueElement?.Element("boolean")?.Value ??
               valueElement?.Value ??
               string.Empty;
    }

    private static IReadOnlyDictionary<string, object> BuildContentStruct(CmsPublishRequest request)
    {
        var contentStruct = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["post_type"] = "post",
            ["post_title"] = request.Subject,
            ["post_content"] = request.Message,
            ["post_status"] = ResolvePostStatus(request),
            ["comment_status"] = ResolveCommentStatus(request)
        };

        if (ResolveStickyFlag(request))
        {
            contentStruct["sticky"] = true;
        }

        var categoryTerms = request.Destination.CategoryIds
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .ToArray();

        var tagTerms = request.Tags
            .Split([',', ';', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var terms = new Dictionary<string, object>(StringComparer.Ordinal);
        var termNames = new Dictionary<string, object>(StringComparer.Ordinal);

        if (categoryTerms.Length > 0)
        {
            if (categoryTerms.All(value => int.TryParse(value, out _)))
            {
                terms["category"] = categoryTerms.Select(int.Parse).Cast<object>().ToArray();
            }
            else
            {
                termNames["category"] = categoryTerms.Cast<object>().ToArray();
            }
        }

        if (tagTerms.Length > 0)
        {
            termNames["post_tag"] = tagTerms.Cast<object>().ToArray();
        }

        if (terms.Count > 0)
        {
            contentStruct["terms"] = terms;
        }

        if (termNames.Count > 0)
        {
            contentStruct["terms_names"] = termNames;
        }

        var customFields = request.CustomFields
            .Where(field => !field.Name.StartsWith("wordpress:", StringComparison.OrdinalIgnoreCase))
            .Select(field => new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["key"] = field.Name,
                ["value"] = field.Value
            })
            .Cast<object>()
            .ToArray();

        if (customFields.Length > 0)
        {
            contentStruct["custom_fields"] = customFields;
        }

        return contentStruct;
    }

    private static string ResolvePostStatus(CmsPublishRequest request)
    {
        return TryReadBooleanCustomField(request, "wordpress:draft", out var draft) && draft
            ? "draft"
            : "publish";
    }

    private static string ResolveCommentStatus(CmsPublishRequest request)
    {
        return TryReadBooleanCustomField(request, "wordpress:comment_status", out var commentsOpen)
            ? (commentsOpen ? "open" : "closed")
            : "open";
    }

    private static bool ResolveStickyFlag(CmsPublishRequest request)
    {
        return TryReadBooleanCustomField(request, "wordpress:sticky", out var sticky) && sticky;
    }

    private static bool TryReadBooleanCustomField(CmsPublishRequest request, string name, out bool value)
    {
        var customValue = request.CustomFields.FirstOrDefault(field =>
            string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase))?.Value;

        if (string.IsNullOrWhiteSpace(customValue))
        {
            value = false;
            return false;
        }

        if (bool.TryParse(customValue, out value))
        {
            return true;
        }

        if (string.Equals(customValue, "1", StringComparison.Ordinal))
        {
            value = true;
            return true;
        }

        if (string.Equals(customValue, "0", StringComparison.Ordinal))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    private static CmsPublishResult? ValidateRequest(CmsPublishRequest request)
    {
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

        return null;
    }

    private static CmsPublishResult ValidationFailed(string message)
    {
        return new CmsPublishResult(
            PublisherId: "cms.wordpress",
            Status: CmsPublishStatus.ValidationFailed,
            ArticleId: null,
            ArticleUrl: string.Empty,
            Message: message);
    }

    private static CmsPublishStatus MapFaultStatus(int faultCode)
    {
        return faultCode switch
        {
            401 or 403 => CmsPublishStatus.AuthenticationFailed,
            404 => CmsPublishStatus.Rejected,
            _ => CmsPublishStatus.Failed
        };
    }

    private static Uri BuildXmlRpcEndpoint(string websiteBaseUrl)
    {
        return websiteBaseUrl.EndsWith("xmlrpc.php", StringComparison.OrdinalIgnoreCase)
            ? new Uri(websiteBaseUrl, UriKind.Absolute)
            : new Uri(new Uri(websiteBaseUrl, UriKind.Absolute), "xmlrpc.php");
    }

    private static string NormalizeWebsiteBaseUrl(string website)
    {
        var normalized = website.Trim();
        if (!normalized.Contains("://", StringComparison.Ordinal))
        {
            normalized = "https://" + normalized;
        }

        if (normalized.EndsWith("xmlrpc.php", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[..^"xmlrpc.php".Length];
        }

        if (!normalized.EndsWith("/", StringComparison.Ordinal))
        {
            normalized += "/";
        }

        return normalized;
    }

    private static string BuildArticleUrl(string websiteBaseUrl, int articleId)
    {
        return $"{websiteBaseUrl}?p={articleId}";
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        client.DefaultRequestHeaders.UserAgent.ParseAdd("IntelligeN/0.3 (+https://github.com)");
        return client;
    }

    private sealed class WordPressXmlRpcFaultException : Exception
    {
        public WordPressXmlRpcFaultException(int faultCode, string message)
            : base(message)
        {
            FaultCode = faultCode;
        }

        public int FaultCode { get; }
    }
}

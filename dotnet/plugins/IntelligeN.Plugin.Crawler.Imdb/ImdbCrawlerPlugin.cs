using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using IntelligeN.SDK.Crawlers;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Crawler.Imdb;

public sealed class ImdbCrawlerPlugin : PluginBase, ICrawlerPlugin
{
    private const string BaseUrl = "https://www.imdb.com/";

    private static readonly string[] SupportedControls =
    [
        "IPicture",
        "ICreator",
        "IDirector",
        "IPublisher",
        "IGenre",
        "IRuntime",
        "IDescription",
        "INotes",
        "IReleaseDate"
    ];

    private static readonly HttpClient HttpClient = CreateHttpClient();

    public override PluginDescriptor Descriptor { get; } = new(
        Id: "crawler.imdb",
        DisplayName: "IMDb",
        Kind: PluginKind.Crawler,
        Version: "0.1.0",
        Description: "Movie metadata crawler using IMDb search plus title metadata extraction.");

    public CrawlerProfile Profile { get; } = new(
        SupportedTemplateTypes: ["Movie"],
        SupportedControlIds: SupportedControls,
        Notes: "Best-effort IMDb crawler for movie metadata. Search is title-driven and extracts JSON-LD plus company credits.");

    public async ValueTask<CrawlerResult> CrawlAsync(
        CrawlerRequest request,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<CrawlerSuggestion>();
        var searchQuery = BuildSearchQuery(request);
        if (string.IsNullOrWhiteSpace(searchQuery))
        {
            return new CrawlerResult(Descriptor.Id, suggestions);
        }

        try
        {
            var searchHtml = await GetStringAsync(
                $"find/?q={Uri.EscapeDataString(searchQuery)}&s=tt&ttype=ft&ref_=fn_ft",
                cancellationToken);

            var titleId = ExtractTitleId(searchHtml);
            if (string.IsNullOrWhiteSpace(titleId))
            {
                return new CrawlerResult(Descriptor.Id, suggestions);
            }

            var titleHtml = await GetStringAsync($"title/{titleId}/", cancellationToken);
            var metadata = ParseTitleMetadata(titleHtml);

            ImdbCompanyCredits? companyCredits = null;
            if (NeedsCompanyCredits(request))
            {
                var companyCreditsHtml = await GetStringAsync($"title/{titleId}/companycredits/", cancellationToken);
                companyCredits = ParseCompanyCredits(companyCreditsHtml);
            }

            AddIfMissing(suggestions, request, "IPicture", metadata.ImageUrl, "Resolved from IMDb title metadata.", 0.88d);
            AddIfMissing(suggestions, request, "IDirector", metadata.Directors, "Resolved from IMDb director credits.", 0.86d);
            AddIfMissing(suggestions, request, "IGenre", metadata.Genres, "Resolved from IMDb genre metadata.", 0.85d);
            AddIfMissing(suggestions, request, "IRuntime", metadata.RuntimeMinutes, "Resolved from IMDb runtime metadata.", 0.84d);
            AddIfMissing(suggestions, request, "IDescription", metadata.Description, "Resolved from IMDb plot summary.", 0.82d);
            AddIfMissing(suggestions, request, "INotes", metadata.Description, "Mirrored from IMDb plot summary for the legacy notes field.", 0.66d);
            AddIfMissing(suggestions, request, "IReleaseDate", metadata.ReleaseDate, "Resolved from IMDb release metadata.", 0.83d);

            if (companyCredits is not null)
            {
                AddIfMissing(suggestions, request, "ICreator", companyCredits.ProductionCompanies, "Resolved from IMDb production companies.", 0.78d);
                AddIfMissing(suggestions, request, "IPublisher", companyCredits.Distributors, "Resolved from IMDb distributors.", 0.74d);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new CrawlerResult(Descriptor.Id, suggestions);
        }

        return new CrawlerResult(Descriptor.Id, suggestions);
    }

    private static HttpClient CreateHttpClient()
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(BaseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IntelligeN", "0.1"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("(AvaloniaMigrationCrawler)"));
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("en-US,en;q=0.9");
        return client;
    }

    private static bool NeedsCompanyCredits(CrawlerRequest request)
    {
        return string.IsNullOrWhiteSpace(request.GetFieldValue("ICreator")) ||
               string.IsNullOrWhiteSpace(request.GetFieldValue("IPublisher"));
    }

    private static async Task<string> GetStringAsync(string relativeUrl, CancellationToken cancellationToken)
    {
        using var response = await HttpClient.GetAsync(relativeUrl, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static void AddIfMissing(
        ICollection<CrawlerSuggestion> suggestions,
        CrawlerRequest request,
        string controlId,
        string value,
        string reason,
        double confidence)
    {
        if (string.IsNullOrWhiteSpace(value) || !string.IsNullOrWhiteSpace(request.GetFieldValue(controlId)))
        {
            return;
        }

        suggestions.Add(new CrawlerSuggestion(controlId, value, reason, confidence));
    }

    private static string BuildSearchQuery(CrawlerRequest request)
    {
        var title = request.GetFieldValue("ITitle");
        if (!string.IsNullOrWhiteSpace(title))
        {
            return NormalizeWhitespace(title);
        }

        var releaseName = request.ReleaseName;
        if (string.IsNullOrWhiteSpace(releaseName))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(releaseName, @"[._]+", " ");
        cleaned = Regex.Replace(cleaned, @"\b(19|20)\d{2}\b.*$", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\b(1080p|720p|2160p|x264|x265|h264|h265|bluray|bdrip|webrip|webdl|german|dl|dts|ac3)\b", string.Empty, RegexOptions.IgnoreCase);
        cleaned = NormalizeWhitespace(cleaned);
        return cleaned;
    }

    private static string ExtractTitleId(string searchHtml)
    {
        var match = Regex.Match(searchHtml, "/title/(?<id>tt\\d+)/", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["id"].Value : string.Empty;
    }

    private static ImdbTitleMetadata ParseTitleMetadata(string titleHtml)
    {
        foreach (var jsonBlock in ExtractJsonLdBlocks(titleHtml))
        {
            var metadata = TryParseJsonLdMetadata(jsonBlock);
            if (metadata is not null)
            {
                return metadata;
            }
        }

        return ImdbTitleMetadata.Empty;
    }

    private static IReadOnlyList<string> ExtractJsonLdBlocks(string html)
    {
        var matches = Regex.Matches(
            html,
            "<script[^>]*type=\"application/ld\\+json\"[^>]*>(?<json>.*?)</script>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        return matches
            .Select(match => WebUtility.HtmlDecode(match.Groups["json"].Value).Trim())
            .Where(json => !string.IsNullOrWhiteSpace(json))
            .ToArray();
    }

    private static ImdbTitleMetadata? TryParseJsonLdMetadata(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            foreach (var candidate in EnumerateJsonLdCandidates(document.RootElement))
            {
                if (!IsMovieCandidate(candidate))
                {
                    continue;
                }

                return new ImdbTitleMetadata(
                    ImageUrl: ReadImageUrl(candidate),
                    Directors: ReadNames(candidate, "director"),
                    Genres: ReadStringList(candidate, "genre"),
                    RuntimeMinutes: ReadRuntimeMinutes(candidate),
                    Description: NormalizeWhitespace(ReadString(candidate, "description")),
                    ReleaseDate: ReadString(candidate, "datePublished"));
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }

    private static IEnumerable<JsonElement> EnumerateJsonLdCandidates(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("@graph", out var graph) && graph.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in graph.EnumerateArray())
                {
                    yield return item;
                }

                yield break;
            }

            yield return root;
            yield break;
        }

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in root.EnumerateArray())
            {
                yield return item;
            }
        }
    }

    private static bool IsMovieCandidate(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var typeElement))
        {
            return false;
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return string.Equals(typeElement.GetString(), "Movie", StringComparison.OrdinalIgnoreCase);
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            return typeElement.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String &&
                string.Equals(item.GetString(), "Movie", StringComparison.OrdinalIgnoreCase));
        }

        return false;
    }

    private static string ReadImageUrl(JsonElement element)
    {
        if (!element.TryGetProperty("image", out var imageElement))
        {
            return string.Empty;
        }

        if (imageElement.ValueKind == JsonValueKind.String)
        {
            return imageElement.GetString() ?? string.Empty;
        }

        if (imageElement.ValueKind == JsonValueKind.Object)
        {
            return ReadString(imageElement, "url");
        }

        if (imageElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in imageElement.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String)
                {
                    return item.GetString() ?? string.Empty;
                }

                if (item.ValueKind == JsonValueKind.Object)
                {
                    var url = ReadString(item, "url");
                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
            }
        }

        return string.Empty;
    }

    private static string ReadNames(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        var names = new List<string>();
        CollectNames(property, names);
        return JoinDistinct(names);
    }

    private static void CollectNames(JsonElement element, ICollection<string> names)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var name = ReadString(element, "name");
                if (!string.IsNullOrWhiteSpace(name))
                {
                    names.Add(name);
                }

                if (element.TryGetProperty("itemListElement", out var itemList))
                {
                    CollectNames(itemList, names);
                }

                break;
            }
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectNames(item, names);
                }

                break;
            case JsonValueKind.String:
                if (!string.IsNullOrWhiteSpace(element.GetString()))
                {
                    names.Add(element.GetString()!);
                }

                break;
        }
    }

    private static string ReadStringList(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return NormalizeWhitespace(property.GetString() ?? string.Empty);
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var values = property.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => NormalizeWhitespace(item.GetString() ?? string.Empty))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();

        return JoinDistinct(values);
    }

    private static string ReadRuntimeMinutes(JsonElement element)
    {
        var duration = ReadString(element, "duration");
        if (string.IsNullOrWhiteSpace(duration))
        {
            return string.Empty;
        }

        var match = Regex.Match(duration, "^PT(?:(?<hours>\\d+)H)?(?:(?<minutes>\\d+)M)?$", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return string.Empty;
        }

        var hours = match.Groups["hours"].Success ? int.Parse(match.Groups["hours"].Value) : 0;
        var minutes = match.Groups["minutes"].Success ? int.Parse(match.Groups["minutes"].Value) : 0;
        var totalMinutes = (hours * 60) + minutes;
        return totalMinutes > 0 ? totalMinutes.ToString(CultureInfo.InvariantCulture) : string.Empty;
    }

    private static string ReadString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return property.GetString() ?? string.Empty;
    }

    private static ImdbCompanyCredits ParseCompanyCredits(string companyCreditsHtml)
    {
        return new ImdbCompanyCredits(
            ProductionCompanies: ExtractCompanyList(companyCreditsHtml, "production"),
            Distributors: ExtractCompanyList(companyCreditsHtml, "distributor"));
    }

    private static string ExtractCompanyList(string html, string bucket)
    {
        var pattern = bucket switch
        {
            "production" => "(?is)(production\\s+companies|production\\s+company).*?(?<names>(?:/company/co\\d+.*?</a>)+)",
            "distributor" => "(?is)(distributors|distributor).*?(?<names>(?:/company/co\\d+.*?</a>)+)",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(pattern))
        {
            return string.Empty;
        }

        var sectionMatch = Regex.Match(html, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!sectionMatch.Success)
        {
            return string.Empty;
        }

        var names = Regex.Matches(sectionMatch.Groups["names"].Value, ">(?<name>[^<>]+)</a>", RegexOptions.IgnoreCase)
            .Select(match => NormalizeWhitespace(WebUtility.HtmlDecode(match.Groups["name"].Value)))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        return JoinDistinct(names);
    }

    private static string JoinDistinct(IEnumerable<string> values)
    {
        return string.Join("; ", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value ?? string.Empty, "\\s+", " ").Trim();
    }

    private sealed record ImdbTitleMetadata(
        string ImageUrl,
        string Directors,
        string Genres,
        string RuntimeMinutes,
        string Description,
        string ReleaseDate)
    {
        public static ImdbTitleMetadata Empty { get; } = new(
            ImageUrl: string.Empty,
            Directors: string.Empty,
            Genres: string.Empty,
            RuntimeMinutes: string.Empty,
            Description: string.Empty,
            ReleaseDate: string.Empty);
    }

    private sealed record ImdbCompanyCredits(
        string ProductionCompanies,
        string Distributors);
}

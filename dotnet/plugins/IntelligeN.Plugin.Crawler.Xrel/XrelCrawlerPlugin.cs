using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using IntelligeN.SDK.Crawlers;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Crawler.Xrel;

public sealed class XrelCrawlerPlugin : PluginBase, ICrawlerPlugin
{
    private const string ApiBaseUrl = "https://xrel-api.nfos.to/v2/";
    private const string SiteBaseUrl = "https://www.xrel.to/";

    private static readonly string[] SupportedControls =
    [
        "ITitle",
        "IPicture",
        "IReleaseDate",
        "IGenre",
        "IRuntime",
        "IAudioStream",
        "IVideoStream",
        "IDescription",
        "INotes",
        "IDirector",
        "ISample"
    ];

    private static readonly HttpClient ApiHttpClient = CreateHttpClient(ApiBaseUrl, acceptJson: true);
    private static readonly HttpClient SiteHttpClient = CreateHttpClient(SiteBaseUrl, acceptJson: false);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public override PluginDescriptor Descriptor { get; } = new(
        Id: "crawler.xrel",
        DisplayName: "xREL",
        Kind: PluginKind.Crawler,
        Version: "0.1.0",
        Description: "Movie metadata crawler using xREL release data plus product metadata.");

    public CrawlerProfile Profile { get; } = new(
        SupportedTemplateTypes: ["Movie"],
        SupportedControlIds: SupportedControls,
        Notes: "Movie-focused xREL crawler. Uses the xREL API and falls back to the product page for runtime and director.");

    public async ValueTask<CrawlerResult> CrawlAsync(
        CrawlerRequest request,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<CrawlerSuggestion>();
        var missingControls = BuildMissingControlSet(request);
        if (missingControls.Count == 0)
        {
            return new CrawlerResult(Descriptor.Id, suggestions);
        }

        var release = await ResolveReleaseAsync(request, cancellationToken);
        if (release is null)
        {
            return new CrawlerResult(Descriptor.Id, suggestions);
        }

        XrelExtInfoInfoDto? extInfo = null;
        if (!string.IsNullOrWhiteSpace(release.ExtInfoId) && NeedsExtInfo(missingControls))
        {
            extInfo = await TryGetJsonAsync<XrelExtInfoInfoDto>(
                ApiHttpClient,
                $"ext_info/info.json?id={Uri.EscapeDataString(release.ExtInfoId)}",
                cancellationToken);
        }

        XrelMediaEntryDto? poster = null;
        if (missingControls.Contains("IPicture") &&
            string.IsNullOrWhiteSpace(extInfo?.CoverUrl) &&
            !string.IsNullOrWhiteSpace(release.ExtInfoId))
        {
            poster = await GetPosterAsync(release.ExtInfoId, cancellationToken);
        }

        var plot = ExtractPlot(extInfo);
        var htmlDetails = XrelHtmlDetails.Empty;
        if (NeedsHtmlDetails(missingControls, plot))
        {
            var productLink = FirstNonEmpty(extInfo?.LinkHref, release.ExtInfoLinkHref);
            if (!string.IsNullOrWhiteSpace(productLink))
            {
                var productHtml = await TryGetStringAsync(SiteHttpClient, productLink, cancellationToken);
                if (!string.IsNullOrWhiteSpace(productHtml))
                {
                    htmlDetails = ParseHtmlDetails(productHtml);
                }
            }
        }

        var sceneReleaseDate = FormatSceneReleaseDate(release.Time);
        var productReleaseDate = SelectBestProductReleaseDate(extInfo?.ReleaseDates);
        var title = FirstNonEmpty(extInfo?.Title, release.ExtInfoTitle);
        var picture = FirstNonEmpty(extInfo?.CoverUrl, poster?.UrlFull);
        var genre = NormalizeDelimitedList(extInfo?.Genre);
        plot = FirstNonEmpty(plot, htmlDetails.Description);

        AddIfMissing(suggestions, request, "ITitle", title, "Resolved from xREL product metadata.", 0.91d);
        AddIfMissing(suggestions, request, "IPicture", picture, "Resolved from xREL cover art metadata.", 0.89d);
        AddIfMissing(
            suggestions,
            request,
            "IReleaseDate",
            FirstNonEmpty(sceneReleaseDate, productReleaseDate),
            !string.IsNullOrWhiteSpace(sceneReleaseDate)
                ? "Resolved from the xREL release timestamp."
                : "Resolved from xREL product release metadata.",
            !string.IsNullOrWhiteSpace(sceneReleaseDate) ? 0.9d : 0.76d);
        AddIfMissing(suggestions, request, "IGenre", genre, "Resolved from xREL genre metadata.", 0.86d);
        AddIfMissing(suggestions, request, "IRuntime", htmlDetails.RuntimeMinutes, "Resolved from the xREL product page runtime.", 0.84d);
        AddIfMissing(suggestions, request, "IAudioStream", NormalizeWhitespace(release.AudioType), "Resolved from xREL release info.", 0.88d);
        AddIfMissing(suggestions, request, "IVideoStream", NormalizeWhitespace(release.VideoType), "Resolved from xREL release info.", 0.88d);
        AddIfMissing(suggestions, request, "IDescription", plot, "Resolved from xREL plot metadata.", 0.82d);
        AddIfMissing(suggestions, request, "INotes", plot, "Mirrored from xREL plot metadata for the legacy notes field.", 0.66d);
        AddIfMissing(suggestions, request, "IDirector", htmlDetails.Directors, "Resolved from the xREL product page credits.", 0.83d);
        AddIfMissing(suggestions, request, "ISample", release.ProofUrl ?? string.Empty, "Resolved from the xREL proof image.", 0.78d);

        return new CrawlerResult(Descriptor.Id, suggestions);
    }

    private static HttpClient CreateHttpClient(string baseUrl, bool acceptJson)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri(baseUrl),
            Timeout = TimeSpan.FromSeconds(15)
        };

        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("IntelligeN", "0.1"));
        client.DefaultRequestHeaders.AcceptLanguage.ParseAdd("de-DE,de;q=0.9,en;q=0.7");

        if (acceptJson)
        {
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        }

        return client;
    }

    private static HashSet<string> BuildMissingControlSet(CrawlerRequest request)
    {
        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var controlId in SupportedControls)
        {
            if (string.IsNullOrWhiteSpace(request.GetFieldValue(controlId)))
            {
                missing.Add(controlId);
            }
        }

        return missing;
    }

    private static bool NeedsExtInfo(IReadOnlySet<string> missingControls)
    {
        return missingControls.Contains("ITitle") ||
               missingControls.Contains("IPicture") ||
               missingControls.Contains("IReleaseDate") ||
               missingControls.Contains("IGenre") ||
               missingControls.Contains("IDescription") ||
               missingControls.Contains("INotes") ||
               missingControls.Contains("IRuntime") ||
               missingControls.Contains("IDirector");
    }

    private static bool NeedsHtmlDetails(IReadOnlySet<string> missingControls, string plot)
    {
        return missingControls.Contains("IRuntime") ||
               missingControls.Contains("IDirector") ||
               ((missingControls.Contains("IDescription") || missingControls.Contains("INotes")) &&
                string.IsNullOrWhiteSpace(plot));
    }

    private static async Task<XrelReleaseDto?> ResolveReleaseAsync(
        CrawlerRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.ReleaseName))
        {
            var exactRelease = await TryGetJsonAsync<XrelReleaseDto>(
                ApiHttpClient,
                $"release/info.json?dirname={Uri.EscapeDataString(request.ReleaseName)}",
                cancellationToken);

            if (IsValidRelease(exactRelease))
            {
                return exactRelease;
            }
        }

        foreach (var query in BuildSearchQueries(request))
        {
            var searchResponse = await TryGetJsonAsync<XrelSearchResponseDto>(
                ApiHttpClient,
                $"search/releases.json?q={Uri.EscapeDataString(query)}&limit=5",
                cancellationToken);

            var match = SelectBestRelease(searchResponse?.Results, request.ReleaseName, request.GetFieldValue("ITitle"));
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private static IEnumerable<string> BuildSearchQueries(CrawlerRequest request)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var yieldReturnBuffer = new List<string>();

        void Add(string value)
        {
            var normalized = NormalizeWhitespace(value);
            if (!string.IsNullOrWhiteSpace(normalized) && seen.Add(normalized))
            {
                yieldReturnBuffer.Add(normalized);
            }
        }

        Add(request.ReleaseName);
        Add(request.GetFieldValue("ITitle"));
        Add(CleanReleaseNameForSearch(request.ReleaseName));

        return yieldReturnBuffer;
    }

    private static string CleanReleaseNameForSearch(string releaseName)
    {
        if (string.IsNullOrWhiteSpace(releaseName))
        {
            return string.Empty;
        }

        var cleaned = Regex.Replace(releaseName, @"[._]+", " ");
        cleaned = Regex.Replace(cleaned, @"-[^-]+$", string.Empty);
        cleaned = Regex.Replace(cleaned, @"\b(19|20)\d{2}\b.*$", string.Empty);
        cleaned = Regex.Replace(
            cleaned,
            @"\b(2160p|1080p|720p|480p|x264|x265|h264|h265|bluray|blu-ray|bdrip|brrip|webrip|webdl|web-dl|dvdrip|dvdr|retail|german|english|multi|dl|dts|dtshd|ac3|aac|hdr|dv|proper|repack|remux)\b",
            string.Empty,
            RegexOptions.IgnoreCase);

        return NormalizeWhitespace(cleaned);
    }

    private static XrelReleaseDto? SelectBestRelease(
        IReadOnlyList<XrelReleaseDto>? results,
        string releaseName,
        string title)
    {
        return results?
            .Where(IsValidRelease)
            .OrderByDescending(result => ScoreRelease(result, releaseName, title))
            .ThenByDescending(result => result.Time ?? 0)
            .FirstOrDefault();
    }

    private static int ScoreRelease(XrelReleaseDto release, string releaseName, string title)
    {
        var score = 0;
        var normalizedDirname = NormalizeSearchText(release.Dirname);
        var normalizedReleaseName = NormalizeSearchText(releaseName);
        var normalizedResultTitle = NormalizeSearchText(release.ExtInfo?.Title);
        var normalizedTitle = NormalizeSearchText(title);

        if (!string.IsNullOrWhiteSpace(normalizedReleaseName))
        {
            if (string.Equals(normalizedDirname, normalizedReleaseName, StringComparison.OrdinalIgnoreCase))
            {
                score += 1000;
            }
            else if (normalizedDirname.Contains(normalizedReleaseName, StringComparison.OrdinalIgnoreCase))
            {
                score += 250;
            }
        }

        if (!string.IsNullOrWhiteSpace(normalizedTitle))
        {
            if (string.Equals(normalizedResultTitle, normalizedTitle, StringComparison.OrdinalIgnoreCase))
            {
                score += 400;
            }
            else if (normalizedResultTitle.Contains(normalizedTitle, StringComparison.OrdinalIgnoreCase) ||
                     normalizedTitle.Contains(normalizedResultTitle, StringComparison.OrdinalIgnoreCase))
            {
                score += 150;
            }
        }

        return score;
    }

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var normalized = Regex.Replace(value, @"[._-]+", " ");
        return NormalizeWhitespace(normalized);
    }

    private static bool IsValidRelease(XrelReleaseDto? release)
    {
        return !string.IsNullOrWhiteSpace(release?.Dirname);
    }

    private static async Task<XrelMediaEntryDto?> GetPosterAsync(
        string extInfoId,
        CancellationToken cancellationToken)
    {
        var mediaEntries = await TryGetJsonAsync<List<XrelMediaEntryDto>>(
            ApiHttpClient,
            $"ext_info/media.json?id={Uri.EscapeDataString(extInfoId)}",
            cancellationToken);

        return mediaEntries?
            .Where(entry =>
                string.Equals(entry.Type, "image", StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(entry.UrlFull))
            .OrderBy(entry => string.Equals(entry.Description, "Poster", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .FirstOrDefault();
    }

    private static XrelHtmlDetails ParseHtmlDetails(string html)
    {
        return new XrelHtmlDetails(
            RuntimeMinutes: ExtractRuntimeMinutes(html),
            Directors: ExtractDirectors(html),
            Description: ExtractDescription(html));
    }

    private static string ExtractRuntimeMinutes(string html)
    {
        var runtimeTitle = ExtractMatchValue(
            html,
            @"Laufzeit:</div>\s*<div class=""l_right"" title=""(?<value>[^""]+)""");
        var match = Regex.Match(runtimeTitle, @"(?<minutes>\d+)");
        return match.Success ? match.Groups["minutes"].Value : string.Empty;
    }

    private static string ExtractDirectors(string html)
    {
        var directorBlock = ExtractMatchValue(
            html,
            @"Regisseur:\s*</div>(?<value>.*?)<div class=""clear""></div>");
        if (string.IsNullOrWhiteSpace(directorBlock))
        {
            return string.Empty;
        }

        var names = Regex.Matches(directorBlock, @"<a [^>]+>(?<name>[^<]+)</a>", RegexOptions.IgnoreCase)
            .Select(match => NormalizeWhitespace(WebUtility.HtmlDecode(match.Groups["name"].Value)))
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToArray();

        return JoinDistinct(names);
    }

    private static string ExtractDescription(string html)
    {
        var articleBody = ExtractMatchValue(
            html,
            @"<div class=""article_text""[^>]*>\s*(?<value>.*?)(?:<p>Quelle:|</div>\s*<div class=""sub_bar"")");
        return SanitizeHtmlFragment(articleBody);
    }

    private static string ExtractMatchValue(string input, string pattern)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var match = Regex.Match(input, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? match.Groups["value"].Value : string.Empty;
    }

    private static string ExtractPlot(XrelExtInfoInfoDto? extInfo)
    {
        if (extInfo?.Externals is null)
        {
            return string.Empty;
        }

        return extInfo.Externals
            .Select(external => SanitizeHtmlFragment(external.Plot))
            .FirstOrDefault(plot => !string.IsNullOrWhiteSpace(plot)) ?? string.Empty;
    }

    private static string SanitizeHtmlFragment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var sanitized = value;
        sanitized = Regex.Replace(sanitized, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"</p>", "\n", RegexOptions.IgnoreCase);
        sanitized = Regex.Replace(sanitized, @"<[^>]+>", " ");
        sanitized = WebUtility.HtmlDecode(sanitized);
        return NormalizeWhitespace(sanitized);
    }

    private static string FormatSceneReleaseDate(long? unixTimestamp)
    {
        if (unixTimestamp is null || unixTimestamp <= 0)
        {
            return string.Empty;
        }

        return DateTimeOffset.FromUnixTimeSeconds(unixTimestamp.Value)
            .ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static string SelectBestProductReleaseDate(IReadOnlyList<XrelProductReleaseDateDto>? releaseDates)
    {
        if (releaseDates is null || releaseDates.Count == 0)
        {
            return string.Empty;
        }

        return releaseDates
            .Where(entry => !string.IsNullOrWhiteSpace(entry.Date))
            .OrderBy(entry => GetProductReleaseDatePriority(entry.Type))
            .Select(entry => entry.Date ?? string.Empty)
            .FirstOrDefault(date => DateOnly.TryParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            ?? string.Empty;
    }

    private static int GetProductReleaseDatePriority(string? type)
    {
        return type?.ToLowerInvariant() switch
        {
            "de-retail" => 0,
            "de-hd" => 1,
            "de-cine" => 2,
            _ => 10
        };
    }

    private static string NormalizeDelimitedList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var values = Regex.Split(WebUtility.HtmlDecode(value), @"\s*[,/;|]\s*")
            .Select(NormalizeWhitespace)
            .Where(item => !string.IsNullOrWhiteSpace(item));

        return JoinDistinct(values);
    }

    private static string JoinDistinct(IEnumerable<string> values)
    {
        return string.Join("; ", values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase));
    }

    private static string NormalizeWhitespace(string? value)
    {
        return Regex.Replace(value ?? string.Empty, @"\s+", " ").Trim();
    }

    private static string FirstNonEmpty(params string?[] values)
    {
        return values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
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

    private static async Task<T?> TryGetJsonAsync<T>(
        HttpClient client,
        string relativeOrAbsoluteUrl,
        CancellationToken cancellationToken)
        where T : class
    {
        try
        {
            using var response = await client.GetAsync(relativeOrAbsoluteUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<T>(responseStream, JsonOptions, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<string> TryGetStringAsync(
        HttpClient client,
        string relativeOrAbsoluteUrl,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(relativeOrAbsoluteUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            return await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return string.Empty;
        }
    }

    private sealed record XrelHtmlDetails(
        string RuntimeMinutes,
        string Directors,
        string Description)
    {
        public static XrelHtmlDetails Empty { get; } = new(
            RuntimeMinutes: string.Empty,
            Directors: string.Empty,
            Description: string.Empty);
    }

    private sealed class XrelSearchResponseDto
    {
        [JsonPropertyName("results")]
        public IReadOnlyList<XrelReleaseDto>? Results { get; init; }
    }

    private sealed class XrelReleaseDto
    {
        [JsonPropertyName("dirname")]
        public string? Dirname { get; init; }

        [JsonPropertyName("time")]
        public long? Time { get; init; }

        [JsonPropertyName("video_type")]
        public string? VideoType { get; init; }

        [JsonPropertyName("audio_type")]
        public string? AudioType { get; init; }

        [JsonPropertyName("proof_url")]
        public string? ProofUrl { get; init; }

        [JsonPropertyName("ext_info")]
        public XrelExtInfoReferenceDto? ExtInfo { get; init; }

        public string ExtInfoId => ExtInfo?.Id ?? string.Empty;

        public string ExtInfoTitle => ExtInfo?.Title ?? string.Empty;

        public string ExtInfoLinkHref => ExtInfo?.LinkHref ?? string.Empty;
    }

    private sealed class XrelExtInfoReferenceDto
    {
        [JsonPropertyName("id")]
        public string? Id { get; init; }

        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("link_href")]
        public string? LinkHref { get; init; }
    }

    private sealed class XrelExtInfoInfoDto
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("link_href")]
        public string? LinkHref { get; init; }

        [JsonPropertyName("genre")]
        public string? Genre { get; init; }

        [JsonPropertyName("cover_url")]
        public string? CoverUrl { get; init; }

        [JsonPropertyName("release_dates")]
        public IReadOnlyList<XrelProductReleaseDateDto>? ReleaseDates { get; init; }

        [JsonPropertyName("externals")]
        public IReadOnlyList<XrelExternalDto>? Externals { get; init; }
    }

    private sealed class XrelProductReleaseDateDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("date")]
        public string? Date { get; init; }
    }

    private sealed class XrelExternalDto
    {
        [JsonPropertyName("plot")]
        public string? Plot { get; init; }
    }

    private sealed class XrelMediaEntryDto
    {
        [JsonPropertyName("type")]
        public string? Type { get; init; }

        [JsonPropertyName("description")]
        public string? Description { get; init; }

        [JsonPropertyName("url_full")]
        public string? UrlFull { get; init; }
    }
}

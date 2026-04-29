using System.Text.RegularExpressions;
using IntelligeN.SDK.Crawlers;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Crawler.ReleaseName;

public sealed class ReleaseNameCrawlerPlugin : PluginBase, ICrawlerPlugin
{
    private static readonly string[] SupportedControls =
    [
        "ITitle",
        "ILanguage",
        "INotes",
        "IAudioStream",
        "IVideoCodec",
        "IVideoStream",
        "IVideoSystem"
    ];

    public override PluginDescriptor Descriptor { get; } = new(
        Id: "crawler.releasename",
        DisplayName: "ReleaseName",
        Kind: PluginKind.Crawler,
        Version: "0.1.0",
        Description: "Legacy-inspired release name parser for title and technical metadata.");

    public CrawlerProfile Profile { get; } = new(
        SupportedTemplateTypes: [],
        SupportedControlIds: SupportedControls,
        Notes: "Ports the old Releasename crawler pattern for title, language, notes and stream metadata.");

    public ValueTask<CrawlerResult> CrawlAsync(
        CrawlerRequest request,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<CrawlerSuggestion>();
        var releaseName = request.ReleaseName;
        if (string.IsNullOrWhiteSpace(releaseName))
        {
            return ValueTask.FromResult(new CrawlerResult(Descriptor.Id, suggestions));
        }

        var templateType = request.TemplateType;

        AddIfMissing(suggestions, request, "ITitle", DetectTitle(releaseName, templateType), "Parsed from release name structure.", 0.72d);
        AddIfMissing(suggestions, request, "ILanguage", DetectLanguage(releaseName, templateType), "Detected from language tags in release name.", 0.82d);
        AddIfMissing(suggestions, request, "INotes", DetectNotes(releaseName, templateType), "Detected from scene tags in release name.", 0.70d);
        AddIfMissing(suggestions, request, "IAudioStream", DetectAudioStream(releaseName), "Detected from audio tags in release name.", 0.86d);
        AddIfMissing(suggestions, request, "IVideoCodec", DetectVideoCodec(releaseName), "Detected from codec tags in release name.", 0.86d);
        AddIfMissing(suggestions, request, "IVideoStream", DetectVideoStream(releaseName), "Detected from source tags in release name.", 0.84d);
        AddIfMissing(suggestions, request, "IVideoSystem", DetectVideoSystem(releaseName, templateType), "Detected from region or video-system tags in release name.", 0.76d);

        return ValueTask.FromResult(new CrawlerResult(Descriptor.Id, suggestions));
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

    private static string DetectTitle(string releaseName, string templateType)
    {
        var title = releaseName;

        title = ReplaceDominantSeparator(title);
        title = RemoveGroupName(title);

        if (IsAudio(templateType))
        {
            title = NormalizeAudioTitle(title);
        }

        title = TrimReleaseMetadata(title);
        title = title.Trim(' ', '-', '.', '_');

        if (!IsSoftware(templateType))
        {
            title = Regex.Replace(title, @"\s(19|20)\d{2}$", string.Empty).Trim();
        }

        title = Regex.Replace(title, @"(?<=\d)\s(?=\d)", ".");
        return ReduceWhitespace(title);
    }

    private static string DetectLanguage(string releaseName, string templateType)
    {
        var lower = releaseName.ToLowerInvariant();
        if (ContainsSceneToken(lower, "subbed"))
        {
            return string.Empty;
        }

        if (IsMovie(templateType) && lower.Contains("german", StringComparison.Ordinal) && ContainsSceneToken(lower, "dl"))
        {
            return "german;english";
        }

        if (lower.Contains("german", StringComparison.Ordinal))
        {
            return "german";
        }

        if (lower.Contains("english", StringComparison.Ordinal))
        {
            return "english";
        }

        if (lower.Contains("spanish", StringComparison.Ordinal))
        {
            return "spanish";
        }

        if (lower.Contains("japanese", StringComparison.Ordinal) || (IsXxx(templateType) && lower.Contains("jav", StringComparison.Ordinal)))
        {
            return "japanese";
        }

        return "english";
    }

    private static string DetectNotes(string releaseName, string templateType)
    {
        var lower = releaseName.ToLowerInvariant();

        if (ContainsAnySceneToken(lower, ["uncut"]))
        {
            return "uncut";
        }

        if (ContainsAnySceneToken(lower, ["unrated"]))
        {
            return "unrated";
        }

        if (ContainsAnySceneToken(lower, ["dc", "directors.cut"]))
        {
            return "director's cut";
        }

        if (IsMovie(templateType) && ContainsAnySceneToken(lower, ["extended"]))
        {
            return "extended";
        }

        if (IsMovie(templateType) && ContainsAnySceneToken(lower, ["theatrical"]))
        {
            return "theatrical";
        }

        return string.Empty;
    }

    private static string DetectAudioStream(string releaseName)
    {
        var lower = releaseName.ToLowerInvariant();

        if (ContainsAnySceneToken(lower, ["mic", "md", "ac3md"]))
        {
            return "mic";
        }

        if (ContainsAnySceneToken(lower, ["line", "ld", "ac3ld"]))
        {
            return "line";
        }

        if (ContainsAnySceneToken(lower, ["aac"]))
        {
            return "aac";
        }

        if (ContainsAnySceneToken(lower, ["ac3", "ac3d", "ac3.dubbed", "dd51", "dd5.1"]))
        {
            return "ac3";
        }

        if (ContainsAnySceneToken(lower, ["dts", "dtsd", "dtshd", "dts-hd", "bluray"]))
        {
            return "dts";
        }

        return "ac3";
    }

    private static string DetectVideoCodec(string releaseName)
    {
        var lower = releaseName.ToLowerInvariant();

        if (ContainsAnySceneToken(lower, ["xvid"]))
        {
            return "xvid";
        }

        if (ContainsAnySceneToken(lower, ["divx"]))
        {
            return "divx";
        }

        if (ContainsAnySceneToken(lower, ["x264", "h264"]))
        {
            return "x264";
        }

        if (ContainsAnySceneToken(lower, ["x265", "h265"]))
        {
            return "x265";
        }

        if (ContainsAnySceneToken(lower, ["dvdr"]))
        {
            return "dvdr";
        }

        if (ContainsAnySceneToken(lower, ["complete.bluray"]))
        {
            return "vc-1";
        }

        if (ContainsAnySceneToken(lower, ["svcd"]))
        {
            return "svcd";
        }

        if (ContainsAnySceneToken(lower, ["vcd"]))
        {
            return "vcd";
        }

        return string.Empty;
    }

    private static string DetectVideoStream(string releaseName)
    {
        var lower = releaseName.ToLowerInvariant();

        if (ContainsAnySceneToken(lower, ["cam", "hdcam"]))
        {
            return "cam";
        }

        if (ContainsAnySceneToken(lower, ["bluray"]))
        {
            return "bluray";
        }

        if (ContainsAnySceneToken(lower, ["bdrip", "bd.rip", "bd-rip", "hdrip", "hd.rip", "hd-rip"]))
        {
            return "bdrip";
        }

        if (ContainsAnySceneToken(lower, ["bdscr", "bd.scr", "bd-scr", "hdscr", "hd.scr", "hd-scr"]))
        {
            return "bdscr";
        }

        if (ContainsAnySceneToken(lower, ["dvd5", "dvdr"]))
        {
            return "dvd5";
        }

        if (ContainsAnySceneToken(lower, ["dvd9"]))
        {
            return "dvd9";
        }

        if (ContainsAnySceneToken(lower, ["dvdrip"]))
        {
            return "dvdrip";
        }

        if (ContainsAnySceneToken(lower, ["dvdscr", "dvd-scr"]))
        {
            return "dvdscr";
        }

        if (ContainsAnySceneToken(lower, ["ppv", "ppvrip"]))
        {
            return "ppvrip";
        }

        foreach (var region in new[] { "r1", "r2", "r3", "r4", "r5", "r6" })
        {
            if (ContainsSceneToken(lower, region))
            {
                return region;
            }
        }

        if (ContainsAnySceneToken(lower, ["scr", "screener"]))
        {
            return "scr";
        }

        if (ContainsAnySceneToken(lower, ["tc", "telecine"]))
        {
            return "tc";
        }

        if (ContainsAnySceneToken(lower, ["ts", "telesync", "hdts", "hd-ts"]))
        {
            return "ts";
        }

        if (ContainsAnySceneToken(lower, ["hdtv"]))
        {
            return "hdtv";
        }

        if (ContainsAnySceneToken(lower, ["vhs"]))
        {
            return "vhs";
        }

        if (ContainsAnySceneToken(lower, ["webdl", "web-dl"]))
        {
            return "webdl";
        }

        if (ContainsAnySceneToken(lower, ["webrip", "web.rip", "web-rip"]))
        {
            return "webrip";
        }

        if (ContainsAnySceneToken(lower, ["workprint"]))
        {
            return "workprint";
        }

        return string.Empty;
    }

    private static string DetectVideoSystem(string releaseName, string templateType)
    {
        var lower = releaseName.ToLowerInvariant();

        if (ContainsAnySceneToken(lower, ["pal"]))
        {
            return "pal";
        }

        if (ContainsAnySceneToken(lower, ["ntsc"]))
        {
            return "ntsc";
        }

        if (ContainsAnySceneToken(lower, ["secam"]))
        {
            return "secam";
        }

        if (IsConsole(templateType))
        {
            if (ContainsAnySceneToken(lower, ["rf"]))
            {
                return "rf";
            }

            if (ContainsAnySceneToken(lower, ["eur", "ger", "fra"]))
            {
                return "eur";
            }

            if (ContainsAnySceneToken(lower, ["usa"]))
            {
                return "usa";
            }

            if (ContainsAnySceneToken(lower, ["jpn", "jap"]))
            {
                return "jpn";
            }
        }

        return string.Empty;
    }

    private static string ReplaceDominantSeparator(string value)
    {
        var underscoreCount = value.Count(character => character == '_');
        var dotCount = value.Count(character => character == '.');

        return underscoreCount > dotCount
            ? value.Replace('_', ' ')
            : value.Replace('.', ' ');
    }

    private static string RemoveGroupName(string value)
    {
        var position = value.LastIndexOf('-');
        return position > 0 ? value[..position] : value;
    }

    private static string NormalizeAudioTitle(string value)
    {
        var normalized = value;
        var firstHyphen = normalized.IndexOf('-', StringComparison.Ordinal);
        if (firstHyphen >= 0)
        {
            var chars = normalized.ToCharArray();
            for (var index = firstHyphen + 1; index < chars.Length; index++)
            {
                if (chars[index] == '-')
                {
                    chars[index] = ' ';
                }
            }

            normalized = new string(chars);
            normalized = normalized[..firstHyphen].TrimEnd() + " - " + normalized[(firstHyphen + 1)..].TrimStart();
        }

        if (normalized.StartsWith("VA - ", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[5..];
        }
        else if (normalized.StartsWith("VA--", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[4..];
        }
        else if (normalized.StartsWith("VA-", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[3..];
        }

        return normalized;
    }

    private static string TrimReleaseMetadata(string value)
    {
        var tokens = ReduceWhitespace(value).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (tokens.Count == 0)
        {
            return string.Empty;
        }

        var titleTokens = new List<string>();
        foreach (var token in tokens)
        {
            var normalized = token.Trim();
            if (Regex.IsMatch(normalized, @"^(19|20)\d{2}$") || IsMetadataToken(normalized))
            {
                break;
            }

            titleTokens.Add(normalized);
        }

        return string.Join(" ", titleTokens);
    }

    private static bool IsMetadataToken(string token)
    {
        return CommonMetadataTokens.Contains(token.ToLowerInvariant());
    }

    private static readonly HashSet<string> CommonMetadataTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "german", "english", "spanish", "japanese", "jav", "dl", "subbed", "uncut", "unrated", "extended", "theatrical",
        "xvid", "divx", "x264", "x265", "h264", "h265", "aac", "ac3", "dts", "dd51", "dd5.1", "mic", "line",
        "bluray", "bdrip", "bdscr", "dvdr", "dvd5", "dvd9", "dvdrip", "dvdscr", "cam", "hdcam", "tc", "telecine",
        "ts", "telesync", "hdtv", "vhs", "webdl", "web-dl", "webrip", "workprint", "pal", "ntsc", "secam",
        "720p", "1080p", "2160p", "svcd", "vcd", "complete", "r1", "r2", "r3", "r4", "r5", "r6"
    };

    private static bool ContainsAnySceneToken(string input, IEnumerable<string> candidates)
    {
        return candidates.Any(candidate => ContainsSceneToken(input, candidate));
    }

    private static bool ContainsSceneToken(string input, string token)
    {
        return Regex.IsMatch(input, $@"(^|[._\-\s]){Regex.Escape(token)}($|[._\-\s])", RegexOptions.IgnoreCase);
    }

    private static string ReduceWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static bool IsMovie(string templateType)
    {
        return templateType.Equals("Movie", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsXxx(string templateType)
    {
        return templateType.Equals("XXX", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsAudio(string templateType)
    {
        return templateType.Equals("Audio", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSoftware(string templateType)
    {
        return templateType.Equals("Software", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsConsole(string templateType)
    {
        return templateType.Equals("Console", StringComparison.OrdinalIgnoreCase) ||
               templateType.Equals("Game", StringComparison.OrdinalIgnoreCase);
    }
}

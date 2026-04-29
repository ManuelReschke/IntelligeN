using System.Text.RegularExpressions;
using IntelligeN.SDK.Crawlers;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.Plugin.Crawler.Example;

public sealed class ExampleCrawlerPlugin : PluginBase, ICrawlerPlugin
{
    public override PluginDescriptor Descriptor { get; } = new(
        Id: "crawler.example",
        DisplayName: "Example Crawler",
        Kind: PluginKind.Crawler,
        Version: "0.2.0",
        Description: "Reference crawler plugin for validating runtime autofill in the new SDK.");

    public CrawlerProfile Profile { get; } = new(
        SupportedTemplateTypes: ["Movie"],
        SupportedControlIds: ["IGenre", "IPicture", "INotes", "IReleaseDate"],
        Notes: "Example movie crawler using a tiny built-in title catalog.");

    public ValueTask<CrawlerResult> CrawlAsync(
        CrawlerRequest request,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<CrawlerSuggestion>();
        var releaseName = request.ReleaseName;
        var title = request.GetFieldValue("ITitle");
        var titleHint = string.IsNullOrWhiteSpace(title) ? releaseName : title;

        AddIfMissing(
            suggestions,
            request,
            "IGenre",
            DetectGenre(titleHint),
            "Derived from example crawler title matching.",
            0.73d);

        AddIfMissing(
            suggestions,
            request,
            "IPicture",
            DetectPicture(titleHint),
            "Resolved from the example crawler's small title catalog.",
            0.66d);

        AddIfMissing(
            suggestions,
            request,
            "INotes",
            DetectNotes(titleHint),
            "Resolved from the example crawler's small title catalog.",
            0.62d);

        AddIfMissing(
            suggestions,
            request,
            "IReleaseDate",
            DetectReleaseDate(titleHint, releaseName),
            "Resolved from the example crawler's title match or release year fallback.",
            0.58d);

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

    private static string DetectGenre(string titleHint)
    {
        if (Contains(titleHint, "spectre") || Contains(titleHint, "bond"))
        {
            return "Action / Thriller";
        }

        if (Contains(titleHint, "matrix"))
        {
            return "Action / Science Fiction";
        }

        if (Contains(titleHint, "batman"))
        {
            return "Action / Crime";
        }

        return string.Empty;
    }

    private static string DetectPicture(string titleHint)
    {
        if (Contains(titleHint, "spectre") || Contains(titleHint, "bond"))
        {
            return "https://m.media-amazon.com/images/M/example-spectre-crawler.jpg";
        }

        if (Contains(titleHint, "matrix"))
        {
            return "https://m.media-amazon.com/images/M/example-matrix-crawler.jpg";
        }

        return string.Empty;
    }

    private static string DetectNotes(string titleHint)
    {
        if (Contains(titleHint, "spectre") || Contains(titleHint, "bond"))
        {
            return "A cryptic message pulls Bond into the orbit of Spectre while he uncovers a wider conspiracy.";
        }

        if (Contains(titleHint, "matrix"))
        {
            return "A computer hacker discovers the reality he knows is an artificial simulation and joins the resistance.";
        }

        return string.Empty;
    }

    private static string DetectReleaseDate(string titleHint, string releaseName)
    {
        if (Contains(titleHint, "spectre") || Contains(titleHint, "bond"))
        {
            return "2015-11-05";
        }

        if (Contains(titleHint, "matrix"))
        {
            return "1999-03-31";
        }

        var yearMatch = Regex.Match(releaseName, "(19|20)\\d{2}");
        return yearMatch.Success ? $"{yearMatch.Value}-01-01" : string.Empty;
    }

    private static bool Contains(string input, string value)
    {
        return input.Contains(value, StringComparison.OrdinalIgnoreCase);
    }
}

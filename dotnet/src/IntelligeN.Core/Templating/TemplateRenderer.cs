using System.Text.RegularExpressions;

namespace IntelligeN.Core.Templating;

public sealed class TemplateRenderer
{
    private static readonly Regex PlaceholderRegex =
        new("{{\\s*(?<token>[A-Za-z0-9_.-]+)\\s*}}", RegexOptions.Compiled);

    private static readonly IReadOnlyDictionary<string, string> FriendlyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ReleaseName"] = "IReleaseName",
            ["Title"] = "ITitle",
            ["OriginalTitle"] = "ITitle",
            ["Picture"] = "IPicture",
            ["ReleaseDate"] = "IReleaseDate",
            ["Genre"] = "IGenre",
            ["Language"] = "ILanguage",
            ["Runtime"] = "IRuntime",
            ["VideoCodec"] = "IVideoCodec",
            ["VideoStream"] = "IVideoStream",
            ["AudioStream"] = "IAudioStream",
            ["Sample"] = "ISample",
            ["Notes"] = "INotes",
            ["Nfo"] = "INFO"
        };

    public TemplatePreview Render(
        ContentTemplateDefinition template,
        TemplateRenderContext context,
        IReadOnlyDictionary<string, string> fields)
    {
        var tokenMap = BuildTokenMap(context, fields);

        return new TemplatePreview(
            Subject: RenderTemplate(template.SubjectTemplate, tokenMap),
            Body: RenderTemplate(template.BodyTemplate, tokenMap),
            Tags: RenderTemplate(template.TagTemplate, tokenMap));
    }

    public IReadOnlyDictionary<string, string> BuildTokenMap(
        TemplateRenderContext context,
        IReadOnlyDictionary<string, string> fields)
    {
        var tokenMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TemplateType"] = context.TemplateType,
            ["TemplateFileName"] = context.TemplateFileName,
            ["XmlFileName"] = context.XmlFileName,
            ["ControlCount"] = context.ControlCount.ToString(),
            ["MirrorCount"] = context.MirrorCount.ToString(),
            ["DirectlinkCount"] = context.DirectlinkCount.ToString(),
            ["CrypterCount"] = context.CrypterCount.ToString(),
            ["GeneratedUtc"] = context.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'")
        };

        foreach (var field in fields)
        {
            tokenMap[field.Key] = field.Value;
        }

        foreach (var alias in FriendlyAliases)
        {
            if (fields.TryGetValue(alias.Value, out var value))
            {
                tokenMap[alias.Key] = value;
            }
        }

        return tokenMap;
    }

    private static string RenderTemplate(string template, IReadOnlyDictionary<string, string> tokenMap)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        return PlaceholderRegex.Replace(template, match =>
        {
            var token = match.Groups["token"].Value;
            return tokenMap.TryGetValue(token, out var value)
                ? value
                : string.Empty;
        }).Trim();
    }
}

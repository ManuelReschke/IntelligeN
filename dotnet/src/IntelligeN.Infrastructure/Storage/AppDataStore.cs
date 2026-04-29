using System.Text.Json;
using IntelligeN.Core.Publishing;
using IntelligeN.Core.Templating;

namespace IntelligeN.Infrastructure.Storage;

public sealed class AppDataStore
{
    private const string TemplatesFileName = "content-templates.json";
    private const string PublishTargetsFileName = "publish-targets.json";
    private const string PublishHistoryFileName = "publish-history.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public IReadOnlyList<ContentTemplateDefinition> LoadTemplates(string dataDirectory)
    {
        var filePath = Path.Combine(dataDirectory, TemplatesFileName);
        return File.Exists(filePath)
            ? LoadFile<ContentTemplateDefinition>(filePath)
            : CreateSeedTemplates();
    }

    public void SaveTemplates(string dataDirectory, IEnumerable<ContentTemplateDefinition> templates)
    {
        SaveFile(Path.Combine(dataDirectory, TemplatesFileName), templates);
    }

    public IReadOnlyList<PublishTargetProfile> LoadPublishTargets(string dataDirectory)
    {
        return LoadFile<PublishTargetProfile>(Path.Combine(dataDirectory, PublishTargetsFileName));
    }

    public void SavePublishTargets(string dataDirectory, IEnumerable<PublishTargetProfile> profiles)
    {
        SaveFile(Path.Combine(dataDirectory, PublishTargetsFileName), profiles);
    }

    public IReadOnlyList<PublishHistoryEntry> LoadPublishHistory(string dataDirectory)
    {
        return LoadFile<PublishHistoryEntry>(Path.Combine(dataDirectory, PublishHistoryFileName))
            .OrderByDescending(entry => entry.CreatedUtc)
            .ToArray();
    }

    public void SavePublishHistory(string dataDirectory, IEnumerable<PublishHistoryEntry> historyEntries)
    {
        SaveFile(Path.Combine(dataDirectory, PublishHistoryFileName),
            historyEntries.OrderByDescending(entry => entry.CreatedUtc));
    }

    private static IReadOnlyList<T> LoadFile<T>(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return [];
        }

        using var stream = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions) ?? [];
    }

    private static void SaveFile<T>(string filePath, IEnumerable<T> items)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        using var stream = File.Create(filePath);
        JsonSerializer.Serialize(stream, items.ToArray(), JsonOptions);
    }

    private static IReadOnlyList<ContentTemplateDefinition> CreateSeedTemplates()
    {
        return
        [
            new ContentTemplateDefinition(
                Id: "movie-article",
                Name: "Movie Article",
                TemplateType: "Movie",
                Category: "Editorial",
                Description: "Lange Artikelvorlage fuer Websites wie WordPress.",
                SubjectTemplate: "{{Title}}",
                BodyTemplate: """
                    {{Title}}

                    Genre: {{Genre}}
                    Sprache: {{Language}}
                    Laufzeit: {{Runtime}}
                    Video: {{VideoStream}} / {{VideoCodec}}
                    Audio: {{AudioStream}}
                    Bild: {{Picture}}

                    {{Notes}}

                    Download:
                    {{CrypterLinks}}

                    Directlinks:
                    {{DirectLinks}}
                    """,
                TagTemplate: "{{Genre}}, {{Language}}"),
            new ContentTemplateDefinition(
                Id: "movie-board",
                Name: "Movie Board",
                TemplateType: "Movie",
                Category: "Board",
                Description: "Kompakte Szene- und Board-Vorlage fuer Foren.",
                SubjectTemplate: "{{Title}} [{{VideoStream}} {{Language}}]",
                BodyTemplate: """
                    [b]{{Title}}[/b]

                    Release: {{ReleaseName}}
                    Genre: {{Genre}}
                    Sprache: {{Language}}
                    Laufzeit: {{Runtime}}
                    Video: {{VideoStream}} / {{VideoCodec}}
                    Audio: {{AudioStream}}
                    Poster: {{Picture}}

                    {{Notes}}

                    [b]Download[/b]
                    {{CrypterLinks}}

                    [b]Directlinks[/b]
                    {{DirectLinks}}
                    """,
                TagTemplate: "{{Genre}}, {{Language}}"),
            new ContentTemplateDefinition(
                Id: "generic-release",
                Name: "Generic Release",
                TemplateType: string.Empty,
                Category: "General",
                Description: "Neutraler Fallback fuer neue Templates und unbekannte Kategorien.",
                SubjectTemplate: "{{Title}}",
                BodyTemplate: """
                    {{Title}}

                    Release: {{ReleaseName}}
                    Template: {{TemplateType}}
                    Mirrors: {{MirrorSummary}}

                    {{Notes}}

                    {{DownloadLinks}}
                    """,
                TagTemplate: "{{Genre}}")
        ];
    }
}

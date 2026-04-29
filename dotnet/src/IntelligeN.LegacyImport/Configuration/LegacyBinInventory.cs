namespace IntelligeN.LegacyImport.Configuration;

public sealed class LegacyBinInventory
{
    public IReadOnlyList<HosterDefinition> LoadHostersFile(string hosterFile) => LoadHosters(hosterFile);

    public void SaveHostersFile(string hosterFile, IEnumerable<HosterDefinition> hosters)
    {
        var document = new System.Xml.Linq.XDocument(
            new System.Xml.Linq.XDeclaration("1.0", "utf-8", null),
            new System.Xml.Linq.XElement(
                "hoster",
                hosters
                    .OrderBy(hoster => hoster.Name, StringComparer.OrdinalIgnoreCase)
                    .Select(hoster =>
                        new System.Xml.Linq.XElement(
                            "hoster",
                            new System.Xml.Linq.XAttribute("name", hoster.Name),
                            new System.Xml.Linq.XAttribute("short", hoster.ShortName),
                            hoster.Aliases.Select(alias => new System.Xml.Linq.XElement("alsoknownas", alias))))));

        Directory.CreateDirectory(Path.GetDirectoryName(hosterFile)!);

        var settings = new System.Xml.XmlWriterSettings
        {
            Indent = true,
            IndentChars = "  ",
            NewLineChars = Environment.NewLine,
            NewLineHandling = System.Xml.NewLineHandling.Replace,
            Encoding = new System.Text.UTF8Encoding(false)
        };

        using var writer = System.Xml.XmlWriter.Create(hosterFile, settings);
        document.Save(writer);
    }

    public LegacyConfigurationSnapshot LoadSnapshot(string configurationRoot)
    {
        return new LegacyConfigurationSnapshot(
            Files: EnumerateConfigurationFiles(configurationRoot),
            Hosters: LoadHosters(Path.Combine(configurationRoot, "hoster.xml")),
            CodeDefinitions: LoadCodeDefinitions(Path.Combine(configurationRoot, "codedef.xml")),
            Controls: LoadControls(Path.Combine(configurationRoot, "controls.xml")));
    }

    public IReadOnlyList<LegacyConfigurationSummary> EnumerateConfigurationFiles(string configurationRoot)
    {
        if (!Directory.Exists(configurationRoot))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(configurationRoot, "*.xml", SearchOption.TopDirectoryOnly)
            .Select(configurationFile =>
            {
                var document = System.Xml.Linq.XDocument.Load(configurationFile, System.Xml.Linq.LoadOptions.None);
                var root = document.Root;
                var directChildElementCount = root?.Elements().Count() ?? 0;
                var info = new FileInfo(configurationFile);

                return new LegacyConfigurationSummary(
                    FileName: Path.GetFileName(configurationFile),
                    RelativePath: Path.GetRelativePath(configurationRoot, configurationFile),
                    RootElement: root?.Name.LocalName ?? "<unknown>",
                    DirectChildElementCount: directChildElementCount,
                    SizeInBytes: info.Length);
            })
            .OrderBy(entry => entry.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyList<string> EnumerateRuntimeFiles(string binRoot)
    {
        if (!Directory.Exists(binRoot))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(binRoot, "*", SearchOption.AllDirectories)
            .Select(Path.GetFullPath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlyList<HosterDefinition> LoadHosters(string hosterFile)
    {
        if (!File.Exists(hosterFile))
        {
            return [];
        }

        var document = System.Xml.Linq.XDocument.Load(hosterFile, System.Xml.Linq.LoadOptions.None);

        return document.Root?
            .Elements("hoster")
            .Select(hoster => new HosterDefinition(
                Name: (string?)hoster.Attribute("name") ?? string.Empty,
                ShortName: (string?)hoster.Attribute("short") ?? string.Empty,
                Aliases: hoster.Elements("alsoknownas")
                    .Select(alias => alias.Value.Trim())
                    .Where(alias => alias.Length > 0)
                    .ToArray()))
            .OrderBy(hoster => hoster.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static IReadOnlyList<CodeDefinitionGroup> LoadCodeDefinitions(string codeDefinitionFile)
    {
        if (!File.Exists(codeDefinitionFile))
        {
            return [];
        }

        var document = System.Xml.Linq.XDocument.Load(codeDefinitionFile, System.Xml.Linq.LoadOptions.PreserveWhitespace);

        return document.Root?
            .Elements()
            .Select(group => new CodeDefinitionGroup(
                Name: group.Name.LocalName,
                Commands: group.Elements()
                    .Select(command => new CodeDefinitionCommand(
                        Name: command.Name.LocalName,
                        Parameter1: (string?)command.Attribute("param1"),
                        Parameter1Value: (string?)command.Attribute("param1value"),
                        Parameter2: (string?)command.Attribute("param2"),
                        Snippet: command.Value.Trim()))
                    .OrderBy(command => command.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()))
            .OrderBy(group => group.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static IReadOnlyList<ControlDefinitionSummary> LoadControls(string controlsFile)
    {
        if (!File.Exists(controlsFile))
        {
            return [];
        }

        var document = System.Xml.Linq.XDocument.Load(controlsFile, System.Xml.Linq.LoadOptions.None);

        return document.Root?
            .Elements()
            .Select(control => BuildControlSummary(control))
            .OrderBy(control => control.ControlName, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];
    }

    private static ControlDefinitionSummary BuildControlSummary(System.Xml.Linq.XElement control)
    {
        var templates = control.Elements("templatetype")
            .Select(template => new ControlTemplateSummary(
                TemplateName: (string?)template.Attribute("name") ?? string.Empty,
                Title: (string?)template.Element("title")?.Attribute("name") ?? string.Empty,
                Hint: (string?)template.Element("hint")?.Attribute("name") ?? string.Empty,
                DefaultValue: (string?)template.Element("value")?.Attribute("name") ?? string.Empty,
                ListItemCount: template.Elements("list").Count()))
            .ToArray();

        var defaultTemplate = templates.FirstOrDefault(template => template.TemplateName.Length == 0) ?? templates.FirstOrDefault();

        return new ControlDefinitionSummary(
            ControlName: control.Name.LocalName,
            TemplateCount: templates.Length,
            DefaultTitle: defaultTemplate?.Title ?? string.Empty,
            DefaultHint: defaultTemplate?.Hint ?? string.Empty,
            TotalListItemCount: templates.Sum(template => template.ListItemCount),
            Templates: templates);
    }
}

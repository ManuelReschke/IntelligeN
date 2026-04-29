using System.Xml.Linq;

namespace IntelligeN.LegacyImport.Workspace;

public sealed class IntelligenXmlWorkspaceLoader
{
    public LoadedXmlWorkspaceDocument Load(string filePath)
    {
        var document = XDocument.Load(filePath, LoadOptions.None);
        var root = document.Root ?? throw new InvalidDataException("XML root element is missing.");

        var templateTypeNode = root.Element("templatetype")
            ?? throw new InvalidDataException("templatetype node is missing.");

        var controls = root.Element("controls")?
            .Elements()
            .Select(control => new LoadedXmlControl(
                ControlId: control.Name.LocalName,
                Title: control.Element("title")?.Value ?? string.Empty,
                Value: control.Element("value")?.Value ?? string.Empty,
                ImageHosters: control.Element("hosters")?
                    .Elements("hoster")
                    .Select(hoster => new LoadedXmlImageHoster(
                        Name: (string?)hoster.Attribute("name") ?? string.Empty,
                        Value: hoster.Value))
                    .ToArray() ?? []))
            .OrderBy(control => control.ControlId, StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        var mirrors = root.Element("mirrors")?
            .Elements("mirror")
            .Select((mirror, index) => new LoadedXmlMirror(
                Index: index + 1,
                Directlinks: mirror.Elements("directlink")
                    .Select(directlink => new LoadedXmlDirectlink(
                        Hoster: (string?)directlink.Attribute("hoster") ?? string.Empty,
                        Status: (string?)directlink.Attribute("status") ?? string.Empty,
                        Size: (string?)directlink.Attribute("size") ?? string.Empty,
                        PartSize: (string?)directlink.Attribute("partsize") ?? string.Empty,
                        Parts: (string?)directlink.Attribute("parts") ?? string.Empty,
                        Urls: SplitLinks(directlink.Value)))
                    .ToArray(),
                Crypters: mirror.Elements("crypter")
                    .Select(crypter => new LoadedXmlCrypter(
                        Name: (string?)crypter.Attribute("name") ?? string.Empty,
                        Status: (string?)crypter.Attribute("status") ?? string.Empty,
                        Size: (string?)crypter.Attribute("size") ?? string.Empty,
                        PartSize: (string?)crypter.Attribute("partsize") ?? string.Empty,
                        Hoster: (string?)crypter.Attribute("hoster") ?? string.Empty,
                        Parts: (string?)crypter.Attribute("parts") ?? string.Empty,
                        StatusImage: (string?)crypter.Attribute("statusimage") ?? string.Empty,
                        StatusImageText: (string?)crypter.Attribute("statusimagetext") ?? string.Empty,
                        Url: crypter.Value.Trim()))
                    .Where(crypter => !string.IsNullOrWhiteSpace(crypter.Url))
                    .ToArray()))
            .ToArray() ?? [];

        return new LoadedXmlWorkspaceDocument(
            FilePath: Path.GetFullPath(filePath),
            TemplateType: templateTypeNode.Value.Trim(),
            TemplateFileName: (string?)templateTypeNode.Attribute("filename") ?? string.Empty,
            TemplateChecksum: (string?)templateTypeNode.Attribute("checksum") ?? string.Empty,
            Controls: controls,
            Mirrors: mirrors);
    }

    private static IReadOnlyList<string> SplitLinks(string value)
    {
        return value
            .Split(['\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .ToArray();
    }
}

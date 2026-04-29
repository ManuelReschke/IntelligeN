namespace IntelligeN.LegacyImport.Workspace;

public sealed record LoadedXmlWorkspaceDocument(
    string FilePath,
    string TemplateType,
    string TemplateFileName,
    string TemplateChecksum,
    IReadOnlyList<LoadedXmlControl> Controls,
    IReadOnlyList<LoadedXmlMirror> Mirrors)
{
    public string FileName => Path.GetFileName(FilePath);

    public int ControlCount => Controls.Count;

    public int MirrorCount => Mirrors.Count;

    public int DirectlinkCount => Mirrors.Sum(mirror => mirror.DirectlinkCount);

    public int DirectlinkUrlCount => Mirrors.Sum(mirror => mirror.DirectlinkUrlCount);

    public int CrypterCount => Mirrors.Sum(mirror => mirror.CrypterCount);

    public string GetControlValue(string controlId)
    {
        return Controls.FirstOrDefault(control =>
            string.Equals(control.ControlId, controlId, StringComparison.OrdinalIgnoreCase))?.Value ?? string.Empty;
    }
}

namespace IntelligeN.LegacyImport.Workspace;

public sealed record LoadedXmlControl(
    string ControlId,
    string Title,
    string Value,
    IReadOnlyList<LoadedXmlImageHoster> ImageHosters)
{
    public int ImageHosterCount => ImageHosters.Count;

    public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? ControlId : Title;

    public string ValuePreview
    {
        get
        {
            var normalized = Value.Replace("\r", " ").Replace("\n", " ").Trim();
            if (normalized.Length <= 120)
            {
                return normalized;
            }

            return normalized[..117] + "...";
        }
    }
}

namespace IntelligeN.LegacyImport.Workspace;

public sealed record LoadedXmlDirectlink(
    string Hoster,
    string Status,
    string Size,
    string PartSize,
    string Parts,
    IReadOnlyList<string> Urls)
{
    public int UrlCount => Urls.Count;

    public string LinksText => string.Join(Environment.NewLine, Urls);

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Hoster)
            ? "Direct links"
            : Hoster;

    public string Summary
    {
        get
        {
            var details = new[]
            {
                string.IsNullOrWhiteSpace(Status) ? string.Empty : $"Status: {Status}",
                string.IsNullOrWhiteSpace(Parts) ? string.Empty : $"Parts: {Parts}",
                UrlCount == 0 ? string.Empty : $"URLs: {UrlCount}"
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" | ", details);
        }
    }
}

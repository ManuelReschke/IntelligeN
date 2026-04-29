namespace IntelligeN.LegacyImport.Workspace;

public sealed record LoadedXmlCrypter(
    string Name,
    string Status,
    string Size,
    string PartSize,
    string Hoster,
    string Parts,
    string StatusImage,
    string StatusImageText,
    string Url)
{
    public string DisplayName =>
        string.IsNullOrWhiteSpace(Name)
            ? "Crypter"
            : Name;

    public string Summary
    {
        get
        {
            var details = new[]
            {
                string.IsNullOrWhiteSpace(Hoster) ? string.Empty : Hoster,
                string.IsNullOrWhiteSpace(Status) ? string.Empty : $"Status: {Status}",
                string.IsNullOrWhiteSpace(Parts) ? string.Empty : $"Parts: {Parts}"
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" | ", details);
        }
    }
}

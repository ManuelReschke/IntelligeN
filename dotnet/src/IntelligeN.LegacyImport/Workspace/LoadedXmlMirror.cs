namespace IntelligeN.LegacyImport.Workspace;

public sealed record LoadedXmlMirror(
    int Index,
    IReadOnlyList<LoadedXmlDirectlink> Directlinks,
    IReadOnlyList<LoadedXmlCrypter> Crypters)
{
    public int DirectlinkCount => Directlinks.Count;

    public int DirectlinkUrlCount => Directlinks.Sum(directlink => directlink.UrlCount);

    public int CrypterCount => Crypters.Count;

    public string DisplayName => $"Mirror {Index}";

    public string Summary =>
        $"{DirectlinkUrlCount} Directlink-URL(s), {CrypterCount} Crypter-Link(s)";
}

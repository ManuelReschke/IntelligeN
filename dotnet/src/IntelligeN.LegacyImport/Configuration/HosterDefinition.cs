namespace IntelligeN.LegacyImport.Configuration;

public sealed record HosterDefinition(
    string Name,
    string ShortName,
    IReadOnlyList<string> Aliases);

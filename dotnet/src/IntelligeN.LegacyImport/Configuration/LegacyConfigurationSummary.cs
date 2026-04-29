namespace IntelligeN.LegacyImport.Configuration;

public sealed record LegacyConfigurationSummary(
    string FileName,
    string RelativePath,
    string RootElement,
    int DirectChildElementCount,
    long SizeInBytes);

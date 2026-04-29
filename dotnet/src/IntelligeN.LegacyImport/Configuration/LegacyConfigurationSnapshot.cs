namespace IntelligeN.LegacyImport.Configuration;

public sealed record LegacyConfigurationSnapshot(
    IReadOnlyList<LegacyConfigurationSummary> Files,
    IReadOnlyList<HosterDefinition> Hosters,
    IReadOnlyList<CodeDefinitionGroup> CodeDefinitions,
    IReadOnlyList<ControlDefinitionSummary> Controls);

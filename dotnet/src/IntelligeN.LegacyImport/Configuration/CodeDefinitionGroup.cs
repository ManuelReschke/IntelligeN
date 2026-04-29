namespace IntelligeN.LegacyImport.Configuration;

public sealed record CodeDefinitionGroup(
    string Name,
    IReadOnlyList<CodeDefinitionCommand> Commands);

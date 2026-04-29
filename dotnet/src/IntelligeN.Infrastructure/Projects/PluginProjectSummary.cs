namespace IntelligeN.Infrastructure.Projects;

public sealed record PluginProjectSummary(
    string Name,
    string Category,
    string ProjectFile,
    string RelativePath,
    string OutputAssemblyPath,
    bool HasBuildOutput);

namespace IntelligeN.Infrastructure.Projects;

public sealed class PluginProjectInventory
{
    public IReadOnlyList<PluginProjectSummary> Enumerate(string pluginRootDirectory)
    {
        if (!Directory.Exists(pluginRootDirectory))
        {
            return [];
        }

        return Directory
            .EnumerateFiles(pluginRootDirectory, "*.csproj", SearchOption.AllDirectories)
            .Select(projectFile =>
            {
                var pluginDirectory = Path.GetDirectoryName(projectFile) ?? pluginRootDirectory;
                var pluginName = Path.GetFileNameWithoutExtension(projectFile);
                var relativePath = Path.GetRelativePath(pluginRootDirectory, pluginDirectory);
                var category = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
                var outputAssemblyPath = Path.Combine(pluginDirectory, "bin", "Debug", "net10.0", $"{pluginName}.dll");

                return new PluginProjectSummary(
                    Name: pluginName,
                    Category: category,
                    ProjectFile: projectFile,
                    RelativePath: relativePath,
                    OutputAssemblyPath: outputAssemblyPath,
                    HasBuildOutput: File.Exists(outputAssemblyPath));
            })
            .OrderBy(project => project.Category, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}

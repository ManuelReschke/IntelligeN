using System.Reflection;
using System.Runtime.Loader;
using IntelligeN.SDK.Plugins;

namespace IntelligeN.PluginHost.Loading;

public sealed class PluginCatalog
{
    public IReadOnlyList<PluginLoadResult> DiscoverFromDirectory(string pluginDirectory)
    {
        if (!Directory.Exists(pluginDirectory))
        {
            return [];
        }

        var results = new List<PluginLoadResult>();

        foreach (var assemblyPath in Directory.EnumerateFiles(pluginDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            results.Add(DiscoverFromAssemblyPath(assemblyPath));
        }

        return results;
    }

    public PluginLoadResult DiscoverFromAssemblyPath(string assemblyPath)
    {
        try
        {
            var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
            var plugins = CreatePlugins(assembly);
            return new PluginLoadResult(assemblyPath, plugins, null);
        }
        catch (Exception exception)
        {
            return new PluginLoadResult(assemblyPath, [], exception);
        }
    }

    private static IReadOnlyList<IPlugin> CreatePlugins(Assembly assembly)
    {
        var plugins = new List<IPlugin>();

        foreach (var type in assembly.DefinedTypes)
        {
            if (type.IsAbstract || type.IsInterface)
            {
                continue;
            }

            if (!typeof(IPlugin).IsAssignableFrom(type))
            {
                continue;
            }

            if (type.GetConstructor(Type.EmptyTypes) is null)
            {
                continue;
            }

            if (Activator.CreateInstance(type.AsType()) is IPlugin plugin)
            {
                plugins.Add(plugin);
            }
        }

        return plugins;
    }
}

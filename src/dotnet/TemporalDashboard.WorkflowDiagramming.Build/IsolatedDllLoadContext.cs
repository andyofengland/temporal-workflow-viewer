using System.Reflection;
using System.Runtime.Loader;

namespace TemporalDashboard.WorkflowDiagramming.Build;

/// <summary>
/// Isolated load context so the workflow assembly is loaded without polluting the build process.
/// Diagramming and Temporal types are resolved from the default context so attribute reflection matches.
/// </summary>
internal sealed class IsolatedDllLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public IsolatedDllLoadContext(string mainAssemblyPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var name = assemblyName.Name ?? "";
        if (name.StartsWith("TemporalDashboard.WorkflowDiagramming", StringComparison.OrdinalIgnoreCase) ||
            name.StartsWith("Temporalio.", StringComparison.OrdinalIgnoreCase))
            return null;

        var path = _resolver.ResolveAssemblyToPath(assemblyName);
        return path != null ? LoadFromAssemblyPath(path) : null;
    }
}

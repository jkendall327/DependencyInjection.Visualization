using DependencyInjection.Visualization;

namespace Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Generates a debug view of the service collection's dependency tree.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to analyze.</param>
    /// <returns>A string representation of the dependency tree, grouped by namespace.</returns>
    /// <remarks>
    /// This method creates a visual representation of all registered services and their dependencies.
    /// It's useful for debugging and understanding the structure of your dependency injection container.
    /// </remarks>
    public static string GetDebugView(this IServiceCollection services)
    {
        var tree = new DependencyTree(services);

        return tree.GenerateTreeView();
    }
}
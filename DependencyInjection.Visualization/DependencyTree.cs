using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

/// <summary>
/// Represents a tree of dependencies registered in an IServiceCollection.
/// </summary>
public class DependencyTree
{
    private readonly TreeBuilder _treeBuilder = new();
    private readonly TreeViewer _treeViewer = new();
    
    private readonly List<ServiceNode> _rootNodes;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to analyze.</param>
    public DependencyTree(IServiceCollection services)
    {
        _rootNodes = _treeBuilder.BuildTree(services);
    }
    
    /// <summary>
    /// Retrieves dependency chains that meet or exceed a specified depth.
    /// </summary>
    /// <param name="minDepth">The minimum depth of chains to include. This is 1-indexed, not 0-indexed.</param>
    /// <param name="onlyUserCode">If true, only includes types from the project's namespace.</param>
    /// <returns>An object containing the filtered chains and a tree view of their hierarchy.</returns>
    public DependencyChains GetRegistrationChainsByDepth(int minDepth, bool onlyUserCode = false)
    {
        var deepChains = new List<ServiceNode>();
        
        foreach (var rootNode in _rootNodes)
        {
            if (!onlyUserCode || TypeRelevance.IsRelevantType(rootNode.Descriptor.ServiceType))
            {
                ExploreChains(rootNode, [rootNode], minDepth, deepChains, onlyUserCode);
            }
        }

        var stringRepresentation = _treeViewer.GenerateTreeView(deepChains);
        
        return new(deepChains, stringRepresentation);
    }

    private void ExploreChains(ServiceNode node, List<ServiceNode> currentChain, int minDepth, List<ServiceNode> result, bool onlyUserCode)
    {
        if (currentChain.Count >= minDepth && !result.Contains(currentChain[0]))
        {
            result.Add(currentChain[0]);
        }

        foreach (var child in node.Dependencies)
        {
            if (!onlyUserCode || TypeRelevance.IsRelevantType(child.Descriptor.ServiceType))
            {
                var newChain = new List<ServiceNode>(currentChain) { child };
                ExploreChains(child, newChain, minDepth, result, onlyUserCode);
            }
        }
    }
    
    /// <summary>
    /// Generates a formatted string representation of the dependency tree.
    /// </summary>
    /// <param name="onlyUserCode">If true, only includes types from namespaces in the current project and its directly-referenced projects.</param>
    /// <returns>A string representation of the dependency tree, grouped by namespace.</returns>
    /// /// <example>
    /// Exemplar output when all services are included:
    /// <code>
    /// // Namespace: YourProject.Services
    /// // --------------------------------------------------
    /// // IFooService -> FooService (Singleton)
    /// // IBarService -> BarService (Scoped)
    /// // 
    /// // Namespace: Microsoft.Extensions.Http
    /// // --------------------------------------------------
    /// // IHttpClientFactory -> DefaultHttpClientFactory (Singleton)
    /// //   ILogger&lt;DefaultHttpClientFactory&gt; -> Logger&lt;DefaultHttpClientFactory&gt; (Singleton)
    /// //   IOptions&lt;HttpClientFactoryOptions&gt; -> OptionsManager&lt;HttpClientFactoryOptions&gt; (Singleton)
    /// // 
    /// // Namespace: Microsoft.Extensions.Logging
    /// // --------------------------------------------------
    /// // ILoggerFactory -> LoggerFactory (Singleton)
    /// //   IOptions&lt;LoggerFilterOptions&gt; -> OptionsManager&lt;LoggerFilterOptions&gt; (Singleton)
    /// //
    /// </code>
    /// 
    /// Exemplar output when only user code is analysed:
    /// <code>
    /// // Namespace: YourProject.Services
    /// // --------------------------------------------------
    /// // IFooService -> FooService (Singleton)
    /// // IBarService -> BarService (Scoped)
    /// </code>
    /// </example>
    public string GenerateTreeView(bool onlyUserCode = false)
    {
        return _treeViewer.GenerateTreeView(_rootNodes, onlyUserCode);
    }
}
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

/// <summary>
/// Represents a tree of dependencies registered in an IServiceCollection.
/// </summary>
public class DependencyTree
{
    private readonly TreeBuilder _treeBuilder = new();
    private readonly TreeViewer _treeViewer = new();
    private readonly DependencyUsageCalculator _usageCalculator = new();
    private readonly DepthAnalyser _depthAnalyser;
    
    private readonly List<ServiceNode> _rootNodes;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to analyze.</param>
    public DependencyTree(IServiceCollection services)
    {
        _depthAnalyser = new(_treeViewer);
        
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
        return _depthAnalyser.GetRegistrationChainsByDepth(_rootNodes, minDepth, onlyUserCode);
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
    
    /// <summary>
    /// Retrieves the most frequently used services in the dependency tree, that is, those which are requested by other services the most.
    /// </summary>
    /// <param name="count">The number of top services to return.</param>
    /// <returns>An enumerable of tuples containing the service type and its usage count.</returns>
    public List<(Type ServiceType, int UsageCount)> GetMostUsedServices(int count)
    {
        return _usageCalculator.GetMostUsedServices(_rootNodes, count);
    }
    
    /// <summary>
    /// Identifies services that are registered by your code but not used as dependencies by any other service.
    /// </summary>
    /// <returns>An enumerable of the unused services.</returns>
    public List<Type> GetUnusedServices()
    {
        return _usageCalculator.GetUnusedServices(_rootNodes);
    }
}
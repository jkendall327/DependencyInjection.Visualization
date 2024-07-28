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
    private readonly DotExporter _dotExporter = new();
    private readonly DepthAnalyser _depthAnalyser;
    
    private readonly List<ServiceNode> _rootNodes;

    /// <summary>
    /// If true, only types from namespace in the currently executing projects will be included in dependency analysis.
    /// </summary>
    public bool OnlyUserCode { get; }
    
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <remarks>The 'user code' spoken of in the bool argument more strictly means
    /// 'all the types with namespaces that start with the first portion of the name of the entry assembly'.
    /// Say your solution contains the Foobar.WebUI, Foobar.Core and Foobar.Tests projects.
    /// The tree will then only care about types starting with a Foobar.* namespace,
    /// such as Foobar.Utils.Http.MyCustomApiClient.</remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to analyze.</param>
    /// <param name="onlyUserCode">If true, only includes types from namespaces in the currently executing projects.</param>
    public DependencyTree(IServiceCollection services, bool onlyUserCode)
    {
        _depthAnalyser = new(_treeViewer);
        OnlyUserCode = onlyUserCode;
        
        _rootNodes = _treeBuilder.BuildTree(services);
    }
    
    /// <summary>
    /// Retrieves dependency chains that meet or exceed a specified depth.
    /// </summary>
    /// <param name="minDepth">The minimum depth of chains to include. This is 1-indexed, not 0-indexed.</param>
    /// <returns>An object containing the filtered chains and a string representation of their hierarchy.</returns>
    public DependencyChains GetRegistrationChainsByDepth(int minDepth)
    {
        return _depthAnalyser.GetRegistrationChainsByDepth(_rootNodes, minDepth, OnlyUserCode);
    }
    
    /// <summary>
    /// Generates a formatted string representation of the dependency tree.
    /// </summary>
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
    /// </code>
    /// </example>
    public string GenerateTreeView()
    {
        return _treeViewer.GenerateTreeView(_rootNodes, OnlyUserCode);
    }
    
    /// <summary>
    /// Retrieves the most frequently used services in the dependency tree, that is, those which are requested by other services the most.
    /// </summary>
    /// <param name="count">The number of top services to return.</param>
    /// <returns>A list of tuples containing the service type and its usage count.</returns>
    public List<(Type ServiceType, int UsageCount)> GetMostUsedServices(int count)
    {
        return _usageCalculator.GetMostUsedServices(_rootNodes, count, OnlyUserCode);
    }
    
    /// <summary>
    /// Identifies types registered in the container that aren't requested by any other service.
    /// </summary>
    /// <remarks>This includes cases where you register an interface (IFoobar), but only ever request concrete instances (Foobar).</remarks>
    /// <returns>A list of the unused services.</returns>
    public List<Type> GetUnusedServices()
    {
        return _usageCalculator.GetUnusedServices(_rootNodes);
    }

    /// <summary>
    /// Generates a DOT (Graph Description Language) representation of the dependency tree.
    /// </summary>
    /// <returns>A string containing the DOT graph description of the dependency tree.</returns>
    /// <remarks>
    /// The generated DOT graph can be used with graph visualization tools like Graphviz to create
    /// a visual representation of the dependency structure.
    /// This will likely be very busy unless <see cref="OnlyUserCode"/> is true.
    /// </remarks>
    public string GetDotGraph()
    {
        return _dotExporter.ExportToDot(_rootNodes, OnlyUserCode);
    }
}
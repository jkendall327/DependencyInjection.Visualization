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
    private readonly List<ServiceNode> _rootNodes;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to analyze.</param>
    /// <param name="assemblyNamePrefix">Optional prefix to filter relevant types. If null, it's determined automatically by calling <see cref="Assembly.GetExecutingAssembly"/>.</param>
    /// <remarks>You </remarks>
    /// <example></example>
    public DependencyTree(IServiceCollection services)
    {
        _rootNodes = _treeBuilder.BuildTree(services);
    }
    
    private Dictionary<Type, int>? _dependencyUsage;
    
    public Dictionary<Type, int> CalculateDependencyUsage(IEnumerable<ServiceNode> rootNodes)
    {
        if (_dependencyUsage is not null)
        {
            return _dependencyUsage;
        }
        
        var usageCount = new Dictionary<Type, int>();

        void TraverseNode(ServiceNode node)
        {
            foreach (var dependency in node.Dependencies)
            {
                var serviceType = dependency.Descriptor.ServiceType;
                usageCount[serviceType] = usageCount.TryGetValue(serviceType, out var count) ? count + 1 : 1;
                TraverseNode(dependency);
            }
        }

        foreach (var rootNode in rootNodes)
        {
            TraverseNode(rootNode);
        }

        _dependencyUsage = usageCount;
        
        return usageCount;
    }
    
    public IEnumerable<(Type ServiceType, int UsageCount)> GetMostUsedServices(IEnumerable<ServiceNode> rootNodes, int count)
    {
        var usageCount = CalculateDependencyUsage(rootNodes);
        
        return usageCount
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    public IEnumerable<Type> GetUnusedServices(IEnumerable<ServiceNode> rootNodes)
    {
        var usageCount = CalculateDependencyUsage(rootNodes);
        var allServices = rootNodes.Select(n => n.Descriptor.ServiceType);
        var unusedServices = allServices.Except(usageCount.Keys);

        return unusedServices.Where(TypeRelevance.IsRelevantType);
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

        var stringRepresentation = GenerateTreeView(deepChains);
        
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
        var nodes = _rootNodes;
        
        if (onlyUserCode)
        {
            nodes = nodes.Where(n => TypeRelevance.IsRelevantType(n.Descriptor.ServiceType)).ToList();
        }
        
        return GenerateTreeView(nodes);
    }

    private string GenerateTreeView(IEnumerable<ServiceNode> nodes)
    {
        var sb = new StringBuilder();
        var groupedNodes = nodes
            .GroupBy(n => n.Descriptor.ServiceType.Namespace)
            .OrderBy(g => g.Key);

        foreach (var group in groupedNodes)
        {
            sb.AppendLine($"Namespace: {group.Key}");
            sb.AppendLine(new('-', 50));

            foreach (var node in group)
            {
                AppendNodeAndDependencies(sb, node, 0);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void AppendNodeAndDependencies(StringBuilder sb, ServiceNode node, int depth)
    {
        sb.AppendLine($"{new string(' ', depth * 2)}{GetServiceDescription(node.Descriptor)}");
        
        foreach (var child in node.Dependencies)
        {
            AppendNodeAndDependencies(sb, child, depth + 1);
        }
    }

    private string GetServiceDescription(ServiceDescriptor descriptor)
    {
        var lifetime = descriptor.Lifetime.ToString();
        var serviceType = descriptor.ServiceType.Name;
        
        var implementation = GetImplementationDescription(descriptor);
        
        return $"{serviceType} -> {implementation} ({lifetime})";
    }
    
    private string GetImplementationDescription(ServiceDescriptor descriptor)
    {
        // For simple cases like .AddSingleton<IFoobar, Foobar>, return Foobar
        if (descriptor.ImplementationType != null)
        {
            return descriptor.ImplementationType.Name;
        }
    
        // When lambdas are used, like .AddSingleton<IFoobar>(x => new Foobar()), get the return type of the lambda
        if (descriptor.ImplementationFactory != null)
        {
            var factoryType = descriptor.ImplementationFactory.GetType();
            var methodInfo = factoryType.GetMethod("Invoke");
            
            if (methodInfo != null)
            {
                return methodInfo.ReturnType.Name;
            }
        }
    
        // When a premade object is used, like .AddSingleton<IFoobar>(foobar), return the type of the instance
        if (descriptor.ImplementationInstance != null)
        {
            return $"Instance of {descriptor.ImplementationInstance.GetType().Name}";
        }

        return "Unknown";
    }
}
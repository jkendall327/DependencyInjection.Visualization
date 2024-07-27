using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyTree;

public class DependencyTree
{
    private readonly TreeBuilder _treeBuilder = new();
    private readonly List<ServiceNode> _rootNodes;
    private readonly Lazy<string> _projectNamespacePrefix;

    public DependencyTree(IServiceCollection services, string? assemblyNamePrefix = null)
    {
        _projectNamespacePrefix = assemblyNamePrefix is null ? new(DetermineNamespacePrefix) : new(() => assemblyNamePrefix);
        _rootNodes = _treeBuilder.BuildTree(services);
    }
    
    private string DetermineNamespacePrefix()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var parts = assemblyName.Split('.');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }
    
    public DependencyChains GetRegistrationChainsByDepth(int minDepth, bool onlyUserCode = false)
    {
        var deepChains = new List<ServiceNode>();
        foreach (var rootNode in _rootNodes)
        {
            if (!onlyUserCode || IsRelevantType(rootNode.Descriptor.ServiceType))
            {
                ExploreChains(rootNode, new List<ServiceNode> { rootNode }, minDepth, deepChains, onlyUserCode);
            }
        }

        var stringRepresentation = GenerateTreeString(deepChains);
        return new DependencyChains(deepChains, stringRepresentation);
    }

    private void ExploreChains(ServiceNode node, List<ServiceNode> currentChain, int minDepth, List<ServiceNode> result, bool onlyUserCode)
    {
        if (currentChain.Count >= minDepth && !result.Contains(currentChain[0]))
        {
            result.Add(currentChain[0]);
        }

        foreach (var child in node.Dependencies)
        {
            if (!onlyUserCode || IsRelevantType(child.Descriptor.ServiceType))
            {
                var newChain = new List<ServiceNode>(currentChain) { child };
                ExploreChains(child, newChain, minDepth, result, onlyUserCode);
            }
        }
    }
    
    public string GenerateTreeString(bool onlyUserCode = false)
    {
        var nodes = _rootNodes;
        if (onlyUserCode)
        {
            nodes = nodes.Where(n => IsRelevantType(n.Descriptor.ServiceType)).ToList();
        }
        return GenerateTreeString(nodes);
    }

    private string GenerateTreeString(IEnumerable<ServiceNode> nodes)
    {
        var sb = new StringBuilder();
        var groupedNodes = nodes
            .GroupBy(n => n.Descriptor.ServiceType.Namespace)
            .OrderBy(g => g.Key);

        foreach (var group in groupedNodes)
        {
            sb.AppendLine($"Namespace: {group.Key}");
            sb.AppendLine(new string('-', 50));

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

    private bool IsRelevantType(Type type)
    {
        return type.Namespace != null && type.Namespace.StartsWith(_projectNamespacePrefix.Value);
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
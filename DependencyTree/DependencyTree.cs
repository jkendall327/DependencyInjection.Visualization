using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyTree;

public class DependencyTree
{
    private readonly IServiceCollection _services;
    private List<ServiceNode> _rootNodes;
    private string _projectNamespacePrefix;

    public DependencyTree(IServiceCollection services)
    {
        _services = services;
        _projectNamespacePrefix = DetermineNamespacePrefix();
        BuildTree();
    }
    
    private string DetermineNamespacePrefix()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var parts = assemblyName.Split('.');
        return parts.Length > 0 ? parts[0] : string.Empty;
    }

    private void BuildTree()
    {
        var serviceDescriptors = _services.ToList();
        _rootNodes = [];

        foreach (var descriptor in serviceDescriptors)
        {
            var node = new ServiceNode(descriptor);
            _rootNodes.Add(node);

            if (descriptor.ImplementationType != null)
            {
                BuildDependencies(node, descriptor.ImplementationType, serviceDescriptors, []);
            }
        }
    }

    private void BuildDependencies(ServiceNode parentNode,
        Type type,
        List<ServiceDescriptor> serviceDescriptors,
        HashSet<Type> visitedTypes)
    {
        if (!visitedTypes.Add(type)) return; // Prevents circular dependencies
        
        var constructor = GetConstructor(type, serviceDescriptors);
    
        if (constructor == null) return;

        foreach (var parameter in constructor.GetParameters())
        {
            var dependency = serviceDescriptors.FirstOrDefault(sd => sd.ServiceType == parameter.ParameterType);

            if (dependency != null)
            {
                var childNode = new ServiceNode(dependency);
                parentNode.Dependencies.Add(childNode);

                if (dependency.ImplementationType != null)
                {
                    BuildDependencies(childNode, dependency.ImplementationType, serviceDescriptors, [..visitedTypes]);
                }
            }
        }
    }

    private ConstructorInfo? GetConstructor(Type type, List<ServiceDescriptor> serviceDescriptors)
    {
        var publicConstructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length);

        foreach (var constructor in publicConstructors)
        {
            if (CanResolveConstructor(constructor, serviceDescriptors))
            {
                return constructor;
            }
        }

        // If no suitable public constructor is found, return null
        return null;
    }

    private bool CanResolveConstructor(ConstructorInfo constructor, List<ServiceDescriptor> serviceDescriptors)
    {
        return constructor.GetParameters().All(parameter => serviceDescriptors.Any(sd => sd.ServiceType == parameter.ParameterType));
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
        return type.Namespace != null && type.Namespace.StartsWith(_projectNamespacePrefix);
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
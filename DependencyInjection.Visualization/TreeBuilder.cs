using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

internal class TreeBuilder
{
    /// <summary>
    /// Builds a simple tree structure representing the dependencies between services.
    /// </summary>
    /// <param name="services">The collection of ServiceDescriptor objects to analyze.</param>
    /// <returns>A list of ServiceNode objects representing the root nodes of the dependency tree: that is, the services directly added to the <see cref="IServiceCollection"/>.</returns>
    /// <remarks>
    /// In the returned structure, each ServiceNode represents a service,
    /// and its Dependencies property contains the services it depends on.
    /// It handles circular dependencies to prevent infinite recursion.
    /// </remarks>
    public List<ServiceNode> BuildTree(IEnumerable<ServiceDescriptor> services)
    {
        var serviceDescriptors = services.ToList();
        
        List<ServiceNode> rootNodes = [];

        foreach (var descriptor in serviceDescriptors)
        {
            var node = new ServiceNode(descriptor);
            
            rootNodes.Add(node);

            if (descriptor.ImplementationType != null)
            {
                BuildDependencies(node, descriptor.ImplementationType, serviceDescriptors, []);
            }
        }

        return rootNodes;
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
}
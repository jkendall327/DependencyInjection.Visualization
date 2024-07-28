using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

internal class TreeBuilder
{
    /// <summary>
    /// Builds a simple tree representing the dependencies between services.
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

    // Examines the constructors of all the services in the DI container to determine their dependencies.
    private void BuildDependencies(ServiceNode parentNode,
        Type type,
        List<ServiceDescriptor> serviceDescriptors,
        HashSet<Type> visitedTypes)
    {
        // Prevents circular dependencies.
        if (!visitedTypes.Add(type)) return; 
        
        var constructor = GetConstructor(type, serviceDescriptors);
    
        if (constructor == null) return;

        foreach (var parameter in constructor.GetParameters())
        {
            var dependency = FindMatchingDescriptor(serviceDescriptors, parameter.ParameterType);

            if (dependency == null) continue;
            
            var childNode = new ServiceNode(dependency);
            parentNode.Dependencies.Add(childNode);

            // ImplementationType -> the DI container was passed a type argument.
            var implementationType = dependency.ImplementationType ?? 
                                     // The DI container was passed a pre-made instance.
                                     dependency.ImplementationInstance?.GetType() ??
                                     // The DI container was passed a factory lambda.
                                     dependency.ImplementationFactory?.Method.ReturnType;

            if (implementationType != null)
            {
                BuildDependencies(childNode, implementationType, serviceDescriptors, [..visitedTypes]);
            }
        }
    }
    
    
    private ServiceDescriptor? FindMatchingDescriptor(List<ServiceDescriptor> descriptors, Type serviceType)
    {
        return descriptors.FirstOrDefault(sd =>
        {
            if (sd.ServiceType == serviceType)
            {
                return true;
            }
            
            return sd.ServiceType.IsGenericTypeDefinition &&
                   serviceType.IsGenericType &&
                   serviceType.GetGenericTypeDefinition() == sd.ServiceType;
        });
    }

    /// <summary>
    /// We try here to filter out constructors that wouldn't be considered by Microsoft.Extensions.DependencyInjection.
    /// Namely we care about public constructors with parameters whose types are registered in the container.
    /// </summary>
    private ConstructorInfo? GetConstructor(Type type, List<ServiceDescriptor> serviceDescriptors)
    {
        var publicConstructors = type
            .GetConstructors(BindingFlags.Public | BindingFlags.Instance)
            .OrderByDescending(c => c.GetParameters().Length);

        return publicConstructors.FirstOrDefault(constructor => CanResolveConstructor(constructor, serviceDescriptors));
    }

    private bool CanResolveConstructor(ConstructorInfo constructor, List<ServiceDescriptor> serviceDescriptors)
    {
        // This could probably be made more efficient with a .Join, but I don't think it's worth it.
        return constructor
            .GetParameters()
            .All(parameter => serviceDescriptors.Any(sd => sd.ServiceType == parameter.ParameterType));
    }
}
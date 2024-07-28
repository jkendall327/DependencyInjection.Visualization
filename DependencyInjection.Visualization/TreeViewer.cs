using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

internal class TreeViewer
{
    public string GenerateTreeView(IEnumerable<ServiceNode> nodes, bool onlyUserCode = false)
    {
        if (onlyUserCode)
        {
            nodes = nodes.Where(n => TypeRelevance.IsUserType(n.Descriptor.ServiceType)).ToList();
        }
        
        var sb = new StringBuilder();
        
        var grouped = nodes
            .GroupBy(n => n.Descriptor.ServiceType.Namespace)
            .OrderBy(g => g.Key);

        foreach (var group in grouped)
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
        var description = GetServiceDescription(node.Descriptor);
        
        sb.AppendLine($"{new string(' ', depth * 2)}{description}");
        
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
    
    // TODO: open generics?
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
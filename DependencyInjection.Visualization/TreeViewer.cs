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
        
        var groupedNodes = nodes
            .GroupBy(n => n.Descriptor.ServiceType.Namespace)
            .OrderBy(g => g.Key);

        foreach (var group in groupedNodes)
        {
            sb.AppendLine($"Namespace: {group.Key}");
            sb.AppendLine(new string('─', 50));

            foreach (var node in group)
            {
                AppendNodeAndDependencies(sb, node, "", true);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void AppendNodeAndDependencies(StringBuilder sb, ServiceNode node, string prefix, bool isLast)
    {
        sb.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{GetServiceDescription(node.Descriptor)}");
    
        for (int i = 0; i < node.Dependencies.Count; i++)
        {
            var child = node.Dependencies[i];
            var childPrefix = prefix + (isLast ? "    " : "│   ");
            AppendNodeAndDependencies(sb, child, childPrefix, i == node.Dependencies.Count - 1);
        }
    }

    private string GetServiceDescription(ServiceDescriptor descriptor)
    {
        var lifetime = descriptor.Lifetime.ToString();
        var serviceType = FormatTypeName(descriptor.ServiceType);
        
        var implementation = GetImplementationDescription(descriptor);
        
        return $"{serviceType} -> {implementation} ({lifetime})";
    }
    
    private string GetImplementationDescription(ServiceDescriptor descriptor)
    {
        // For simple cases like .AddSingleton<IFoobar, Foobar>, return Foobar
        if (descriptor.ImplementationType != null)
        {
            return FormatTypeName(descriptor.ImplementationType);
        }
    
        // When lambdas are used, like .AddSingleton<IFoobar>(x => new Foobar()), get the return type of the lambda

        if (descriptor.ImplementationFactory != null)
        {
            var factoryType = descriptor.ImplementationFactory.GetType();
            var methodInfo = factoryType.GetMethod("Invoke");
            
            if (methodInfo != null)
            {
                return FormatTypeName(methodInfo.ReturnType);
            }
        }
    
        // When a premade object is used, like .AddSingleton<IFoobar>(foobar), return the type of the instance
        if (descriptor.ImplementationInstance != null)
        {
            return $"Instance of {FormatTypeName(descriptor.ImplementationInstance.GetType())}";
        }

        return "Unknown";
    }

    /// <summary>
    /// Handles generic types.
    /// </summary>
    private string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
        return $"{type.Name.Split('`')[0]}<{genericArgs}>";
    }
}
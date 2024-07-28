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
            sb.AppendLine(new('─', 50));

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
        var serviceDescription = GetServiceDescription(node);
        
        // The glyphs here, as well as the child stuff below, handles the indentation for dependencies to a service.
        sb.AppendLine($"{prefix}{(isLast ? "└── " : "├── ")}{serviceDescription}");
    
        for (int i = 0; i < node.Dependencies.Count; i++)
        {
            var child = node.Dependencies[i];
            var childPrefix = prefix + (isLast ? "    " : "│   ");
        
            AppendNodeAndDependencies(sb, child, childPrefix, i == node.Dependencies.Count - 1);
        }
    }

    private string GetServiceDescription(ServiceNode node)
    {
        var lifetime = node.Lifetime.ToString();
        
        var serviceType = node.ServiceTypeName;
        
        var implementation = node.GetImplementationDescription();
        
        // If the service was registered 'simply', e.g. services.AddSingleton<Foobar>(),
        // there's no point naming the type twice as both service and implementation.
        return serviceType == implementation
            ? $"{serviceType} ({lifetime})"
            : $"{serviceType} -> {implementation} ({lifetime})";
    }
}
using System.Text;

namespace DependencyInjection.Visualization;

internal class DotExporter
{
    public string ExportToDot(IEnumerable<ServiceNode> nodes, bool onlyUserCode)
    {
        if (onlyUserCode)
        {
            nodes = nodes.Where(x => TypeRelevance.IsUserType(x.Descriptor.ServiceType));
        }
        
        var sb = new StringBuilder();
        sb.AppendLine("digraph DependencyTree {");

        foreach (var node in nodes)
        {
            ExportNode(node, sb);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private void ExportNode(ServiceNode node, StringBuilder sb)
    {
        var nodeName = SanitizeNodeName(node.ServiceTypeName);
        sb.AppendLine($"  {nodeName} [label=\"{node.ServiceTypeName}\"];");

        foreach (var dependency in node.Dependencies)
        {
            var depName = SanitizeNodeName(dependency.ServiceTypeName);
            sb.AppendLine($"  {nodeName} -> {depName};");
            ExportNode(dependency, sb);
        }
    }

    private string SanitizeNodeName(string name) => name.Replace("<", "_").Replace(">", "_").Replace("`1", "T");
}
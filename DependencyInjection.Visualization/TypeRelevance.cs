using System.Reflection;

namespace DependencyInjection.Visualization;

public class TypeRelevance
{
    private static readonly Lazy<string> ProjectNamespacePrefix = new(DetermineNamespacePrefix);

    public static bool IsRelevantType(Type type)
    {
        return type.Namespace != null && type.Namespace.StartsWith(ProjectNamespacePrefix.Value);
    }
    
    private static string DetermineNamespacePrefix()
    {
        var assemblyName = Assembly.GetExecutingAssembly().GetName().Name;
        var parts = assemblyName.Split('.');
        
        return parts.Length > 0 ? parts[0] : string.Empty;
    }
}
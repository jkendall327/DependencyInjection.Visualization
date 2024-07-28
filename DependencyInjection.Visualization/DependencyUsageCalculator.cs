namespace DependencyInjection.Visualization;

internal class DependencyUsageCalculator
{
    private Dictionary<Type, int>? _dependencyUsage;

    public List<(Type ServiceType, int UsageCount)> GetMostUsedServices(IEnumerable<ServiceNode> rootNodes,
        int count,
        bool onlyUserCode)
    {
        var usageCount = CalculateDependencyUsage(rootNodes);

        var filtered = onlyUserCode
            ? usageCount.Where(kvp => TypeRelevance.IsUserType(kvp.Key))
            : usageCount;
        
        return filtered
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => (kvp.Key, kvp.Value))
            .ToList();
    }

    public List<Type> GetUnusedServices(IList<ServiceNode> rootNodes)
    {
        var usageCount = CalculateDependencyUsage(rootNodes);
        var allServices = rootNodes.Select(n => n.Descriptor.ServiceType);
        var unusedServices = allServices.Except(usageCount.Keys);

        return unusedServices.Where(TypeRelevance.IsUserType).ToList();
    }

    private Dictionary<Type, int> CalculateDependencyUsage(IEnumerable<ServiceNode> rootNodes)
    {
        if (_dependencyUsage is not null)
        {
            return _dependencyUsage;
        }
        
        var usageCount = new Dictionary<Type, int>();

        foreach (var rootNode in rootNodes)
        {
            TraverseNode(rootNode, usageCount);
        }

        _dependencyUsage = usageCount;
        
        return usageCount;
    }

    private void TraverseNode(ServiceNode node, Dictionary<Type, int> usageCount)
    {
        foreach (var dependency in node.Dependencies)
        {
            var serviceType = dependency.Descriptor.ServiceType;
            
            usageCount[serviceType] = usageCount.TryGetValue(serviceType, out var count) ? count + 1 : 1;
            
            TraverseNode(dependency, usageCount);
        }
    }
}
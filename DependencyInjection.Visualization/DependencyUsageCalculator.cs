namespace DependencyInjection.Visualization;

internal class DependencyUsageCalculator
{
    private Dictionary<Type, int>? _dependencyUsage;

    public IEnumerable<(Type ServiceType, int UsageCount)> GetMostUsedServices(IEnumerable<ServiceNode> rootNodes, int count)
    {
        var usageCount = CalculateDependencyUsage(rootNodes);
        
        return usageCount
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    public IEnumerable<Type> GetUnusedServices(IEnumerable<ServiceNode> rootNodes)
    {
        var usageCount = CalculateDependencyUsage(rootNodes);
        var allServices = rootNodes.Select(n => n.Descriptor.ServiceType);
        var unusedServices = allServices.Except(usageCount.Keys);

        return unusedServices.Where(TypeRelevance.IsUserType);
    }

    private Dictionary<Type, int> CalculateDependencyUsage(IEnumerable<ServiceNode> rootNodes)
    {
        if (_dependencyUsage is not null)
        {
            return _dependencyUsage;
        }
        
        var usageCount = new Dictionary<Type, int>();

        void TraverseNode(ServiceNode node)
        {
            foreach (var dependency in node.Dependencies)
            {
                var serviceType = dependency.Descriptor.ServiceType;
                usageCount[serviceType] = usageCount.TryGetValue(serviceType, out var count) ? count + 1 : 1;
                TraverseNode(dependency);
            }
        }

        foreach (var rootNode in rootNodes)
        {
            TraverseNode(rootNode);
        }

        _dependencyUsage = usageCount;
        
        return usageCount;
    }
}
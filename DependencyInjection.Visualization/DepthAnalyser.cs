namespace DependencyInjection.Visualization;

internal class DepthAnalyser
{
    private readonly TreeViewer _treeViewer;

    public DepthAnalyser(TreeViewer treeViewer)
    {
        _treeViewer = treeViewer;
    }

    public DependencyChains GetRegistrationChainsByDepth(List<ServiceNode> rootNodes, int minDepth, bool onlyUserCode = false)
    {
        var deepChains = new List<ServiceNode>();

        IEnumerable<ServiceNode> ofInterest = rootNodes;
        
        if (onlyUserCode)
        {
            ofInterest = rootNodes.Where(rootNode => TypeRelevance.IsUserType(rootNode.Descriptor.ServiceType));
        }
        
        foreach (var node in ofInterest)
        {
            ExploreChains(node, [node], minDepth, deepChains, onlyUserCode);
        }

        var stringRepresentation = _treeViewer.GenerateTreeView(deepChains);
        
        return new(deepChains, stringRepresentation);
    }

    private void ExploreChains(ServiceNode node, List<ServiceNode> currentChain, int minDepth, List<ServiceNode> result, bool onlyUserCode)
    {
        if (currentChain.Count >= minDepth && !result.Contains(currentChain[0]))
        {
            result.Add(currentChain[0]);
        }

        foreach (var child in node.Dependencies)
        {
            if (onlyUserCode && !TypeRelevance.IsUserType(child.Descriptor.ServiceType)) continue;
            
            var newChain = new List<ServiceNode>(currentChain) { child };
            
            ExploreChains(child, newChain, minDepth, result, onlyUserCode);
        }
    }
}
namespace DependencyInjection.Visualization;

public class DepthAnalyser
{
    private readonly TreeViewer _treeViewer;

    public DepthAnalyser(TreeViewer treeViewer)
    {
        _treeViewer = treeViewer;
    }

    public DependencyChains GetRegistrationChainsByDepth(List<ServiceNode> rootNodes, int minDepth, bool onlyUserCode = false)
    {
        var deepChains = new List<ServiceNode>();
        
        foreach (var rootNode in rootNodes)
        {
            if (!onlyUserCode || TypeRelevance.IsRelevantType(rootNode.Descriptor.ServiceType))
            {
                ExploreChains(rootNode, [rootNode], minDepth, deepChains, onlyUserCode);
            }
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
            if (!onlyUserCode || TypeRelevance.IsRelevantType(child.Descriptor.ServiceType))
            {
                var newChain = new List<ServiceNode>(currentChain) { child };
                ExploreChains(child, newChain, minDepth, result, onlyUserCode);
            }
        }
    }
}
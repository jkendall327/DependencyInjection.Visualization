namespace DependencyTree;

public class DependencyChains
{
    public List<ServiceNode> RootNodes { get; }
    public string StringRepresentation { get; }

    public DependencyChains(List<ServiceNode> rootNodes, string stringRepresentation)
    {
        RootNodes = rootNodes;
        StringRepresentation = stringRepresentation;
    }
}
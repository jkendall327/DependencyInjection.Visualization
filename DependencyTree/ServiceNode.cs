using Microsoft.Extensions.DependencyInjection;

namespace DependencyTree;

public class ServiceNode(ServiceDescriptor descriptor)
{
    public ServiceDescriptor Descriptor { get; } = descriptor;
    public List<ServiceNode> Dependencies { get; } = [];
}
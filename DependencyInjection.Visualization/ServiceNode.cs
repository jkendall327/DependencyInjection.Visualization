using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

public class ServiceNode(ServiceDescriptor descriptor)
{
    public ServiceDescriptor Descriptor { get; } = descriptor;
    public List<ServiceNode> Dependencies { get; } = [];
}
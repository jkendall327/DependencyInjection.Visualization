using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization;

public class ServiceNode(ServiceDescriptor descriptor)
{
    public ServiceDescriptor Descriptor { get; } = descriptor;
    public List<ServiceNode> Dependencies { get; } = [];

    public ServiceLifetime Lifetime => Descriptor.Lifetime;
    public string ServiceTypeName => FormatTypeName(Descriptor.ServiceType); 
    
    public string GetImplementationDescription()
    {
        // For simple cases like .AddSingleton<IFoobar, Foobar>, return Foobar
        if (Descriptor.ImplementationType != null)
        {
            return FormatTypeName(Descriptor.ImplementationType);
        }
    
        // When lambdas are used, like .AddSingleton<IFoobar>(x => new Foobar()), get the return type of the lambda
        if (Descriptor.ImplementationFactory != null)
        {
            var factoryType = Descriptor.ImplementationFactory.GetType();
            var methodInfo = factoryType.GetMethod("Invoke");
            
            if (methodInfo != null)
            {
                return FormatTypeName(methodInfo.ReturnType);
            }
        }
        
        // When a premade object is used, like .AddSingleton<IFoobar>(foobar), return the type of the instance
        if (Descriptor.ImplementationInstance != null)
        {
            return $"Instance of {FormatTypeName(Descriptor.ImplementationInstance.GetType())}";
        }

        return "Unknown";
    }

    /// <summary>
    /// Handles generic types.
    /// </summary>
    private string FormatTypeName(Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        // The recursive call here lets us handle arbitrarily-nested generics, e.g. List<List<List<Foobar>>>.
        var genericArgs = string.Join(", ", type.GetGenericArguments().Select(FormatTypeName));
        return $"{type.Name.Split('`')[0]}<{genericArgs}>";
    }
}
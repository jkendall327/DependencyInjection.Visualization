using DependencyInjection.Visualization;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;

public class DepthAnalyserTests
{
    private IServiceCollection CreateTestServices()
    {
        var services = new ServiceCollection();
        services.AddTransient<IServiceA, ServiceA>();
        services.AddScoped<IServiceB, ServiceB>();
        services.AddSingleton<IServiceC, ServiceC>();
        services.AddTransient<ServiceD>();
        return services;
    }
    
    public interface IServiceA { }
    public class ServiceA : IServiceA { }
    public interface IServiceB { }
    public class ServiceB : IServiceB { public ServiceB(IServiceA service1) { } }
    public interface IServiceC { }
    public class ServiceC : IServiceC { public ServiceC(IServiceA service1, IServiceB service2) { } }
    public class ServiceD { public ServiceD(IServiceC service3) { } }

    [Fact]
    public void GetRegistrationChainsByDepth_BasicFunctionality()
    {
        var services = CreateTestServices();
        var tree = new DependencyTree(services, false);

        var result = tree.GetRegistrationChainsByDepth(2);

        result.RootNodes.Should().NotBeEmpty();
        result.StringRepresentation.Should().NotBeNullOrWhiteSpace();
        result.RootNodes.Should().Contain(n => n.Descriptor.ServiceType == typeof(ServiceD));
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(1, 4)]
    public void GetRegistrationChainsByDepth_DepthFiltering(int minDepth, int expectedChains)
    {
        var services = CreateTestServices();
        var tree = new DependencyTree(services, false);

        var result = tree.GetRegistrationChainsByDepth(minDepth);

        result.RootNodes.Should().HaveCount(expectedChains);
    }
}
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace DependencyInjection.Visualization.Tests;

public class TreeBuilderTests
{
    private readonly TreeBuilder _treeBuilder = new();
    
    [Fact]
    public void BuildTree_EmptyServiceCollection_ReturnsEmptyList()
    {
        var services = new ServiceCollection();
        
        var result = _treeBuilder.BuildTree(services);
        
        result.Should().BeEmpty();
    }

    [Fact]
    public void BuildTree_SingleServiceWithNoDependencies_ReturnsOneRootNode()
    {
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        
        var result = _treeBuilder.BuildTree(services);
        
        result.Should().HaveCount(1);
        result[0].Descriptor.ImplementationType.Should().Be(typeof(TestService));
        result[0].Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void BuildTree_ServiceWithOneDependency_ReturnsCorrectTree()
    {
        var services = new ServiceCollection();
        
        // Both services are added to the container directly, thus they are both roots.
        services.AddTransient<ITestService, TestService>();
        services.AddTransient<IDependentService, DependentService>();
        
        var result = _treeBuilder.BuildTree(services);
        
        result.Should().HaveCount(2);

        var dependant = result.Single(n => n.Descriptor.ImplementationType == typeof(DependentService));
        
        dependant.Dependencies.Should().HaveCount(1);
        
        dependant.Dependencies[0].Descriptor.ImplementationType.Should().Be(typeof(TestService));
    }

    [Fact]
    public void BuildTree_ServiceWithMultipleDependencies_ReturnsCorrectTree()
    {
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        services.AddTransient<IOtherService, OtherService>();
        services.AddTransient<IMultiDependentService, MultiDependentService>();

        var result = _treeBuilder.BuildTree(services);

        result.Should().HaveCount(3);
        var multiDependant = result.Single(n => n.Descriptor.ImplementationType == typeof(MultiDependentService));
        multiDependant.Dependencies.Should().HaveCount(2);
        multiDependant.Dependencies.Should().Contain(n => n.Descriptor.ImplementationType == typeof(TestService));
        multiDependant.Dependencies.Should().Contain(n => n.Descriptor.ImplementationType == typeof(OtherService));
    }

    [Fact]
    public void BuildTree_CircularDependency_HandlesGracefully()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICircularA, CircularA>();
        services.AddTransient<ICircularB, CircularB>();

        var result = _treeBuilder.BuildTree(services);

        result.Should().HaveCount(2);
        var nodeA = result.Single(n => n.Descriptor.ImplementationType == typeof(CircularA));
        var nodeB = result.Single(n => n.Descriptor.ImplementationType == typeof(CircularB));
        nodeA.Dependencies.Should().HaveCount(1);
        nodeB.Dependencies.Should().HaveCount(1);
        nodeA.Dependencies[0].Descriptor.ImplementationType.Should().Be(typeof(CircularB));
        nodeB.Dependencies[0].Descriptor.ImplementationType.Should().Be(typeof(CircularA));
    }

    [Fact]
    public void BuildTree_ServiceWithNoSuitableConstructor_ExcludedFromTree()
    {
        var services = new ServiceCollection();
        services.AddTransient<INoConstructorService, NoConstructorService>();

        var result = _treeBuilder.BuildTree(services);

        result.Should().HaveCount(1);
        result[0].Dependencies.Should().BeEmpty();
    }

    [Fact]
    public void BuildTree_ServiceWithMultipleConstructors_UsesConstructorWithMostResolvableParameters()
    {
        var services = new ServiceCollection();
        services.AddTransient<ITestService, TestService>();
        services.AddTransient<IOtherService, OtherService>();
        services.AddTransient<IMultiConstructorService, MultiConstructorService>();

        var result = _treeBuilder.BuildTree(services);

        result.Should().HaveCount(3);
        var multiConstructorNode = result.Single(n => n.Descriptor.ImplementationType == typeof(MultiConstructorService));
        multiConstructorNode.Dependencies.Should().HaveCount(2);
        multiConstructorNode.Dependencies.Should().Contain(n => n.Descriptor.ImplementationType == typeof(TestService));
        multiConstructorNode.Dependencies.Should().Contain(n => n.Descriptor.ImplementationType == typeof(OtherService));
    }
}
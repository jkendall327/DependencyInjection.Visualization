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
        services.AddTransient<ITestService, TestService>();
        services.AddTransient<IDependentService, DependentService>();
        
        var result = _treeBuilder.BuildTree(services);
        
        result.Should().HaveCount(2);
        var dependentNode = result.Should().Contain(n => n.Descriptor.ImplementationType == typeof(DependentService)).Which;
        dependentNode.Dependencies.Should().HaveCount(1);
        dependentNode.Dependencies[0].Descriptor.ImplementationType.Should().Be(typeof(TestService));
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
        var multiDependentNode = result.Should().Contain(n => n.Descriptor.ImplementationType == typeof(MultiDependentService)).Which;
        multiDependentNode.Dependencies.Should().HaveCount(2);
        multiDependentNode.Dependencies.Should().Contain(n => n.Descriptor.ImplementationType == typeof(TestService));
        multiDependentNode.Dependencies.Should().Contain(n => n.Descriptor.ImplementationType == typeof(OtherService));
    }

    [Fact]
    public void BuildTree_CircularDependency_HandlesGracefully()
    {
        var services = new ServiceCollection();
        services.AddTransient<ICircularA, CircularA>();
        services.AddTransient<ICircularB, CircularB>();

        var result = _treeBuilder.BuildTree(services);

        result.Should().HaveCount(2);
        var nodeA = result.Should().Contain(n => n.Descriptor.ImplementationType == typeof(CircularA)).Which;
        var nodeB = result.Should().Contain(n => n.Descriptor.ImplementationType == typeof(CircularB)).Which;
        nodeA.Dependencies.Should().HaveCount(1);
        nodeB.Dependencies.Should().HaveCount(1);
        nodeA.Dependencies[0].Descriptor.ImplementationType.Should().Be(typeof(CircularB));
        nodeB.Dependencies[0].Descriptor.ImplementationType.Should().Be(typeof(CircularA));
    }
}
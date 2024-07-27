namespace DependencyInjection.Visualization.Tests;

public interface ITestService { }
public class TestService : ITestService { }
public interface IDependentService { }
public class DependentService : IDependentService
{
    public DependentService(ITestService testService) { }
}
public interface IOtherService { }
public class OtherService : IOtherService { }
public interface IMultiDependentService { }
public class MultiDependentService : IMultiDependentService
{
    public MultiDependentService(ITestService testService, IOtherService otherService) { }
}
public interface ICircularA { }
public class CircularA : ICircularA
{
    public CircularA(ICircularB b) { }
}
public interface ICircularB { }
public class CircularB : ICircularB
{
    public CircularB(ICircularA a) { }
}
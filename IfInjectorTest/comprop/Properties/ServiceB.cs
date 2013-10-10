
namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(ServiceB))]
    public interface IServiceB
    {
    }

    [IfInjector.Singleton]
    public class ServiceB : IServiceB
    {
        public ServiceB()
        {
        }
    }
}

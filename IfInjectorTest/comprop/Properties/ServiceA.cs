namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(ServiceA))]
    public interface IServiceA
    {
    }

    [IfInjector.Singleton]
    public class ServiceA : IServiceA
    {
        public ServiceA()
        {
        }
    }
}

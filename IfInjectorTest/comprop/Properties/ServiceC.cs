namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(ServiceC))]
    public interface IServiceC
    {
    }

    [IfInjector.Singleton]
    public class ServiceC : IServiceC
    {
        public ServiceC()
        {
        }
    }
}

using MEFAttr = System.ComponentModel.Composition;

namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(ServiceA))]
    public interface IServiceA
    {
    }

    [MEFAttr.ExportAttribute(typeof(IServiceA))]
    [MEFAttr.PartCreationPolicy(MEFAttr.CreationPolicy.Shared)]
    [IfInjector.Singleton]
    public class ServiceA : IServiceA
    {
        public ServiceA()
        {
        }
    }
}

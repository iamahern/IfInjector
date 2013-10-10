using System;

namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(SubObjectA))]
    public interface ISubObjectA
    {
        void Verify(string containerName);
    }

    public class SubObjectA : ISubObjectA
    {
        [IfInjector.Inject]
        public IServiceA ServiceA { get; set; }

        public void Verify(string containerName)
        {
            if (this.ServiceA == null)
            {
                throw new Exception("ServiceA was null for SubObjectC for container " + containerName);
            }
        }
    }
}

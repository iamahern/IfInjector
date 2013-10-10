using System;

namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(SubObjectC))]
    public interface ISubObjectC
    {
        void Verify(string containerName);
    }

    public class SubObjectC : ISubObjectC
    {
        [IfInjector.Inject]
        public IServiceC ServiceC { get; set; }

        public void Verify(string containerName)
        {
            if (this.ServiceC == null)
            {
                throw new Exception("ServiceC was null for SubObjectC for container " + containerName);
            }
        }
    }
}

using System;

namespace IocPerformance.Classes.Properties
{
    [IfInjector.ImplementedBy(typeof(ComplexPropertyObject))]
    public interface IComplexPropertyObject
    {
        void Verify(string containerName);
    }

    public class ComplexPropertyObject : IComplexPropertyObject
    {
        [IfInjector.Inject]
        public IServiceA ServiceA { get; set; }

        [IfInjector.Inject]
        public IServiceB ServiceB { get; set; }

        [IfInjector.Inject]
        public IServiceC ServiceC { get; set; }

        [IfInjector.Inject]
        public ISubObjectA SubObjectA { get; set; }

        [IfInjector.Inject]
        public ISubObjectB SubObjectB { get; set; }

        [IfInjector.Inject]
        public ISubObjectC SubObjectC { get; set; }

        public void Verify(string containerName)
        {
            if (this.ServiceA == null)
            {
                throw new Exception("ServiceA is null on ComplexPropertyObject for container " + containerName);
            }

            if (this.ServiceB == null)
            {
                throw new Exception("ServiceB is null on ComplexPropertyObject for container " + containerName);
            }

            if (this.ServiceC == null)
            {
                throw new Exception("ServiceC is null on ComplexPropertyObject for container " + containerName);
            }

            if (this.SubObjectA == null)
            {
                throw new Exception("SubObjectA is null on ComplexPropertyObject for container " + containerName);
            }

            this.SubObjectA.Verify(containerName);

            if (this.SubObjectB == null)
            {
                throw new Exception("SubObjectB is null on ComplexPropertyObject for container " + containerName);
            }

            this.SubObjectB.Verify(containerName);

            if (this.SubObjectC == null)
            {
                throw new Exception("SubObjectC is null on ComplexPropertyObject for container " + containerName);
            }

            this.SubObjectC.Verify(containerName);
        }
    }
}

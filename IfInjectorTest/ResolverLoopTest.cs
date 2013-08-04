using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfInjectorTypes;

namespace IfInjectorTest
{
    [TestFixture]
    public class ResolverLoopTest
    {
		protected internal static Injector injector = Injector.NewInstance();
		
       	[Test, Timeout(400)]
        public void TestResolverWithLoopingTypes1()
        {
			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcreteSomething).Name);

            try
            {
				var concrete = injector.Resolve<ConcreteSomething>();
            }
			catch (InjectorException ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

		[Test, Timeout(100)]
        public void TestResolverWithLoopingTypes2()
        {
			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcreteSecretLoop).Name);

            try
            {
                var concrete = injector.Resolve<ConcreteSecretLoop>();
            }
			catch (InjectorException ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception);
            //Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

		[Test, Timeout(100)]
        public void TestResolverWithPropertyLooping()
        {
			injector.Bind<ConcretePropertyLoop>()
				.AddPropertyInjector<ConcretePropertyLoop> (v => v.MyTestProperty);
            
			//fFastInjector.Injector.InternalResolver<ConcretePropertyLoop>.AddPropertySetter(v => v.MyTestProperty);//, () => Injector.Resolve<ConcretePropertyLoop>());

			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcretePropertyLoop).Name);

            try
            {
                var concrete = injector.Resolve<ConcretePropertyLoop>();
            }
			catch (InjectorException ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception);
            Assert.AreEqual(expectedErrorMessage, exception.Message);
        }

        interface ISomething
        {
            int Id { get; set; }
        }

        class ConcreteSomething : ISomething
        {
            public ConcreteSomething(ConcreteSomething parentSomething)
            {
            }

            public int Id
            {
                get
                {
                    throw new NotImplementedException();
                }
                set
                {
                    throw new NotImplementedException();
                }
            }
        }

        class ConcreteSecretLoop
        {
            public ConcreteSecretLoop()
            {
                var other = injector.Resolve<ConcreteSecretLoop2>();
            }
        }

        class ConcreteSecretLoop2
        {
            public ConcreteSecretLoop2()
            {
                var other = injector.Resolve<ConcreteSecretLoop>();
            }
        }

        class ConcretePropertyLoop
        {
            public ConcretePropertyLoop MyTestProperty { get; set; }
        }
    }
}

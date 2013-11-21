using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.Errors;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
    [TestFixture]
    public class ResolverLoopTest : Base2WayTest
    {
		static IfInjector.Injector SInjector { get; set; }

		[SetUp]
		public void SetupSInjector() {
			SInjector = Injector;
		}

       	[Test, Timeout(400)]
        public void TestResolverWithLoopingTypes1()
        {
			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcreteSomething).Name);

            try
            {
				var concrete = Injector.Resolve<ConcreteSomething>();
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
                var concrete = Injector.Resolve<ConcreteSecretLoop>();
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
			Bind(MakeBind<ConcretePropertyLoop>()
				.InjectMember<ConcretePropertyLoop> (v => v.MyTestProperty));
            
			//fFastInjector.Injector.InternalResolver<ConcretePropertyLoop>.AddPropertySetter(v => v.MyTestProperty);//, () => Injector.Resolve<ConcretePropertyLoop>());

			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcretePropertyLoop).Name);

            try
            {
                var concrete = Injector.Resolve<ConcretePropertyLoop>();
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
                var other = SInjector.Resolve<ConcreteSecretLoop2>();
            }
        }

        class ConcreteSecretLoop2
        {
            public ConcreteSecretLoop2()
            {
				var other = SInjector.Resolve<ConcreteSecretLoop>();
            }
        }

        class ConcretePropertyLoop
        {
            public ConcretePropertyLoop MyTestProperty { get; set; }
        }
    }
}

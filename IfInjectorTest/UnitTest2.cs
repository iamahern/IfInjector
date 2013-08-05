using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfInjectorTypes;

namespace IfInjectorTest
{
    [TestFixture]
    public class UnitTest2
    {
		private Injector injector = Injector.NewInstance();

        class TestClassSetResolverByConstructorInfoTest
        {
            public TestClassSetResolverByConstructorInfoTest() { }
            public TestClassSetResolverByConstructorInfoTest(OtherClassSetResolverByConstructorInfoTest something) { }

            public class OtherClassSetResolverByConstructorInfoTest { }
        }


        [Test]
        public void ResolveByTypeObjectTest()
        {
            // resolve at least twice to execute both code paths
			var myObject = injector.Resolve(typeof(TestClassTestResolveByTypeObject));
			myObject = injector.Resolve(typeof(TestClassTestResolveByTypeObject));
			myObject = injector.Resolve(typeof(TestClassTestResolveByTypeObject));
        }

        class TestClassTestResolveByTypeObject { }

        [Test]
        public void AddPropertyInjectorTest()
        {
			var binding = injector.Bind<TestClassAddPropertyInjectorTest> ();
			binding.AddPropertyInjector((TestClassAddPropertyInjectorTest v) => v.Other);
			binding.AddPropertyInjector((TestClassAddPropertyInjectorTest v) => v.MyStringProperty, () => "Goldfinger");

			var result = injector.Resolve<TestClassAddPropertyInjectorTest>();

            Assert.AreEqual("Goldfinger", result.MyStringProperty);
            Assert.AreEqual(7, result.Other.Id);
        }

        class TestClassAddPropertyInjectorTest
        {
            public string MyStringProperty { get; set; }
            public OtherClassAddPropertyInjectorTest Other { get; set; }

            public class OtherClassAddPropertyInjectorTest { public int Id { get { return 7; } } }
        }

        [Test]
        public void InterfaceExceptionTest()
        {
			InjectorException exception = null;
            try
            {
				injector.Resolve<IInterfaceExceptionTest>();
            }
			catch (InjectorException ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception);
        }

        interface IInterfaceExceptionTest { }

        [Test]
        public void AddPropertySetterNotMemberExpression()
        {
			var binding = injector.Bind<TestClassAddPropertyInjectorTest>();

			InjectorException exception = null;
            try
            {
				binding.AddPropertyInjector((TestClassAddPropertyInjectorTest v) => "");
            }
			catch (InjectorException ex)
            {
                exception = ex;
            }

            Assert.IsNotNull(exception);
        }

        [Test]
        public void ExceptionConstructorTest()
        {
			var ex1 = new InjectorException(InjectorErrors.ErrorAmbiguousBinding, "foobar");
			var ex2 = new InjectorException(InjectorErrors.ErrorAmbiguousBinding, "something", ex1);

            Assert.IsNotNull(ex1);
            Assert.IsNotNull(ex2);
        }

		[Test]
		public void ExceptionErrorCodeFormattingTest()
		{
			var random = Guid.NewGuid ().ToString ();
			var ex = InjectorErrors.ErrorAmbiguousBinding.FormatEx (random);

			Assert.AreEqual (string.Format (InjectorErrors.ErrorAmbiguousBinding.MessageTemplate, random), ex.Message);
			Assert.AreEqual ("IF0004", InjectorErrors.ErrorAmbiguousBinding.MessageCode);
		}

		class NoProperConstructor {
			private NoProperConstructor() {}
		}

		[Test]
		public void NoProperConstructorTest() {
			var injector = Injector.NewInstance ();
			var ex1 = InjectorErrors.ErrorNoAppropriateConstructor.FormatEx (typeof(NoProperConstructor).FullName);

			try { 
				injector.Resolve<NoProperConstructor>();
				Assert.Fail();
			} catch (InjectorException ex) {
				Assert.AreEqual (ex1.ErrorType.MessageCode, ex.ErrorType.MessageCode);
			}
		}

		[Test]
		public void TestVerify() {
			var injector = Injector.NewInstance ();
			injector.Bind<NoProperConstructor> ();

			try { 
				injector.Verify();
				Assert.Fail();
			} catch (InjectorException ex) {
				var ex1 = InjectorErrors.ErrorNoAppropriateConstructor.FormatEx (typeof(NoProperConstructor).FullName);
				Assert.AreEqual (ex1.ErrorType.MessageCode, ex.ErrorType.MessageCode);
			}
		}
    }
}

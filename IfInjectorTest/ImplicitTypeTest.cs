using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfInjectorTypes;

namespace IfInjectorTest
{
    [TestFixture]
    public class ImplicitTypeTest
    {		
        [Test]
        public void TestResolveInterfaces()
        {
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass1>();
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyInterfaceDerived>());
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyInterface>());
        }

		[Test]
		public void TestResolveInterfacesByType()
		{
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass1>();
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve(typeof(MyInterfaceDerived)));
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve(typeof(MyInterface)));
		}

		[Test]
		public void TestBindingSpecificity()
		{
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyInterface, MyTestClass2> ();

			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyTestClass1>());
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyInterfaceDerived>());

			Assert.IsInstanceOf<MyTestClass2>(injector.Resolve<MyInterface>());
		}

		[Test]
		public void TestErrorOnAmbiguousResolution()
		{
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyInterfaceDerived, MyTestClass2> ();

			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorAmbiguousBinding.MessageTemplate, typeof(MyInterface).Name);

			try 
			{
				injector.Resolve<MyInterface> ();
			} 
			catch (InjectorException ex) 
			{
				exception = ex;
			}

			Assert.IsNotNull(exception);
			Assert.AreEqual(expectedErrorMessage, exception.Message);
		}
      
		[Test]
		public void TestResolveBaseTypeImplicitly()
		{
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyTestClass3>();

			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyTestClass1>());
			Assert.IsInstanceOf<MyTestClass3>(injector.Resolve<MyTestClass2>());
			Assert.IsInstanceOf<MyTestClass3>(injector.Resolve<MyTestClass3>());
		}

		[Test]
		public void TestResolveBaseTypeImplicitlyNonGeneric()
		{
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyTestClass3>();

			Assert.IsInstanceOf<MyTestClass3>(injector.Resolve(typeof(MyTestClass2)));
		}

        interface MyInterface
        {
        }

		interface MyInterfaceDerived : MyInterface
		{
		}


		class MyTestClass1 : MyInterfaceDerived
        {
        }

		class MyTestClass2 : MyInterfaceDerived
		{
		}

		class MyTestClass3 : MyTestClass2
		{
		}

    }
}

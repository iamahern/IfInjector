using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfCore;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
    [TestFixture]
    public class ImplicitTypeTest : Base2WayTest
    {		
        [Test]
        public void TestResolveInterfaces()
        {
			Bind<MyTestClass1>();
			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve<MyInterfaceDerived>());
			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve<MyInterface>());
        }

		[Test]
		public void TestResolveInterfacesByType()
		{
			Bind<MyTestClass1>();
			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve(typeof(MyInterfaceDerived)));
			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve(typeof(MyInterface)));
		}

		[Test]
		public void TestBindingSpecificity()
		{
			Bind<MyTestClass1>();
			Bind<MyInterface, MyTestClass2> ();

			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve<MyTestClass1>());
			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve<MyInterfaceDerived>());

			Assert.IsInstanceOf<MyTestClass2>(Injector.Resolve<MyInterface>());
		}

		[Test]
		public void TestErrorOnAmbiguousResolution()
		{
			Bind<MyTestClass1>();
			Bind<MyInterfaceDerived, MyTestClass2> ();

			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorAmbiguousBinding.MessageTemplate, typeof(MyInterface).Name);

			try 
			{
				Injector.Resolve<MyInterface> ();
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
			Bind<MyTestClass1>();
			Bind<MyTestClass3>();

			Assert.IsInstanceOf<MyTestClass1>(Injector.Resolve<MyTestClass1>());
			Assert.IsInstanceOf<MyTestClass3>(Injector.Resolve<MyTestClass2>());
			Assert.IsInstanceOf<MyTestClass3>(Injector.Resolve<MyTestClass3>());
		}

		[Test]
		public void TestResolveBaseTypeImplicitlyNonGeneric()
		{
			Bind<MyTestClass1>();
			Bind<MyTestClass3>();

			Assert.IsInstanceOf<MyTestClass3>(Injector.Resolve(typeof(MyTestClass2)));
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

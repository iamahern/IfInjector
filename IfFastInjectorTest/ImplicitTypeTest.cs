using System;
using NUnit.Framework;
using IfFastInjector;
using IfFastInjector.IfInjectorTypes;

namespace IfFastInjectorMxTest
{
    [TestFixture]
    public class ImplicitTypeTest
    {		
        [Test]
        public void TestResolveInterfaces()
        {
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass1>();
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyInterfaceDerived>());
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyInterface>());
        }

		[Test]
		public void TestResolveInterfacesByType()
		{
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass1>();
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve(typeof(MyInterfaceDerived)));
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve(typeof(MyInterface)));
		}

		[Test]
		public void TestBindingSpecificity()
		{
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyInterface, MyTestClass2> ();

			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyTestClass1>());
			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyInterfaceDerived>());

			Assert.IsInstanceOf<MyTestClass2>(injector.Resolve<MyInterface>());
		}

		[Test]
		public void TestErrorOnAmbiguousResolution()
		{
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyInterfaceDerived, MyTestClass2> ();

			IfFastInjectorException exception = null;
			var expectedErrorMessage = string.Format(IfFastInjectorErrors.ErrorAmbiguousBinding.MessageTemplate, typeof(MyInterface).Name);

			try 
			{
				injector.Resolve<MyInterface> ();
			} 
			catch (IfFastInjectorException ex) 
			{
				exception = ex;
			}

			Assert.IsNotNull(exception);
			Assert.AreEqual(expectedErrorMessage, exception.Message);
		}
      
		[Test]
		public void TestResolveBaseTypeImplicitly()
		{
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass1>();
			injector.Bind<MyTestClass3>();

			Assert.IsInstanceOf<MyTestClass1>(injector.Resolve<MyTestClass1>());
			Assert.IsInstanceOf<MyTestClass3>(injector.Resolve<MyTestClass2>());
			Assert.IsInstanceOf<MyTestClass3>(injector.Resolve<MyTestClass3>());
		}

		[Test]
		public void TestResolveBaseTypeImplicitlyNonGeneric()
		{
			IfInjector injector = IfInjector.NewInstance();
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

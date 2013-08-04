using System;
using NUnit.Framework;
using IfInjector;
using System.Diagnostics;

namespace IfInjectorTest
{
    [TestFixture]
    public class MultipleInjectorsTest
    {
		private Injector injector1 = Injector.NewInstance();
		private Injector injector2 = Injector.NewInstance();

        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
			MyClass i1expect = new MyClass(), 
					i2expect = new MyClass();

			injector1.Bind<MyClass, MyClass> (() => i1expect);
			injector2.Bind<MyClass, MyClass> (() => i2expect);

			var res1 = injector2.Resolve<MyClass>();
			var res2 = injector2.Resolve<MyClass>();
        }

        class MyClass
        {
        }
    }
}

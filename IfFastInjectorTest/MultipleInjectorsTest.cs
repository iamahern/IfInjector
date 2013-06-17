using System;
using NUnit.Framework;
using IfFastInjector;
using System.Diagnostics;

namespace IfFastInjectorMxTest
{
    [TestFixture]
    public class MultipleInjectorsTest
    {
		private IfInjector injector1 = IfInjector.NewInstance();
		private IfInjector injector2 = IfInjector.NewInstance();

        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
			MyClass i1expect = new MyClass(), 
					i2expect = new MyClass();

			injector1.Bind<MyClass> (() => i1expect);
			injector2.Bind<MyClass> (() => i2expect);

			var res1 = injector2.Resolve<MyClass>();
			var res2 = injector2.Resolve<MyClass>();
        }

        class MyClass
        {
        }
    }
}

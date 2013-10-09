using System;
using NUnit.Framework;
using IfInjector;
using System.Diagnostics;

namespace IfInjectorTest
{
    [TestFixture]
    public class MultipleInjectorsTest
    {
        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
			Injector injector1 = new Injector();
			Injector injector2 = new Injector();

			MyClass i1expect = new MyClass(), 
					i2expect = new MyClass();

			injector1.Bind(Binding.For<MyClass>().SetFactory(() => i1expect));
			injector2.Bind(Binding.For<MyClass>().SetFactory(() => i2expect));

			var res1 = injector2.Resolve<MyClass>();
			var res2 = injector2.Resolve<MyClass>();
        }

        class MyClass
        {
        }
    }
}

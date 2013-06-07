using System;
using NUnit.Framework;
using IfFastInjector;
using System.Diagnostics;

namespace IfFastInjector
{
    [TestFixture]
    public class MultipleInjectorsTest
    {
		private Injector injector1 = new Injector();
		private Injector injector2 = new Injector();

        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
			MyClass i1expect = new MyClass(), 
					i2expect = new MyClass();

			injector1.SetResolver<MyClass> (() => i1expect);
			injector2.SetResolver<MyClass> (() => i2expect);

			var res1 = injector2.Resolve<MyClass>();
			var res2 = injector2.Resolve<MyClass>();
        }

        class MyClass
        {
        }
    }
}

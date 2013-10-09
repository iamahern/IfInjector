using System;
using NUnit.Framework;
using IfInjector;
using System.Diagnostics;

namespace IfInjectorTest
{
    [TestFixture]
    public class UnitTest1
    {
        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
            var x = new myClass();
			var injector = new Injector();

			injector.Bind(Binding.For<myInterface>().SetFactory(() => GetNew()));

			var z1 = injector.Resolve<myInterface>();
			var z2 = injector.Resolve<myInterface>();
        }

		myClass GetNew()
        {
            return new myClass();
        }

        interface myInterface
        {
        }

        class myClass : myInterface
        {
        }

        [Test]
        public void TestResolverReplace()
		{
			var injector = new Injector ();
			MyTestResolverReplaceDependency dep = new MyTestResolverReplaceDependency ();

			injector.Bind(Binding.For<MyTestResolverReplace>().SetFactory(() => new MyTestResolverReplace(dep)));

			var result = injector.Resolve<MyTestResolverReplace>();
			Assert.AreSame(dep, result.dependency);
        }

        class MyTestResolverReplace
        {
			public readonly MyTestResolverReplaceDependency dependency;

			public MyTestResolverReplace(MyTestResolverReplaceDependency dependency) { this.dependency = dependency; }
        }

        class MyTestResolverReplaceDependency
        {

        }
    }
}

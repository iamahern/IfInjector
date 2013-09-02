using System;
using NUnit.Framework;
using IfInjector;
using System.Diagnostics;

namespace IfInjectorTest
{
    [TestFixture]
    public class UnitTest1
    {
		private Injector injector = new Injector();

        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
            var x = new myClass();

			injector.Bind<myInterface, myClass>().SetFactory(() => GetNew());

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
			MyTestResolverReplaceDependency dep = new MyTestResolverReplaceDependency ();

			injector.Bind<MyTestResolverReplace>().SetFactory(() => new MyTestResolverReplace(dep));

			var result = injector.Resolve<MyTestResolverReplace>();
			Assert.IsTrue (object.ReferenceEquals(dep, result.dependency));
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

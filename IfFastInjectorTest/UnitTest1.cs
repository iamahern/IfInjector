using System;
using NUnit.Framework;
using IfFastInjector;
using System.Diagnostics;

namespace IfFastInjector
{
    [TestFixture]
    public class UnitTest1
    {
		private Injector injector = new Injector();

        [Test]
        public void RegisterMethodToResolveInterfaceTest()
        {
            var x = new myClass();

			injector.SetResolver<myInterface>(() => GetNew());

			var z1 = injector.Resolve<myInterface>();
			var z2 = injector.Resolve<myInterface>();
        }

        myInterface GetNew()
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

			injector.SetResolver<MyTestResolverReplace> (() => new MyTestResolverReplace(dep));

			// TODO - Fix test; is this OK?
            //var resolverExpression = Injector.InternalResolver<MyTestResolverReplace>.ResolverExpression;
            //Assert.IsTrue(resolverExpression.ToString().Contains("Invoke(InternalResolver`1.Resolve"));

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

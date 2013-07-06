using System;
using NUnit.Framework;
using IfFastInjector;
using IfFastInjector.IfInjectorTypes;

namespace IfFastInjectorMxTest
{
    [TestFixture]
    public class BindingAttributeTest
    {
		[Test]
        public void ImplicitBindPublicProperty ()
		{
			// resolve at least twice to execute both code paths
			var injector = IfInjector.NewInstance ();
			injector.Bind<Inner> ().AsSingleton ();
			injector.Bind<Outer> ().AsSingleton ();

			var res = injector.Resolve<Outer> ();

			Assert.IsNotNull (res.MyInner);
		}

		[Test]
		public void ImplicitBindPrivateProperty ()
		{
			// resolve at least twice to execute both code paths
			var injector = IfInjector.NewInstance ();
			injector.Bind<Inner> ().AsSingleton ();
			injector.Bind<Outer> ().AsSingleton ();

			var res = injector.Resolve<Outer> ();

			Assert.IsNotNull (res.GetMyInnerPrivateProp());
		}

		[Test]
		public void ImplicitBindPrivateField ()
		{
			// resolve at least twice to execute both code paths
			var injector = IfInjector.NewInstance ();
			injector.Bind<Inner> ().AsSingleton ();
			injector.Bind<Outer> ().AsSingleton ();

			var res = injector.Resolve<Outer> ();

			Assert.IsNotNull (res.GetMyInnerPrivateField());
		}

		class Outer
		{
			[IfInject]
			public Inner MyInner { get; private set; }

			[IfInject]
			private Inner MyInnerPrivate { get; set; }

			[IfInject]
			private Inner myInnerPrivate;

			public Inner GetMyInnerPrivateProp ()
			{
				return MyInnerPrivate;
			}

			public Inner GetMyInnerPrivateField ()
			{
				return myInnerPrivate;
			}
		}

		class Inner
		{
		}
    }
}

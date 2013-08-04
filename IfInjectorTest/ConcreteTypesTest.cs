using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest
{
	[TestFixture()]
	public class ConcreteTypesTest
	{
		[Test()]
		public void TestCanSetConcreteOnlyProperties ()
		{
			string expectX = "foobar";

			var injector = Injector.NewInstance ();
			injector.Bind<Foo, Bar> ()
				.AddPropertyInjector (v => v.X, () => expectX);

			Bar b = (Bar)injector.Resolve<Foo> ();

			Assert.AreEqual (expectX, b.X);
		}

		[Test()]
		public void TestTypeSingletonsForInterfaceBindings ()
		{
			var injector = Injector.NewInstance ();
			injector.Bind<Foo, Bar> ().AsSingleton();

			Foo a = injector.Resolve<Foo> ();
			Foo b = injector.Resolve<Foo> ();

			Assert.IsTrue(object.ReferenceEquals(a, b));
		}


		interface Foo {}

		class Bar : Foo {
			public string X { get; set; }
		}
	}
}


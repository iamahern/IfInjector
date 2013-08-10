using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest
{
	[TestFixture()]
	public class ConcreteTypesTest : Base2WayTest
	{
		[Test()]
		public void TestCanSetConcreteOnlyProperties ()
		{
			string expectX = "foobar";

			Bind<Foo, Bar> ()
				.AddPropertyInjector (v => v.X, () => expectX);

			Bar b = (Bar)Injector.Resolve<Foo> ();

			Assert.AreEqual (expectX, b.X);
		}

		[Test()]
		public void TestTypeSingletonsForInterfaceBindings ()
		{
			Bind<Foo, Bar> ().AsSingleton();

			Foo a = Injector.Resolve<Foo> ();
			Foo b = Injector.Resolve<Foo> ();

			Assert.IsTrue(object.ReferenceEquals(a, b));
		}


		interface Foo {}

		class Bar : Foo {
			public string X { get; set; }
		}
	}
}


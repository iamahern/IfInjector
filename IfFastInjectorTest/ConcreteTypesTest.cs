using NUnit.Framework;
using System;

using IfFastInjector;

namespace FastInjectorMxTest
{
	[TestFixture()]
	public class ConcreteTypesTest
	{
		[Test()]
		public void TestCanSetConcreteOnlyProperties ()
		{
			string expectX = "foobar";

			IfInjector injector = IfInjector.NewInstance ();
			injector.Bind<Foo, Bar> ()
				.AddPropertyInjector (v => v.X, () => expectX);

			Bar b = (Bar)injector.Resolve<Foo> ();

			Assert.AreEqual (expectX, b.X);
		}

		[Test()]
		public void TestTypeSingletonsForInterfaceBindings ()
		{
			IfInjector injector = IfInjector.NewInstance ();
			injector.Bind<Foo, Bar> ().AsSingleton();

			Foo a = injector.Resolve<Foo> ();
			Foo b = injector.Resolve<Foo> ();

			Assert.IsTrue(object.ReferenceEquals(a, b));
		}

		[Test()]
		public void TestBindingsRespectKeyConcreateTypeDistinction ()
		{
			IfInjector injector = IfInjector.NewInstance ();
			injector.Bind<Foo, Bar> ().AsSingleton();
			injector.Bind<Bar, Bar> ().AsSingleton();

			Foo a = injector.Resolve<Foo> ();
			Foo b = injector.Resolve<Bar> ();

			Assert.IsFalse(object.ReferenceEquals(a, b));
		}


		interface Foo {}

		class Bar : Foo {
			public string X { get; set; }
		}
	}
}


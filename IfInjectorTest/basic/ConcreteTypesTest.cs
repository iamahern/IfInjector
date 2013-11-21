using NUnit.Framework;
using System;

using IfInjector;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
	[TestFixture()]
	public class ConcreteTypesTest : Base2WayTest
	{
		[Test()]
		public void TestCanSetConcreteOnlyProperties ()
		{
			string expectX = "foobar";

			Bind(MakeBind<Foo, Bar> ()
				.InjectMember (v => v.X, () => expectX));

			Bar b = (Bar)Injector.Resolve<Foo> ();

			Assert.AreEqual (expectX, b.X);
		}

		[Test()]
		public void TestTypeSingletonsForInterfaceBindings ()
		{
			Bind(MakeBind<Foo, Bar> ().AsSingleton());

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


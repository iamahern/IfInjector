using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfInjectorExtensions;

namespace IfInjectorTest
{
	[TestFixture()]
	public class FactoryTest
	{
		class A1 {}
		class A2 {}
		class A3 {}
		class A4 {}

		class B{
			public readonly A1 Ma1;
			public readonly A2 Ma2;
			public readonly A3 Ma3;
			public readonly A4 Ma4;

			public B(A1 a1, A2 a2, A3 a3, A4 a4) {
				Ma1 = a1;
				Ma2 = a2;
				Ma3 = a3;
				Ma4 = a4;
			}

			public B(A1 a1, A2 a2, A3 a3) {
				Ma1 = a1;
				Ma2 = a2;
				Ma3 = a3;
			}

			public B(A1 a1, A2 a2) {
				Ma1 = a1;
				Ma2 = a2;
			}

			public B(A1 a1) {
				Ma1 = a1;
			}
		};
		
		[Test]
		public void TestFunc1 ()
		{
			var injector = Injector.NewInstance();
			injector.Bind<B,A1,B> ((a1) => new B (a1));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNull (b.Ma2);
			Assert.IsNull (b.Ma3);
			Assert.IsNull (b.Ma4);
		}

		[Test]
		public void TestFunc2 ()
		{
			var injector = Injector.NewInstance();
			injector.Bind<B,A1,A2,B> ((a1,a2) => new B (a1,a2));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNotNull (b.Ma2);
			Assert.IsNull (b.Ma3);
			Assert.IsNull (b.Ma4);
		}

		[Test]
		public void TestFunc3 ()
		{
			var injector = Injector.NewInstance();
			injector.Bind<B,A1,A2,A3,B> ((a1,a2,a3) => new B (a1,a2,a3));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNotNull (b.Ma2);
			Assert.IsNotNull (b.Ma3);
			Assert.IsNull (b.Ma4);
		}

		[Test]
		public void TestFunc4 ()
		{
			var injector = Injector.NewInstance();
			injector.Bind<B,A1,A2,A3,A4,B> ((a1,a2,a3,a4) => new B (a1,a2,a3,a4));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNotNull (b.Ma2);
			Assert.IsNotNull (b.Ma3);
			Assert.IsNotNull (b.Ma4);
		}
	}
}


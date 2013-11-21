using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest
{
	[TestFixture()]
	public class FactoryTest
	{
		class A1 {}
		class A2 {}
		class A3 {}
		class A4 {}

		interface IB {}

		class B : IB {
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

			public B() {}

			public C C { get; private set; }

			public D MyD = null;
		};

		class C {}
		class D {}
		
		[Test]
		public void TestFunc1 ()
		{
			var injector = new Injector();
			injector.Register(Binding.For<B>().SetFactory((A1 a1) => new B (a1)));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNull (b.Ma2);
			Assert.IsNull (b.Ma3);
			Assert.IsNull (b.Ma4);
		}

		[Test]
		public void TestFunc2 ()
		{
			var injector = new Injector();
			injector.Register(Binding.For<B>().SetFactory((A1 a1, A2 a2) => new B (a1,a2)));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNotNull (b.Ma2);
			Assert.IsNull (b.Ma3);
			Assert.IsNull (b.Ma4);
		}

		[Test]
		public void TestFunc3 ()
		{
			var injector = new Injector();
			injector.Register(Binding.For<B>().SetFactory ((A1 a1, A2 a2, A3 a3) => new B (a1,a2,a3)));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNotNull (b.Ma2);
			Assert.IsNotNull (b.Ma3);
			Assert.IsNull (b.Ma4);
		}

		[Test]
		public void TestFunc4 ()
		{
			var injector = new Injector();
			injector.Register(Binding.For<B>().SetFactory ((A1 a1, A2 a2, A3 a3, A4 a4) => new B (a1,a2,a3,a4)));
			var b = injector.Resolve<B> ();
			Assert.IsNotNull (b.Ma1);
			Assert.IsNotNull (b.Ma2);
			Assert.IsNotNull (b.Ma3);
			Assert.IsNotNull (b.Ma4);
		}

		[Test]
		public void TestSingltonInterfaceFactoryWithProperty() {
			var injector = new Injector ();
			injector.Register(Binding.For<IB>().SetFactory (() => new B ())
				.InjectMember (ist => ist.C)
				.InjectMember (ist => ist.MyD));
			var b = injector.Resolve<IB> () as B;
			Assert.IsNull (b.Ma1);
			Assert.IsNull (b.Ma2);
			Assert.IsNull (b.Ma3);
			Assert.IsNull (b.Ma4);
			Assert.IsNotNull (b.C);
			Assert.IsNotNull (b.MyD);
		}
	}
}


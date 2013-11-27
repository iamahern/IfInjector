using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.Bindings.Lifestyles;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
	[TestFixture()]
	public class LifestyleTest : Base2WayTest
	{
		static int changeCounter = 0;

		static readonly Lifestyle customLifestyle = Lifestyle.CreateCustom (instanceCreator => {
			int counter = 0;
			object instance = instanceCreator();

			return () => {
				if (counter != changeCounter) {
					instance = instanceCreator();
					counter = changeCounter;
				}

				return instance;
			};
		});

		class A {}
		class B {
			[Inject]
			public A A { get; set; }
		}

		[Test()]
		public void TestSingleton ()
		{
			Bind (MakeBind<A> ().SetLifestyle (Lifestyle.Singleton));

			var a1 = Injector.Resolve<A> ();
			var a2 = Injector.Resolve<A> ();

			Assert.AreSame (a1, a2);
		}

		[Test()]
		public void TestTransient ()
		{
			Bind (MakeBind<A> ().SetLifestyle (Lifestyle.Transient));

			var a1 = Injector.Resolve<A> ();
			var a2 = Injector.Resolve<A> ();

			Assert.AreNotSame (a1, a2);
		}

		[Test()]
		public void TestCustomLifestyle ()
		{
			Bind (MakeBind<A> ().SetLifestyle (customLifestyle));
			Bind (MakeBind<B> ());

			var b1 = Injector.Resolve<B> ();
			var b2 = Injector.Resolve<B> ();

			changeCounter++;

			var b3 = Injector.Resolve<B> ();
			var b4 = Injector.Resolve<B> ();

			Assert.AreSame (b1.A, b2.A);
			Assert.AreSame (b3.A, b4.A);
			Assert.AreNotSame (b1.A, b3.A);

			Assert.AreNotSame (b1, b2);
			Assert.AreNotSame (b2, b3);
			Assert.AreNotSame (b3, b4);
		}
	}
}
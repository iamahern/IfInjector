using NUnit.Framework;
using System;

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

			var a1 = Injector.Resolve<A> ();
			var a2 = Injector.Resolve<A> ();

			changeCounter++;

			var a3 = Injector.Resolve<A> ();
			var a4 = Injector.Resolve<A> ();

			Assert.AreSame (a1, a2);
			Assert.AreSame (a3, a4);
			Assert.AreNotSame (a1, a3);
		}
	}
}
using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfCore;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
	[TestFixture()]
	public class RecompilationTest : Base2WayTest
	{
		[Singleton]
		class A {}

		[Singleton]
		class B {
			public B(A a) {}
		}

		class C {}

		[Test]
		public void TestDoNotAllowRecompilation ()
		{
			var before = Injector.Resolve<B> ();

			try {
				if (IsFactory) {
					Injector.Register(Binding.For<C> ().SetFactory(() => new C ()));
				} else {
					Injector.Register(Binding.For<C> ());
				}

				Assert.Fail("Exception should be thrown");
			} catch (InjectorException ex) {
				Assert.AreEqual (InjectorErrors.ErrorBindingRegistrationNotPermitted, ex.ErrorType);
			}

		}
	}
}


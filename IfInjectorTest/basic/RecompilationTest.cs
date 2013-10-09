using NUnit.Framework;
using System;

using IfInjector;
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

		[Singleton]
		class C {
			public A MyAProp { get; set; }
			public A MyAField = null;
		}

		[Singleton]
		class D {
			public D(C c) {}
		}

		[Singleton]
		class E {}

		[SetUp]
		public void Setup() {
			var console = new System.Diagnostics.ConsoleTraceListener ();
			System.Diagnostics.Debug.Listeners.Add (console);
			System.Diagnostics.Debug.AutoFlush = true;
		}

		[Test]
		public void TestConstructDependency ()
		{
			var before = Injector.Resolve<B> ();
			if (IsFactory) {
				Injector.Bind(Binding.For<B> ().SetFactory((A a) => new B (a)));
			} else {
				Injector.Bind(Binding.For<B> ());
			}
			var after = Injector.Resolve<B> ();
			Assert.AreNotSame(before, after);
			Assert.AreSame(after, Injector.Resolve<B>());
		}

		[Test]
		public void TestPropertyDependency ()
		{
			var injector = new Injector();

			Bind(MakeBind<C> ().AddPropertyInjector (c => c.MyAProp));
			var before = Injector.Resolve<C> ();

			Bind<A> ();
			var after = Injector.Resolve<C> ();

			Assert.AreNotSame(before, after);
			Assert.AreSame(after, Injector.Resolve<C>());
		}

		[Test]
		public void TestFieldDependency ()
		{
			Bind(MakeBind<C> ().AddPropertyInjector (c => c.MyAField));
			var before = Injector.Resolve<C> ();

			Bind<A> ();
			var after = Injector.Resolve<C> ();

			Assert.AreNotSame(before, after);
			Assert.AreSame(after, Injector.Resolve<C>());
		}

		[Test]
		public void TestDeepDependency ()
		{
			Bind(MakeBind<C> ().AddPropertyInjector (c => c.MyAField));

			var before = Injector.Resolve<D> ();

			Bind<A> ();

			var after = Injector.Resolve<D> ();

			Assert.AreNotSame(before, after);
			Assert.AreSame(after, Injector.Resolve<D>());
		}

		[Test]
		public void TestOnlyRecompileAffectedGraph ()
		{
			Bind<E> ();
			Bind(MakeBind<C> ().AddPropertyInjector (c => c.MyAField));

			var beforeD = Injector.Resolve<D> ();
			var beforeE = Injector.Resolve<E> ();

			Bind<A> ();

			var afterD = Injector.Resolve<D> ();
			var afterE = Injector.Resolve<E> ();

			Assert.AreNotSame(beforeD, afterD);
			Assert.AreSame(afterD, Injector.Resolve<D>());
			Assert.AreSame(beforeE, afterE);
		}
	}
}


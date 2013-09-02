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


		[Test]
		public void TestConstructDependency ()
		{
			var before = Injector.Resolve<B> ();
			if (IsFactory) {
				Injector.Bind<B> ().SetFactory((A a) => new B (a));
			} else {
				Injector.Bind<B> ();
			}
			var after = Injector.Resolve<B> ();
			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, Injector.Resolve<B>()));
		}

		[Test]
		public void TestPropertyDependency ()
		{
			var injector = new Injector();

			Bind<C> ().AddPropertyInjector (c => c.MyAProp);
			var before = Injector.Resolve<C> ();

			Bind<A> ();
			var after = Injector.Resolve<C> ();

			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, Injector.Resolve<C>()));
		}

		[Test]
		public void TestFieldDependency ()
		{
			Bind<C> ().AddPropertyInjector (c => c.MyAField);
			var before = Injector.Resolve<C> ();

			Bind<A> ();
			var after = Injector.Resolve<C> ();

			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, Injector.Resolve<C>()));
		}

		[Test]
		public void TestDeepDependencyDependency ()
		{
			Bind<C> ().AddPropertyInjector (c => c.MyAField);

			var before = Injector.Resolve<D> ();

			Bind<A> ();

			var after = Injector.Resolve<D> ();

			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, Injector.Resolve<D>()));
		}

		[Test]
		public void TestOnlyRecompileAffectedGraph ()
		{
			Bind<E> ();
			Bind<C> ().AddPropertyInjector (c => c.MyAField);

			var beforeD = Injector.Resolve<D> ();
			var beforeE = Injector.Resolve<E> ();

			Bind<A> ();

			var afterD = Injector.Resolve<D> ();
			var afterE = Injector.Resolve<E> ();

			Assert.IsFalse (object.ReferenceEquals(beforeD, afterD));
			Assert.IsTrue (object.ReferenceEquals(afterD, Injector.Resolve<D>()));
			Assert.IsTrue (object.ReferenceEquals(beforeE, afterE));
		}
	}
}


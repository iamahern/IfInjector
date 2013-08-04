using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest
{
	[TestFixture()]
	public class RecompilationTests
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
			var injector = Injector.NewInstance();
			var before = injector.Resolve<B> ();
			injector.Bind<B> ();
			var after = injector.Resolve<B> ();
			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, injector.Resolve<B>()));
		}

		[Test]
		public void TestPropertyDependency ()
		{
			var injector = Injector.NewInstance();

			injector.Bind<C> ().AddPropertyInjector (c => c.MyAProp);
			var before = injector.Resolve<C> ();

			injector.Bind<A> ();
			var after = injector.Resolve<C> ();

			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, injector.Resolve<C>()));
		}

		[Test]
		public void TestFieldDependency ()
		{
			var injector = Injector.NewInstance();

			injector.Bind<C> ().AddPropertyInjector (c => c.MyAField);
			var before = injector.Resolve<C> ();

			injector.Bind<A> ();
			var after = injector.Resolve<C> ();

			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, injector.Resolve<C>()));
		}

		[Test]
		public void TestDeepDependencyDependency ()
		{
			var injector = Injector.NewInstance();
			injector.Bind<C> ().AddPropertyInjector (c => c.MyAField);

			var before = injector.Resolve<D> ();

			injector.Bind<A> ();

			var after = injector.Resolve<D> ();

			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, injector.Resolve<D>()));
		}

		[Test]
		public void TestOnlyRecompileAffectedGraph ()
		{
			var injector = Injector.NewInstance();
			injector.Bind<E> ();
			injector.Bind<C> ().AddPropertyInjector (c => c.MyAField);

			var beforeD = injector.Resolve<D> ();
			var beforeE = injector.Resolve<E> ();

			injector.Bind<A> ();

			var afterD = injector.Resolve<D> ();
			var afterE = injector.Resolve<E> ();

			Assert.IsFalse (object.ReferenceEquals(beforeD, afterD));
			Assert.IsTrue (object.ReferenceEquals(afterD, injector.Resolve<D>()));
			Assert.IsTrue (object.ReferenceEquals(beforeE, afterE));
		}
	}
}


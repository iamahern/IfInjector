using NUnit.Framework;
using System;

using IfFastInjector;

namespace IfFastInjectorMxTest
{
	[TestFixture()]
	public class RecompilationTests
	{
		[IfSingleton]
		class A {}

		[IfSingleton]
		class B {
			public B(A a) {}
		}

		[IfSingleton]
		class C {
			public A MyAProp { get; set; }
			public A MyAField = null;
		}

		[IfSingleton]
		class D {
			public D(C c) {}
		}

		[IfSingleton]
		class E {}


		[Test]
		public void TestConstructDependency ()
		{
			var injector = IfInjector.NewInstance();
			var before = injector.Resolve<B> ();
			injector.Bind<B> ();
			var after = injector.Resolve<B> ();
			Assert.IsFalse (object.ReferenceEquals(before, after));
			Assert.IsTrue (object.ReferenceEquals(after, injector.Resolve<B>()));
		}

		[Test]
		public void TestPropertyDependency ()
		{
			var injector = IfInjector.NewInstance();

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
			var injector = IfInjector.NewInstance();

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
			var injector = IfInjector.NewInstance();
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
			var injector = IfInjector.NewInstance();
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


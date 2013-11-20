using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.Bindings.Lifestyles;
using IfInjector.Errors;

namespace IfInjectorTest
{
	[TestFixture()]
	public class GenericBindingTest : BaseTest
	{
		interface IBasic<ArgTP> {}
		class Basic<ArgTP> : IBasic<ArgTP> {}

		[Test()]
		public void TestBasicRegistration ()
		{
			var injector = new Injector ();
			injector.Register (OpenGenericBinding.For (typeof(IBasic<>)).To (typeof(Basic<>)));

			var obj = injector.Resolve<IBasic<object>> ();
			Assert.NotNull (obj);
			Assert.AreEqual (typeof(Basic<object>), obj.GetType ());

			var obj2 = injector.Resolve<IBasic<object>> ();
			Assert.AreNotSame (obj, obj2);

			var ex = injector.Resolve<IBasic<Exception>> ();
			Assert.NotNull (ex);
			Assert.AreEqual (typeof(Basic<Exception>), ex.GetType ());
		}

		[Test()]
		public void TestBasicRegistrationWithLifestyles ()
		{
			var injector = new Injector ();
			injector.Register (OpenGenericBinding.For (typeof(IBasic<>)).To (typeof(Basic<>)).SetLifestyle(Lifestyle.Singleton));

			var obj = injector.Resolve<IBasic<object>> ();
			Assert.NotNull (obj);
			Assert.AreEqual (typeof(Basic<object>), obj.GetType ());

			var obj2 = injector.Resolve<IBasic<object>> ();
			Assert.AreSame (obj, obj2);

		}

		[ImplementedBy(typeof(MImplicit<>))]
		interface IImplicit<ArgTP> {}
		class MImplicit<ArgTP> : IImplicit<ArgTP> {}
		
		[Test()]
		public void TestImplicitRegistration ()
		{
			var injector = new Injector ();

			var obj = injector.Resolve<IImplicit<object>> ();
			Assert.NotNull (obj);
			Assert.AreEqual (typeof(MImplicit<object>), obj.GetType ());

			var ex = injector.Resolve<IImplicit<Exception>> ();
			Assert.NotNull (ex);
			Assert.AreEqual (typeof(MImplicit<Exception>), ex.GetType ());
		}

		interface IImplicitOverride<ArgTP> : IImplicit<ArgTP>{}
		class MImplicitOverride<ArgTP> : IImplicitOverride<ArgTP>{}
		
		[Test()]
		public void TestExplicitSingleBeatsImplicit ()
		{
			var injector = new Injector ();

			injector.Register (Binding.For<IImplicit<object>>().To<MImplicitOverride<object>> ());
			injector.Register (Binding.For<MImplicitOverride<Exception>>());

			// should override
			var obj = injector.Resolve<IImplicit<object>> ();
			Assert.NotNull (obj);
			Assert.AreEqual (typeof(MImplicitOverride<object>), obj.GetType ());

			// should not override since there is an implicit binding present
			var ex = injector.Resolve<IImplicit<Exception>> ();
			Assert.NotNull (ex);
			Assert.AreEqual (typeof(MImplicit<Exception>), ex.GetType ());

			// should resolve via implicit resolution since there is no ImplemtedBy on the key type
			var exover = injector.Resolve<IImplicitOverride<Exception>> ();
			Assert.NotNull (exover);
			Assert.AreEqual (typeof(MImplicitOverride<Exception>), exover.GetType ());
		}

		public void TestExplicitLifeStyle ()
		{
			var injector = new Injector ();

			injector.Register (Binding.For<IImplicit<object>>().To<MImplicitOverride<object>> ());
			injector.Register (Binding.For<MImplicitOverride<Exception>>());

			// should override
			var obj = injector.Resolve<IImplicit<object>> ();
			Assert.NotNull (obj);
			Assert.AreEqual (typeof(MImplicitOverride<object>), obj.GetType ());

			// should not override since there is an implicit binding present
			var ex = injector.Resolve<IImplicit<Exception>> ();
			Assert.NotNull (ex);
			Assert.AreEqual (typeof(MImplicit<Exception>), ex.GetType ());

			// should resolve via implicit resolution since there is no ImplemtedBy on the key type
			var exover = injector.Resolve<IImplicitOverride<Exception>> ();
			Assert.NotNull (exover);
			Assert.AreEqual (typeof(MImplicitOverride<Exception>), exover.GetType ());
		}

		[Test]
		public void TestRejectNonGenericTypeParameter() {
			ExpectError (() => {
				OpenGenericBinding.For(typeof(Exception));
			}, InjectorErrors.ErrorGenericsCannotCreateBindingForNonGeneric, typeof(Exception));
		}

		[Test]
		public void TestRejectClosedGenericTypeParameter() {
			ExpectError (() => {
				OpenGenericBinding.For(typeof(Basic<object>));
			}, InjectorErrors.ErrorGenericsCannotCreateBindingForClosedGeneric, typeof(Basic<object>));
		}

		[Test]
		public void TestRejectNonDerivedToBinding() {
			ExpectError (() => {
				OpenGenericBinding.For(typeof(Basic<>)).To(typeof(MImplicit<>));
			}, InjectorErrors.ErrorGenericsBindToTypeIsNotDerivedFromKey, typeof(Basic<>), typeof(MImplicit<>));
		}

		[ImplementedBy(typeof(MDouble<>))]
		interface IDouble<A,B> {}
		class MDouble<B> : IDouble<object,B>{}

		[Test]
		public void TestRejectIncorrectGenericArgsToBinding() {
			ExpectError (() => {
				OpenGenericBinding.For(typeof(IDouble<,>)).To(typeof(MDouble<>));
			}, InjectorErrors.ErrorGenericsBindToTypeMustHaveSameTypeArgsAsKey, typeof(IDouble<,>), typeof(MDouble<>));
		}

		[Test]
		public void TestRejectImplicitIncorrectTypeArgs() {
			// type args
			ExpectError (() => {
				var injector = new Injector();
				injector.Resolve<IDouble<object,int>>();
			}, InjectorErrors.ErrorGenericsBindToTypeMustHaveSameTypeArgsAsKey, typeof(IDouble<,>), typeof(MDouble<>));
		}

		[ImplementedBy(typeof(MDouble<>))]
		interface ImplicitNotDereived<A> {}

		[Test]
		public void TestRejectImplicitNonDerivedBindingType() {
			// not derived
			ExpectError (() => {
				var injector = new Injector();
				injector.Resolve<ImplicitNotDereived<int>>();
			}, InjectorErrors.ErrorGenericsBindToTypeIsNotDerivedFromKey, typeof(ImplicitNotDereived<>), typeof(MDouble<>));
		}

	}
}


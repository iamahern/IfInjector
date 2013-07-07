using System;
using NUnit.Framework;
using IfFastInjector;
using IfFastInjector.IfInjectorTypes;

using System.Linq.Expressions;

namespace IfFastInjectorMxTest
{
    [TestFixture]
    public class BindingAttributeTest
    {
		IfInjector injector = IfInjector.NewInstance ();

		public BindingAttributeTest() {
			injector.Bind<Inner> ().AsSingleton ();
			injector.Bind<Outer> ().AsSingleton ();
		}

		[Test]
        public void ImplicitBindPublicProperty ()
		{
			var res = injector.Resolve<Outer> ();
			Assert.IsNotNull (res.MyInner);
		}

		[Test]
		public void ImplicitBindPrivateProperty ()
		{
			var res = injector.Resolve<Outer> ();
			Assert.IsNotNull (res.GetMyInnerPrivateProp());
		}

		[Test]
		public void ImplicitBindPrivateField ()
		{
			var res = injector.Resolve<Outer> ();
			Assert.IsNotNull (res.GetMyInnerPrivateField());
		}

		[Test]
		public void ImplicitBindDerivedProperty()
		{
			var res = injector.Resolve<Outer> ();
			Assert.IsNotNull (res.ParentInner);
		}

		[Test]
		public void ImplicitBindDerivedField()
		{
			var res = injector.Resolve<Outer> ();
			Assert.IsNotNull (res.GetMyParentInner());
		}

		[Test]
		public void FactoryConstructorAutoBinding()
		{
			var mInjector = IfInjector.NewInstance ();
			mInjector.Bind<Parent, Outer> (() => new Outer()).AsSingleton();
			mInjector.Bind<Inner> ().AsSingleton ();

			var res = injector.Resolve<Parent> ();
			Assert.IsNotNull (res.FactoryParentInner);
		}

		[Test]
		public void ShadowProperties() {
			var res = injector.Resolve<Outer> ();
			Assert.IsNull (res.ShadowProp);

			Parent resPar = res as Parent;
			Assert.IsNotNull (resPar.ShadowProp);
		}

		class Parent {
			[IfInject]
			public Inner ParentInner { get; set; }

			[IfInject]
			private Inner myParentInner = null;

			public Inner GetMyParentInner() {
				return myParentInner;
			}

			public virtual Inner FactoryParentInner { get; set; }

			[IfInject]
			public Inner ShadowProp { get; set; }
		}

		class Outer : Parent
		{
			[IfInject]
			public Inner MyInner { get; private set; }

			[IfInject]
			private Inner MyInnerPrivate { get; set; }

			[IfInject]
			private Inner myInnerPrivate = null;

			public Inner GetMyInnerPrivateProp ()
			{
				return MyInnerPrivate;
			}

			public Inner GetMyInnerPrivateField ()
			{
				return myInnerPrivate;
			}

			[IfInject]
			public override Inner FactoryParentInner { get; set; }

			public new Inner ShadowProp { get; set; }
		}

		class Inner
		{
		}

#pragma warning disable
		class TestPrimitiveBinding {
			[IfInject]
			int Bad;
		}

		class TestPrimitiveBindingProp {
			[IfInject]
			int Bad { get; set; }
		}

		class TestStuctBinding {
			[IfInject]
			DateTime Bad;
		}

		class TestStuctBindingProp {
			[IfInject]
			DateTime Bad { get; set; }
		}
#pragma warning enable

		[Test]
		public void PrimitiveBindingTest() {
			GenericBadTypeBindingTest<TestPrimitiveBinding> ();
			GenericBadTypeBindingTest<TestPrimitiveBindingProp> ();
		}

		[Test]
		public void StructBindingTest() {
			GenericBadTypeBindingTest<TestStuctBinding> ();
			GenericBadTypeBindingTest<TestStuctBindingProp> ();
		}

		private void GenericBadTypeBindingTest<T>() where T : class {
			try {
				var gbInjector = IfInjector.NewInstance ();
				gbInjector.Bind<T> ();
				Assert.Fail("Attempting to bind should fail");
			} catch (IfFastInjectorException ex) {
				Assert.AreEqual (string.Format(IfFastInjectorErrors.ErrorUnableToBindNonClassFieldsProperties, "Bad", typeof(T).Name), ex.Message);
			}
		}
    }
}

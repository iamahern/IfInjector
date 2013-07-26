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
				Assert.AreEqual (string.Format(IfFastInjectorErrors.ErrorUnableToBindNonClassFieldsProperties.MessageTemplate, "Bad", typeof(T).Name), ex.Message);
			}
		}

		[IfImplementedBy(typeof(MyIFaceImpl))]
		interface MyIFace { }

		[IfImplementedBy(typeof(MyIFaceImpl))]
		class MyIFaceBaseImpl : MyIFace {}

		class MyIFaceImpl : MyIFaceBaseImpl {}

		[Test]
		public void CheckImplementedBy() {
			var mInjector = IfInjector.NewInstance ();
			var res = mInjector.Resolve<MyIFace> ();

			Assert.IsNotNull (res);
			Assert.IsInstanceOf<MyIFaceImpl> (res);

			var res2 = mInjector.Resolve<MyIFaceBaseImpl> ();

			Assert.IsNotNull (res2);
			Assert.IsInstanceOf<MyIFaceImpl> (res2);
		}

		[Test]
		public void CheckImplementedByOverrideAndAmbiguity() {
			var mInjector = IfInjector.NewInstance ();

			// Check - ambiguous situation where Resolve<XXX> may be for type with an @IfImplementedBy; but the user explicitly Bind<YYY> where YYY : XXX.
			mInjector.Bind<MyIFace, MyIFaceImpl>().AsSingleton();
			mInjector.Bind<MyIFaceImpl>().AsSingleton();

			var res1 = mInjector.Resolve<MyIFaceBaseImpl> ();
			var res2 = mInjector.Resolve<MyIFaceBaseImpl> ();
			Assert.IsNotNull (res1);
			Assert.IsInstanceOf<MyIFaceImpl> (res1);
			Assert.IsFalse (object.ReferenceEquals(res1, res2)); // This should use the IfImplementedBy, not the bind statement

			var res3 = mInjector.Resolve<MyIFace> ();
			var res4 = mInjector.Resolve<MyIFace> ();
			Assert.IsNotNull (res3);
			Assert.IsTrue (object.ReferenceEquals(res3, res4));

			var res5 = mInjector.Resolve<MyIFaceBaseImpl> ();
			Assert.IsNotNull (res5);
			Assert.IsFalse (object.ReferenceEquals(res4, res5));
			Assert.IsFalse (object.ReferenceEquals(res1, res5));
		}

		[IfSingleton]
		class MySingletonBase {}
		class MyNonSingletonDerived : MySingletonBase {}

		[Test]
		public void CheckSingletonBehavior() {
			var mInjector = IfInjector.NewInstance ();

			var res1 = mInjector.Resolve<MyNonSingletonDerived> ();
			var res2 = mInjector.Resolve<MyNonSingletonDerived> ();
			Assert.IsNotNull (res1);
			Assert.IsFalse (object.ReferenceEquals(res1, res2));

			var res3 = mInjector.Resolve<MySingletonBase> ();
			var res4 = mInjector.Resolve<MySingletonBase> ();
			Assert.IsNotNull (res3);
			Assert.IsTrue (object.ReferenceEquals(res3, res4));
		}

		[Test]
		public void CheckOverrideSingletonBehavior() {
			var mInjector = IfInjector.NewInstance ();
			mInjector.Bind<MySingletonBase> ().AsSingleton (false);

			var res1 = mInjector.Resolve<MySingletonBase> ();
			var res2 = mInjector.Resolve<MySingletonBase> ();
			Assert.IsNotNull (res1);
			Assert.IsNotNull (res2);
			Assert.IsFalse (object.ReferenceEquals(res1, res2));
		}

    }
}

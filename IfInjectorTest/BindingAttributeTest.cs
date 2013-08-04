using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfInjectorTypes;

using System.Linq.Expressions;

namespace IfInjectorTest
{
    [TestFixture]
    public class BindingAttributeTest
    {
		class Parent {
			[Inject]
			public Inner ParentInner { get; set; }

			[Inject]
			private Inner myParentInner = null;

			public Inner GetMyParentInner() {
				return myParentInner;
			}

			public virtual Inner FactoryParentInner { get; set; }

			[Inject]
			public Inner ShadowProp { get; set; }
		}

		class Outer : Parent
		{
			[Inject]
			public Inner MyInner { get; private set; }

			[Inject]
			private Inner MyInnerPrivate { get; set; }

			[Inject]
			private Inner myInnerPrivate = null;

			public Inner GetMyInnerPrivateProp ()
			{
				return MyInnerPrivate;
			}

			public Inner GetMyInnerPrivateField ()
			{
				return myInnerPrivate;
			}

			[Inject]
			public override Inner FactoryParentInner { get; set; }

			public new Inner ShadowProp { get; set; }
		}

		class Inner
		{
		}

		Injector injector = Injector.NewInstance ();

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
			var mInjector = Injector.NewInstance ();
			mInjector.Bind<Parent, Outer> ().SetFactory(() => new Outer()).AsSingleton();
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
			[Inject]
			int Bad;
		}

		class TestPrimitiveBindingProp {
			[Inject]
			int Bad { get; set; }
		}

		class TestStuctBinding {
			[Inject]
			DateTime Bad;
		}

		class TestStuctBindingProp {
			[Inject]
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
				var gbInjector = Injector.NewInstance ();
				gbInjector.Bind<T> ();
				Assert.Fail("Attempting to bind should fail");
			} catch (InjectorException ex) {
				Assert.AreEqual (string.Format(InjectorErrors.ErrorUnableToBindNonClassFieldsProperties.MessageTemplate, "Bad", typeof(T).FullName), ex.Message);
			}
		}

		[ImplementedBy(typeof(MyIFaceImpl))]
		interface MyIFace { }

		[ImplementedBy(typeof(MyIFaceImpl))]
		class MyIFaceBaseImpl : MyIFace {}

		class MyIFaceImpl : MyIFaceBaseImpl {}

		[Test]
		public void CheckImplementedBy() {
			var mInjector = Injector.NewInstance ();
			var res = mInjector.Resolve<MyIFace> ();

			Assert.IsNotNull (res);
			Assert.IsInstanceOf<MyIFaceImpl> (res);

			var res2 = mInjector.Resolve<MyIFaceBaseImpl> ();

			Assert.IsNotNull (res2);
			Assert.IsInstanceOf<MyIFaceImpl> (res2);
		}

		[Test]
		public void CheckImplementedByOverrideAndAmbiguity() {
			var mInjector = Injector.NewInstance ();

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

		[Singleton]
		class MySingletonBase {}
		class MyNonSingletonDerived : MySingletonBase {}

		[Test]
		public void CheckSingletonBehavior() {
			var mInjector = Injector.NewInstance ();

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
			var mInjector = Injector.NewInstance ();
			mInjector.Bind<MySingletonBase> ().AsSingleton (false);

			var res1 = mInjector.Resolve<MySingletonBase> ();
			var res2 = mInjector.Resolve<MySingletonBase> ();
			Assert.IsNotNull (res1);
			Assert.IsNotNull (res2);
			Assert.IsFalse (object.ReferenceEquals(res1, res2));
		}

    }
}

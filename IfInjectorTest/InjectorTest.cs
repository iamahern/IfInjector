using System;
using NUnit.Framework;
using IfInjector;

namespace IfInjectorTest
{
    [TestFixture]
    public class InjectorTest
    {
        [Test]
        public void ResolveInterface()
        {
			Injector injector = Injector.NewInstance();
			injector.Bind<MyInterface, MyTestClass>();

			var result = injector.Resolve<MyInterface>();

			Assert.IsInstanceOf<MyTestClass>(result);
        }

        [Test]
        public void ResolveDefaultConcrete()
        {
			Injector injector = Injector.NewInstance();
			var result = injector.Resolve<MyTestClass>();
            Assert.IsInstanceOf<MyTestClass>(result);
        }

		[Test]
		public void ResolveConcreteTypeSame() {
			Injector injector = Injector.NewInstance();
			injector.Bind<MyTestClass, MyTestClass>();
			var result = injector.Resolve<MyTestClass>();
			Assert.IsInstanceOf<MyTestClass>(result);
		}

		[Test]
		public void BindConcreteType() {
			var injector = Injector.NewInstance();
			injector.Bind<MyTestClass>();
			var result = injector.Resolve<MyTestClass>();
			Assert.IsInstanceOf<MyTestClass>(result);
		}

        [Test]
        public void InjectProperty()
        {
			var injector = Injector.NewInstance();
			injector
                .Bind<MyInterface, MyTestClass>()
             	.AddPropertyInjector(v => v.MyProperty)
                .AddPropertyInjector(v => v.MyOtherProperty, () => new MyPropertyClass());

			var result = injector.Resolve<MyTestClass>();

            Assert.IsInstanceOf<MyTestClass>(result);
        }

        interface MyInterface
        {
            MyPropertyClass MyProperty { get; set; }
            MyPropertyClass MyOtherProperty { get; set; }
        }

        class MyTestClass : MyInterface
        {
            public MyPropertyClass MyProperty { get; set; }
            public MyPropertyClass MyOtherProperty { get; set; }

			private MyPropertyClass myProperty;
			public void SetMyProperty(MyPropertyClass m) {
				myProperty = m;
			}
			public MyPropertyClass GetMyProperty() {
				return myProperty;
			}

			private MyPropertyClass myOtherProperty;
			public void SetMyOtherProperty(MyPropertyClass m) {
				myOtherProperty = m;
			}
			public MyPropertyClass GetMyOtherProperty() {
				return myOtherProperty;
			}
        }

        class MyPropertyClass
        {

        }

        [Test]
        public void TestSelectConstructorByAttribute()
        {
			var injector = Injector.NewInstance();
			injector.Bind<TestSelectConstructorByAttributeTestClass.IMyOtherInterface, TestSelectConstructorByAttributeTestClass.MyOtherInterfaceClass>();

			var result = injector.Resolve<TestSelectConstructorByAttributeTestClass>();

			Assert.IsInstanceOf<TestSelectConstructorByAttributeTestClass>(result);
            Assert.IsTrue(result.CorrectConstructorWasUsed);
        }

        class TestSelectConstructorByAttributeTestClass
        {
            public TestSelectConstructorByAttributeTestClass()
            {
                CorrectConstructorWasUsed = false;
            }

            [Inject]
            public TestSelectConstructorByAttributeTestClass(IMyOtherInterface dep)
            {
                CorrectConstructorWasUsed = true;
            }

            public bool CorrectConstructorWasUsed { get; set; }

            public interface IMyOtherInterface { }
            public class MyOtherInterfaceClass : IMyOtherInterface { }
        }

        [Test]
        public void TestSelectConstructorByIgnoreAttribute()
        {
			var injector = Injector.NewInstance();
			injector.Bind<TestIgnoreConstructorByAttributeTestClass.IMyOtherInterface, TestIgnoreConstructorByAttributeTestClass.MyOtherInterfaceClass>();

			var result = injector.Resolve<TestIgnoreConstructorByAttributeTestClass>();

			Assert.IsInstanceOf<TestIgnoreConstructorByAttributeTestClass>(result);
            Assert.IsTrue(result.CorrectConstructorWasUsed);
        }

        class TestIgnoreConstructorByAttributeTestClass
        {
            [IgnoreConstructor]
            public TestIgnoreConstructorByAttributeTestClass()
            {
                CorrectConstructorWasUsed = false;
            }

            public TestIgnoreConstructorByAttributeTestClass(IMyOtherInterface dep)
            {
                CorrectConstructorWasUsed = true;
            }

            public bool CorrectConstructorWasUsed { get; set; }

            public interface IMyOtherInterface { }
            public class MyOtherInterfaceClass : IMyOtherInterface { }
        }
    }
}

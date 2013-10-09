using System;
using NUnit.Framework;
using IfInjector;

namespace IfInjectorTest.Basic
{
    [TestFixture]
    public class InjectorTest : Base2WayTest
    {
        [Test]
        public void ResolveInterface()
        {
			Bind<MyInterface, MyTestClass>();

			var result = Injector.Resolve<MyInterface>();

			Assert.IsInstanceOf<MyTestClass>(result);
        }

        [Test]
        public void ResolveDefaultConcrete()
        {
			var result = Injector.Resolve<MyTestClass>();
            Assert.IsInstanceOf<MyTestClass>(result);
        }

		[Test]
		public void ResolveConcreteTypeSame() {
			Bind<MyTestClass, MyTestClass>();
			var result = Injector.Resolve<MyTestClass>();
			Assert.IsInstanceOf<MyTestClass>(result);
		}

		[Test]
		public void BindConcreteType() {
			Bind<MyTestClass>();
			var result = Injector.Resolve<MyTestClass>();
			Assert.IsInstanceOf<MyTestClass>(result);
		}

        [Test]
        public void InjectProperty()
        {
			Bind(MakeBind<MyInterface, MyTestClass>()
             	.AddPropertyInjector(v => v.MyProperty)
                .AddPropertyInjector(v => v.MyOtherProperty, () => new MyPropertyClass()));

			var result = Injector.Resolve<MyTestClass>();

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
			Bind<TestSelectConstructorByAttributeTestClass.IMyOtherInterface, TestSelectConstructorByAttributeTestClass.MyOtherInterfaceClass>();

			var result = Injector.Resolve<TestSelectConstructorByAttributeTestClass>();

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
    }
}

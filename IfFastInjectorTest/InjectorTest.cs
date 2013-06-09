using System;
using NUnit.Framework;
using IfFastInjector;

namespace IfFastInjector
{
    [TestFixture]
    public class InjectorTest
    {
		IfInjector injector = IfInjector.NewInstance();

        [Test]
        public void ResolveInterface()
        {
			injector.Bind<MyInterface, MyTestClass>();

			var result = injector.Resolve<MyInterface>();

			Assert.IsInstanceOf<MyTestClass>(result);
        }

        [Test]
        public void ResolveDefaultConcrete()
        {
			var result = injector.Resolve<MyTestClass>();
            Assert.IsInstanceOf<MyTestClass>(result);
        }

		[Test]
		public void ResolveConcreteTypeSame() {
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass, MyTestClass>();
			var result = injector.Resolve<MyTestClass>();
			Assert.IsInstanceOf<MyTestClass>(result);
		}

		[Test]
		public void BindConcreteType() {
			IfInjector injector = IfInjector.NewInstance();
			injector.Bind<MyTestClass>();
			var result = injector.Resolve<MyTestClass>();
			Assert.IsInstanceOf<MyTestClass>(result);
		}

        [Test]
        public void InjectProperty()
        {
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
        }

        class MyPropertyClass
        {

        }

        [Test]
        public void TestSelectConstructorByAttribute()
        {
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

            [IfInject]
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
			injector.Bind<TestIgnoreConstructorByAttributeTestClass.IMyOtherInterface, TestIgnoreConstructorByAttributeTestClass.MyOtherInterfaceClass>();

			var result = injector.Resolve<TestIgnoreConstructorByAttributeTestClass>();

			Assert.IsInstanceOf<TestIgnoreConstructorByAttributeTestClass>(result);
            Assert.IsTrue(result.CorrectConstructorWasUsed);
        }

        class TestIgnoreConstructorByAttributeTestClass
        {
            [IfIgnoreConstructor]
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

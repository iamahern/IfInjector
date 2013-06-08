using System;
using NUnit.Framework;
using IfFastInjector;

namespace IfFastInjector
{
    [TestFixture]
    public class InjectorTest
    {
		Injector injector = Injector.NewInstance();

        [Test]
        public void ResolveInterface()
        {
			injector.SetResolver<MyInterface, MyTestClass>();

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
        public void InjectProperty()
        {
			injector
                .SetResolver<MyInterface, MyTestClass>()
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
			injector.SetResolver<TestSelectConstructorByAttributeTestClass.IMyOtherInterface, TestSelectConstructorByAttributeTestClass.MyOtherInterfaceClass>();

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
			injector.SetResolver<TestIgnoreConstructorByAttributeTestClass.IMyOtherInterface, TestIgnoreConstructorByAttributeTestClass.MyOtherInterfaceClass>();

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

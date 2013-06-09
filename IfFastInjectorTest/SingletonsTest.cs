using NUnit.Framework;
using System;

using IfFastInjector;

namespace FastInjectorMxTest
{
	[TestFixture()]
	public class SingletonsTest
	{
		[Test()]
		public void TestSingletonWithSetResolver ()
		{
			IfInjector injector = IfInjector.NewInstance ();

			injector.Bind<MyIFace, MyClass> ().AsSingleton ();

			MyIFace inst1 = injector.Resolve<MyIFace> ();
			MyIFace inst2 = injector.Resolve<MyIFace> ();

			Assert.IsTrue(object.ReferenceEquals(inst1, inst2));
		}

		[Test()]
		public void TestSingletonWithForConcreteType ()
		{
			IfInjector injector = IfInjector.NewInstance ();

			injector.Bind<MyClass> ().AsSingleton ();

			MyClass inst1 = injector.Resolve<MyClass> ();
			MyClass inst2 = injector.Resolve<MyClass> ();

			Assert.IsTrue(object.ReferenceEquals(inst1, inst2));
		}

		interface MyIFace {}
		class MyClass : MyIFace { }
	}
}


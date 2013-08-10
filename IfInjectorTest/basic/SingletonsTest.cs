using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest
{
	[TestFixture()]
	public class SingletonsTest : Base2WayTest
	{
		[Test()]
		public void TestSingletonWithSetResolver ()
		{
			Bind<MyIFace, MyClass> ().AsSingleton ();

			MyIFace inst1 = Injector.Resolve<MyIFace> ();
			MyIFace inst2 = Injector.Resolve<MyIFace> ();

			Assert.IsTrue(object.ReferenceEquals(inst1, inst2));
		}

		[Test()]
		public void TestSingletonWithForConcreteType ()
		{
			Bind<MyClass> ().AsSingleton ();

			MyClass inst1 = Injector.Resolve<MyClass> ();
			MyClass inst2 = Injector.Resolve<MyClass> ();

			Assert.IsTrue(object.ReferenceEquals(inst1, inst2));
		}

		interface MyIFace {}
		class MyClass : MyIFace { }
	}
}


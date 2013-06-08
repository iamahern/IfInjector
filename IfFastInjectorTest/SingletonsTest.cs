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

			injector.SetResolver<MyIFace, MyClass> ().AsSingleton ();

			MyIFace inst1 = injector.Resolve<MyIFace> ();
			MyIFace inst2 = injector.Resolve<MyIFace> ();

			Assert.IsTrue(object.ReferenceEquals(inst1, inst2));
		}

		interface MyIFace {}
		class MyClass : MyIFace { }
	}
}


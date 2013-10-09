using NUnit.Framework;
using System;

using IfInjector;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
	[TestFixture()]
	public class SingletonsTest : Base2WayTest
	{
		[Test()]
		public void TestSingletonWithSetResolver ()
		{
			Bind(MakeBind<MyIFace, MyClass> ().AsSingleton ());

			MyIFace inst1 = Injector.Resolve<MyIFace> ();
			MyIFace inst2 = Injector.Resolve<MyIFace> ();

			Assert.IsTrue(object.ReferenceEquals(inst1, inst2));
		}

		[Test()]
		public void TestSingletonWithForConcreteType ()
		{
			Bind(MakeBind<MyClass> ().AsSingleton ());

			MyClass inst1 = Injector.Resolve<MyClass> ();
			MyClass inst2 = Injector.Resolve<MyClass> ();

			Assert.AreSame(inst1, inst2);
		}
		
		public void EnsureInnerSingletonIsSame1() {
			Bind(MakeBind<MyClass> ().AsSingleton ());

			MyClass inst1 = Injector.Resolve<MyClass> ();
			MyClass inst2 = Injector.Resolve<MyTransient> ().MyClass;
			MyClass inst3 = Injector.Resolve<MyTransient> ().MyClass;

			Assert.AreSame (inst1, inst2);
			Assert.AreSame (inst2, inst3);
		}

		public void EnsureInnerSingletonIsSame2() {
			Bind(MakeBind<MyClass> ().AsSingleton ());

			MyClass inst1 = Injector.Resolve<MyTransient> ().MyClass;
			MyClass inst2 = Injector.Resolve<MyClass> ();
			MyClass inst3 = Injector.Resolve<MyTransient> ().MyClass;

			Assert.AreSame (inst1, inst2);
			Assert.AreSame (inst2, inst3);
		}

		interface MyIFace {}
		class MyClass : MyIFace { }

		class MyTransient { 
			[Inject]
			public MyClass MyClass { get; set; }
		}
	}
}


using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfCore;
using IfInjectorTest;

namespace IfInjectorTest.Basic
{
	[TestFixture()]
	public class PropertyInjectionTest : Base2WayTest
	{
		[Test()]
		public void InjectMembersImplicit ()
		{
			Bind<MyClass>()
				.AddPropertyInjector((x) => x.Age, () => 10)
				.AddPropertyInjector((x) => x.Name, () => "Mike");

			var instance = new MyClass ();

			Assert.AreSame(instance, Injector.InjectProperties(instance));

			// Explicit Binding should not affect test
			Assert.AreNotEqual (10, instance.Age);
			Assert.AreNotEqual ("Mike", instance.Name);

			// Only implicit bindings should be considered
			Assert.IsNotNull (instance.MyOtherClass);
			Assert.IsNotNull (instance.GetMyOtherClass2 ());
		}

		[Test()]
		public void InjectMembersExplicit ()
		{
			Bind<MyClass>()
				.AddPropertyInjector((x) => x.Age, () => 10)
				.AddPropertyInjector((x) => x.Name, () => "Mike");

			var instance = new MyClass ();

			Assert.AreSame(instance, Injector.InjectProperties(instance, true));

			// Explicit Binding should not affect test
			Assert.AreEqual (10, instance.Age);
			Assert.AreEqual ("Mike", instance.Name);

			// Only implicit bindings should be considered
			Assert.IsNotNull (instance.MyOtherClass);
			Assert.IsNotNull (instance.GetMyOtherClass2 ());
		}
		
		[Test, Timeout(100)]
		public void TestResolverWithPropertyLooping()
		{
			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcretePropertyLoop).Name);

			try
			{
				var concrete = new ConcretePropertyLoop();
				Injector.InjectProperties(concrete);
			}
			catch (InjectorException ex)
			{
				exception = ex;
			}

			Assert.IsNotNull(exception);
			Assert.AreEqual(expectedErrorMessage, exception.Message);
		}

		[Test, Timeout(100)]
		public void TestMayInjectMembersEvenIfConstructorLoops() 
		{
			if (IsFactory) {
				Injector.Bind<LoopingConstructorOnly> ().SetFactory ((LoopingConstructorOnly lco) => new LoopingConstructorOnly (lco));
			} else {
				Injector.Bind<LoopingConstructorOnly> ();
			}

			bool caughtEx = false;
			try {
				Injector.Resolve<LoopingConstructorOnly>();
			} catch (InjectorException) {
				caughtEx = true;
			}
			Assert.IsTrue (caughtEx);

			var val = new LoopingConstructorOnly ();
			Injector.InjectProperties (val);

			Assert.IsNotNull (val.MCls);
		}


		class LoopingConstructorOnly {

			public LoopingConstructorOnly(){}

			[Inject]
			public LoopingConstructorOnly(LoopingConstructorOnly c) {}

			[Inject]
			public MyClass MCls { get; set; }
		}

		class MyClass {
			public int Age { get; set; }
			public string Name { get; set; }

			[Inject]
			public MyOtherClass MyOtherClass { get; private set; }

			[Inject]
			private MyOtherClass MyOtherClass2 = null;

			public MyOtherClass GetMyOtherClass2() {
				return MyOtherClass2;
			}
		}

		class MyOtherClass {}

		class ConcretePropertyLoop
		{
			[Inject]
			public ConcretePropertyLoop MyTestProperty { get; set; }
		}
	}
}


using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfInjectorTypes;

namespace IfInjectorTest
{
	[TestFixture()]
	public class PropertyInjectionTest
	{
		[Test()]
		public void InjectMembers ()
		{
			var injector = Injector.NewInstance ();
			injector.Bind<MyClass>()
				.AddPropertyInjector<int>((x) => x.Age, () => 10)
				.AddPropertyInjector((x) => x.Name, () => "Mike");

			var instance = new MyClass ();

			Assert.IsTrue (object.ReferenceEquals(instance, injector.InjectProperties(instance)));
			Assert.AreEqual (10, instance.Age);
			Assert.AreSame ("Mike", instance.Name);
		}

		[Test, Timeout(100)]
		public void TestResolverWithPropertyLooping()
		{
			var injector = Injector.NewInstance ();
			injector.Bind<ConcretePropertyLoop>()
				.AddPropertyInjector<ConcretePropertyLoop> (v => v.MyTestProperty);

			//fFastInjector.Injector.InternalResolver<ConcretePropertyLoop>.AddPropertySetter(v => v.MyTestProperty);//, () => Injector.Resolve<ConcretePropertyLoop>());

			InjectorException exception = null;
			var expectedErrorMessage = string.Format(InjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcretePropertyLoop).Name);

			try
			{
				var concrete = new ConcretePropertyLoop();
				injector.InjectProperties(concrete);
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
			var injector = Injector.NewInstance ();
			injector.Bind<LoopingConstructorOnly> ();

			bool caughtEx = false;
			try {
				injector.Resolve<LoopingConstructorOnly>();
			} catch (InjectorException) {
				caughtEx = true;
			}
			Assert.IsTrue (caughtEx);

			var val = new LoopingConstructorOnly ();
			injector.InjectProperties (val);

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
		}

		class ConcretePropertyLoop
		{
			public ConcretePropertyLoop MyTestProperty { get; set; }
		}
	}
}


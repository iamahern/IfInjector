using NUnit.Framework;
using System;

using IfFastInjector;
using IfFastInjector.IfInjectorTypes;

namespace IfFastInjectorMxTest
{
	[TestFixture()]
	public class PropertyInjectionTest
	{
		[Test()]
		public void InjectMembers ()
		{
			var injector = IfInjector.NewInstance ();
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
			var injector = IfInjector.NewInstance ();
			injector.Bind<ConcretePropertyLoop>()
				.AddPropertyInjector<ConcretePropertyLoop> (v => v.MyTestProperty);

			//fFastInjector.Injector.InternalResolver<ConcretePropertyLoop>.AddPropertySetter(v => v.MyTestProperty);//, () => Injector.Resolve<ConcretePropertyLoop>());

			IfFastInjectorException exception = null;
			var expectedErrorMessage = string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected.MessageTemplate, typeof(ConcretePropertyLoop).Name);

			try
			{
				var concrete = new ConcretePropertyLoop();
				injector.InjectProperties(concrete);
			}
			catch (IfFastInjectorException ex)
			{
				exception = ex;
			}

			Assert.IsNotNull(exception);
			Assert.AreEqual(expectedErrorMessage, exception.Message);
		}

		[Test, Timeout(100)]
		public void TestMayInjectMembersEvenIfConstructorLoops() 
		{
			var injector = IfInjector.NewInstance ();
			injector.Bind<LoopingConstructorOnly> ();

			bool caughtEx = false;
			try {
				injector.Resolve<LoopingConstructorOnly>();
			} catch (IfFastInjectorException) {
				caughtEx = true;
			}
			Assert.IsTrue (caughtEx);

			var val = new LoopingConstructorOnly ();
			injector.InjectProperties (val);

			Assert.IsNotNull (val.MCls);
		}


		class LoopingConstructorOnly {

			public LoopingConstructorOnly(){}

			[IfInject]
			public LoopingConstructorOnly(LoopingConstructorOnly c) {}

			[IfInject]
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


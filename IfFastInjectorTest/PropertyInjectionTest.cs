using NUnit.Framework;
using System;

using IfFastInjector;

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
	// DEFECT: cannot handle non reference types
	//			.AddPropertyInjector<int>((x) => x.Age, () => 10)
				.AddPropertyInjector((x) => x.Name, () => "Mike");

			var instance = new MyClass ();

			Assert.IsTrue (object.ReferenceEquals(instance, injector.InjectProperties(instance)));
	//		Assert.AreEqual (10, instance.Age);
			Assert.AreSame ("Mike", instance.Name);
		}

		class MyClass {
			public int Age { get; set; }
			public string Name { get; set; }
		}
	}
}


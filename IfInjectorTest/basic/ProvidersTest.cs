using NUnit.Framework;
using System;
using IfInjector.IfCore;

namespace IfInjectorTest.Basic
{
	[TestFixture()]
	public class ProvidersTest : Base2WayTest
	{
		[Test]
		public void InjectorIsImplicitlyBoundTest() {
			Bind<MyInjectorCls> ();

			var inst = Injector.Resolve<MyInjectorCls> ();
			Assert.IsNotNull (inst.Injector);

			var inst2 = inst.Injector.Resolve<MyInjectorCls> ();
			Assert.AreSame (inst, inst2);
		}

		[Test]
		public void InjectorMayNotBeReboundTest() {
			Bind<MyInjectorCls> ();

			try {
				Bind<IfInjector.Injector> ();
				Assert.Fail("Error expected.");
			} catch (InjectorException ex) {
				Assert.AreEqual (InjectorErrors.ErrorMayNotBindInjector, ex.ErrorType);
			}
		}

		[IfInjector.Singleton]
		class MyInjectorCls {
			[IfInjector.Inject]
			public IfInjector.Injector Injector { get; set; }
		}
	}
}


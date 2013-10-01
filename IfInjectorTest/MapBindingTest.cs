using System;
using System.Collections.Generic;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfCore;
using IfInjectorTest;

using System.Linq.Expressions;

namespace IfInjectorTest
{
	[TestFixture()]
	public class MapBindingTest
	{
		[Test()]
		public void TestBasicBindings ()
		{
			var injector = new Injector ();

			injector.Bind<ID, D> ();

			var binding = injector.BindDictionary<string, IFace> ();
			binding.AddBinding<A> ("A");
			binding.AddBinding<B> ("B");
			binding.AddBinding<C> ("C").AddPropertyInjector (o => o.A);

			var dict = injector.Resolve<Dictionary<string, IFace>> ();
			Assert.NotNull (dict);
			Assert.AreEqual (3, dict.Count);

			foreach (var kv in dict) {
				Assert.AreEqual (kv.Key, kv.Value.GetType ().Name);
			}
		}

		[Test()]
		public void TestOldStyleMapBinding ()
		{
			var injector = new Injector ();
			injector.Bind<Dictionary<string, IFace>> ().SetFactory (() => Fact());

			var d2 = injector.Resolve<Dictionary<string, IFace>> ();
			Assert.AreEqual (1, d2.Count);
		}

		Dictionary<string, IFace> Fact() {
			var dict = new Dictionary<string, IFace>();
			dict.Add("A", new A());
			return dict;
		}

		interface IFace {
			object Get ();
		}

		[Singleton]
		class A : IFace {
			public object Get() {
				return this;
			}
		}
		
		class B : IFace {
			[Inject]
			private ID d = null;

			public object Get() {
				return d;
			}
		}

		[Singleton]
		class C : IFace {
			public A A { get; private set; }

			public object Get() {
				return A.Get();
			}
		}

		interface ID {}
		[Singleton]
		class D : ID {}
	}
}


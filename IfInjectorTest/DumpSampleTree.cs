using NUnit.Framework;
using System;
using System.Linq.Expressions;

using IfInjector;
using IfInjector.Bindings.Config;

namespace IfInjectorTest
{
	[TestFixture()]
	public class DumpSampleTree
	{
		class EV : ExpressionVisitor {
			public override Expression Visit(Expression node) {
				Console.WriteLine ("+++++++++" + node.NodeType);
				return base.Visit(node);
			}
		}

		class A {}

		[Singleton]
		class B {
			[Inject]
			public B(A a) { }
		}

		class C {
			[Inject]
			public A A { get; set; }

			[Inject]
			public B B { get; set; }
		
			[Inject]
			public C(B b) { }
		}

		[Test()]
		public void TestCase ()
		{
			var injector = new Injector ();
			injector.Register (Binding.For<C> ());

			var c = injector.Resolve<C>();

			var expr = injector.ResolveResolverExpression (BindingKey<C>.InstanceKey);
			var visitor = new EV ();
			visitor.Visit (expr);
		}
	}
}


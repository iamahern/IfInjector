using NUnit.Framework;
using System;

using IfInjector.Bindings.Config;

namespace IfInjectorTest.Bindings.Config
{
	[TestFixture]
	public class BindingKeyTest
	{
		[Test]
		public void TestGet_Equals_HashCode ()
		{
			BindingKey b1 = BindingKey.Get<object> ();
			BindingKey b2 = BindingKey.Get<object> ();

			Assert.AreEqual (b1, b2);
			Assert.AreEqual (b1.GetHashCode (), b2.GetHashCode ());

			BindingKey b1Ex = BindingKey.Get<Exception> ();
			BindingKey b2Ex = BindingKey.Get<Exception> ();

			Assert.AreEqual (b1Ex, b2Ex);
			Assert.AreEqual (b1Ex.GetHashCode(), b2Ex.GetHashCode());

			Assert.AreNotEqual (b1, b1Ex);
			Assert.AreNotEqual (b2, b2Ex);
		}
	}
}
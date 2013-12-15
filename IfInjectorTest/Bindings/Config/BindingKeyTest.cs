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
			BindingKey b1 = BindingKey.Get(typeof(object));
			BindingKey b2 = BindingKey<object>.InstanceKey;

			Assert.AreEqual (false, b1.Member);
			Assert.AreEqual (false, b2.Member);
			Assert.AreEqual (null, b1.Qualifier);
			Assert.AreEqual (null, b2.Qualifier);

			Assert.AreEqual (b1, b2);
			Assert.AreEqual (b1.GetHashCode (), b2.GetHashCode ());

			BindingKey b1Ex = BindingKey.Get(typeof(Exception));
			BindingKey b2Ex = BindingKey<Exception>.InstanceKey;

			Assert.AreEqual (b1Ex, b2Ex);
			Assert.AreEqual (b1Ex.GetHashCode(), b2Ex.GetHashCode());

			Assert.AreNotEqual (b1, b1Ex);
			Assert.AreNotEqual (b2, b2Ex);
		}

		[Test]
		public void TestGetWithMember_Equals_HashCode()
		{
			BindingKey b1 = BindingKey.Get(typeof(object));
			BindingKey b1i = BindingKey.Get(typeof(object), true);
			BindingKey b2i = BindingKey<object>.MembersKey;

			Assert.AreEqual (true, b1i.Member);
			Assert.AreEqual (true, b2i.Member);
			Assert.AreEqual (null, b1i.Qualifier);
			Assert.AreEqual (null, b2i.Qualifier);

			Assert.AreEqual (b1i, b2i);
			Assert.AreEqual (b1i.GetHashCode (), b2i.GetHashCode ());
			Assert.AreNotEqual (b1, b1i);
			Assert.AreNotEqual (b1.GetHashCode (), b1i.GetHashCode ());

			BindingKey b1Ex = BindingKey.Get(typeof(Exception));
			BindingKey b1Exi = BindingKey.Get(typeof(Exception), true);
			BindingKey b2Exi = BindingKey<Exception>.MembersKey;

			Assert.AreEqual (b1Exi, b2Exi);
			Assert.AreEqual (b1Exi.GetHashCode(), b2Exi.GetHashCode());
			Assert.AreNotEqual (b1Ex.GetHashCode (), b1Exi.GetHashCode ());

			Assert.AreNotEqual (b1i, b1Exi);
			Assert.AreNotEqual (b2i, b2Exi);
		}

		[Test]
		public void TestGetWithQualifer_Equals_HashCode()
		{
			BindingKey b1 = BindingKey.Get(typeof(object));
			BindingKey b1q = BindingKey.Get(typeof(object), "foo");
			BindingKey b2q = BindingKey<object>.Get("foo");

			Assert.AreEqual (false, b1q.Member);
			Assert.AreEqual (false, b2q.Member);
			Assert.AreEqual ("foo", b1q.Qualifier);
			Assert.AreEqual ("foo", b2q.Qualifier);

			Assert.AreEqual (b1q, b2q);
			Assert.AreEqual (b1q.GetHashCode (), b2q.GetHashCode ());
			Assert.AreNotEqual (b1, b1q);
			Assert.AreNotEqual (b1.GetHashCode (), b1q.GetHashCode ());

			BindingKey b1Ex = BindingKey.Get(typeof(Exception));
			BindingKey b1Exq = BindingKey.Get(typeof(Exception), "foo");
			BindingKey b2Exq = BindingKey<Exception>.Get("foo");

			Assert.AreEqual (b1Exq, b2Exq);
			Assert.AreEqual (b1Exq.GetHashCode(), b2Exq.GetHashCode());
			Assert.AreNotEqual (b1Ex.GetHashCode (), b1Exq.GetHashCode ());

			Assert.AreNotEqual (b1q, b1Exq);
			Assert.AreNotEqual (b2q, b2Exq);
		}
	}
}
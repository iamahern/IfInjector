using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class SingletonsTest : IfInjectorTest.Basic.SingletonsTest
	{
		public SingletonsTest() {
			IsFactory = true;
		}
	}
}


using NUnit.Framework;
using System;

using IfInjector;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class SingletonsTest : IfInjectorTest.SingletonsTest
	{
		public SingletonsTest() {
			IsFactory = true;
		}
	}
}


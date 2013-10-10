using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class LifestyleTest : IfInjectorTest.Basic.LifestyleTest
	{
		public LifestyleTest() {
			IsFactory = true;
		}
	}
}


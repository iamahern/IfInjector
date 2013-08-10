using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class ImplicitTypeTest : IfInjectorTest.ImplicitTypeTest
	{
		public ImplicitTypeTest() {
			IsFactory = true;
		}
	}
}


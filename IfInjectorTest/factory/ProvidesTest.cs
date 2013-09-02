using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class ProvidesTest : IfInjectorTest.Basic.ProvidersTest
	{
		public ProvidesTest() {
			IsFactory = true;
		}
	}
}


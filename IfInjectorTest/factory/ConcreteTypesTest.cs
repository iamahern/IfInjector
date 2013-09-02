using NUnit.Framework;
using System;

using IfInjectorTest.Basic;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class ConcreteTypesTest : IfInjectorTest.Basic.ConcreteTypesTest
	{
		public ConcreteTypesTest() {
			IsFactory = true;
		}
	}
}


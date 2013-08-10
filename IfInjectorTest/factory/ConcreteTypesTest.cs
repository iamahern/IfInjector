using NUnit.Framework;
using System;

using IfInjectorTest;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class ConcreteTypesTest : IfInjectorTest.ConcreteTypesTest
	{
		public ConcreteTypesTest() {
			IsFactory = true;
		}
	}
}


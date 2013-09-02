using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class BindingAttributeTest : IfInjectorTest.Basic.BindingAttributeTest
	{
		public BindingAttributeTest() {
			IsFactory = true;
		}
	}
}


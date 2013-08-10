using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class BindingAttributeTest : IfInjectorTest.BindingAttributeTest
	{
		public BindingAttributeTest() {
			IsFactory = true;
		}
	}
}


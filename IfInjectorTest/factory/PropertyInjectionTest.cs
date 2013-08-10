using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfInjectorTypes;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class PropertyInjectionTest : IfInjectorTest.PropertyInjectionTest
	{
		public PropertyInjectionTest() {
			IsFactory = true;
		}
	}
}


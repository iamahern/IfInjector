using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfCore;

namespace IfInjectorTest.Factory
{
	[TestFixture()]
	public class PropertyInjectionTest : IfInjectorTest.Basic.PropertyInjectionTest
	{
		public PropertyInjectionTest() {
			IsFactory = true;
		}
	}
}


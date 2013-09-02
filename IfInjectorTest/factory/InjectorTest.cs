using System;
using NUnit.Framework;
using IfInjector;

namespace IfInjectorTest.Factory
{
    [TestFixture]
	public class InjectorTest : IfInjectorTest.Basic.InjectorTest
    {
		public InjectorTest() {
			IsFactory = true;
		}
    }
}

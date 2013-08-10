using System;
using NUnit.Framework;
using IfInjector;

namespace IfInjectorTest.Factory
{
    [TestFixture]
	public class InjectorTest : IfInjectorTest.InjectorTest
    {
		public InjectorTest() {
			IsFactory = true;
		}
    }
}

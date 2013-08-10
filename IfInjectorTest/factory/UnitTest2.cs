using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfInjectorTypes;

namespace IfInjectorTest.Factory
{
    [TestFixture]
    public class UnitTest2 : IfInjectorTest.UnitTest2
    {
		public UnitTest2() {
			IsFactory = true;
		}
    }
}

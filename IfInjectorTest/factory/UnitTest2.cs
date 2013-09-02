using System;
using NUnit.Framework;
using IfInjector;
using IfInjector.IfCore;

namespace IfInjectorTest.Factory
{
    [TestFixture]
    public class UnitTest2 : IfInjectorTest.Basic.UnitTest2
    {
		public UnitTest2() {
			IsFactory = true;
		}
    }
}

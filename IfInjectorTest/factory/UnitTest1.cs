using System;
using NUnit.Framework;
using IfInjector;
using System.Diagnostics;

namespace IfInjectorTest.Factory
{
    [TestFixture]
	public class UnitTest1 : IfInjectorTest.UnitTest1
    {
		public UnitTest1() {
			IsFactory = true;
		}
    }
}

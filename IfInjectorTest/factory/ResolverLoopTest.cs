using System;
using NUnit.Framework;

namespace IfInjectorTest.Factory
{
    [TestFixture]
    public class ResolverLoopTest : IfInjectorTest.Basic.ResolverLoopTest
    {
		public ResolverLoopTest() {
			IsFactory = true;
		}
    }
}

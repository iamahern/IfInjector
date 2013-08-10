using System;
using NUnit.Framework;

namespace IfInjectorTest.Factory
{
    [TestFixture]
    public class ResolverLoopTest : IfInjectorTest.ResolverLoopTest
    {
		public ResolverLoopTest() {
			IsFactory = true;
		}
    }
}

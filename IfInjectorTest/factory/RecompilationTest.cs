using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory 
{
	[TestFixture()]
	public class RecompilationTest : IfInjectorTest.Basic.RecompilationTest
	{
		public RecompilationTest() {
			IsFactory = true;
		}
	}
}
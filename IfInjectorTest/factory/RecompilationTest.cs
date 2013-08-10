using NUnit.Framework;
using System;

namespace IfInjectorTest.Factory 
{
	[TestFixture()]
	public class RecompilationTest : IfInjectorTest.RecompilationTest
	{
		public RecompilationTest() {
			IsFactory = true;
		}
	}
}
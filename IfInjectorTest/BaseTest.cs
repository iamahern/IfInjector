using NUnit.Framework;
using System;

using IfInjector;
using IfInjector.IfCore;

namespace IfInjectorTest
{
	public abstract class BaseTest
	{
		protected void ExpectError(Action closure, InjectorError errorType, params object[] args) {
			try {
				closure();
				Assert.Fail("Exception expected.");
			} catch (InjectorException ex) {
				Assert.AreSame (errorType, ex.ErrorType);
				Assert.AreEqual (errorType.FormatEx (args).Message, ex.Message);
			}
		}
	}
}


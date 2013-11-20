using System;

namespace IfInjector
{
	/// <summary>
	/// Implemented by attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ImplementedByAttribute : Attribute {
		private readonly Type implementor;
		public ImplementedByAttribute(Type implementor) {
			this.implementor = implementor;
		}

		public Type Implementor { get { return implementor; } }
	}
}


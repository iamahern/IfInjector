using System;

namespace IfInjector.Bindings.Fluent.OpenGeneric
{
	/// <summary>
	/// Ongoing open generic binding.
	/// </summary>
	public interface IOngoingOpenGenericBinding : IOpenGenericBinding {
		/// <summary>
		/// Binds the open generic to a particular concrete type.
		/// </summary>
		/// <param name="concreteType">Concrete type.</param>
		IOpenGenericBinding To (Type concreteType);
	}
}


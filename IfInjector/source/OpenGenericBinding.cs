using System;
using IfInjector.Bindings.Fluent.OpenGeneric;

namespace IfInjector
{
	/// <summary>
	/// Open generic binding.
	/// </summary>
	public static class OpenGenericBinding {

		/// <summary>
		/// Create a binding for the specified binding type.
		/// </summary>
		/// <param name="bindingType">Binding type.</param>
		public static IOngoingOpenGenericBinding For(Type bindingType) {
			return new OngoingOpenGenericBinding (bindingType);
		}
	}
}


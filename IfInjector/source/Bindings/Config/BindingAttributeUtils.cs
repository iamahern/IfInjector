using System;
using System.Linq;

namespace IfInjector.Bindings.Config
{
	internal static class BindingAttributeUtils
	{
		/// <summary>
		/// Gets if implemented by.
		/// </summary>
		/// <returns>The if implemented by.</returns>
		/// <param name="bindingType">Binding type.</param>
		internal static Type GetImplementedBy(Type bindingType) {
			var implTypeAttr = bindingType.GetCustomAttributes(typeof(ImplementedByAttribute), false).FirstOrDefault();
			if (implTypeAttr != null) {
				return (implTypeAttr as ImplementedByAttribute).Implementor;
			}

			return null;
		}
	}
}


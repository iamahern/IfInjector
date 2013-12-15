using System;
using IfInjector.Bindings.Config;
using IfInjector.Errors;

namespace IfInjector.Bindings.Fluent.OpenGeneric
{
	/// <summary>
	/// Ongoing open generic binding.
	/// </summary>
	internal class OngoingOpenGenericBinding : BoundOpenGenericBinding, IOngoingOpenGenericBinding {
		internal OngoingOpenGenericBinding(Type bindingType) : base(bindingType, bindingType) {}

		public IBoundOpenGenericBinding To(Type concreteType) {
			ValidateGenericType (concreteType);
			ValidateLinage (concreteType, concreteType);
			ValidateCompatibleGenericArgs (concreteType);

			return new BoundOpenGenericBinding (BindingKey.BindingType, concreteType);
		}

		private void ValidateLinage(Type nConcreteType, Type originalCheckType) {
			var bindingType = this.BindingKey.BindingType;

			// Check same generic base
			if (nConcreteType.IsGenericType && nConcreteType.GetGenericTypeDefinition () == bindingType) {
				return;
			}

			// check extend interface
			foreach (var it in nConcreteType.GetInterfaces()) {
				if (it.IsGenericType && it.GetGenericTypeDefinition () == bindingType) {
					return;
				}
			}

			// recurse
			Type nConcreteTypeBase = nConcreteType.BaseType;
			if (nConcreteTypeBase == null) {
				throw InjectorErrors.ErrorGenericsBindToTypeIsNotDerivedFromKey.FormatEx (bindingType, originalCheckType);
			}

			ValidateLinage (nConcreteTypeBase, originalCheckType);
		}

		private void ValidateCompatibleGenericArgs(Type nConcreteType) {
			var bindingType = this.BindingKey.BindingType;
			if (bindingType.GetGenericArguments().Length == nConcreteType.GetGenericArguments().Length) {
				return; // TODO: Naive
			}

			throw InjectorErrors.ErrorGenericsBindToTypeMustHaveSameTypeArgsAsKey.FormatEx (bindingType, nConcreteType);
		}
	}
}
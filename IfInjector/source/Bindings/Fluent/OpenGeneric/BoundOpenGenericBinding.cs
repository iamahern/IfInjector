using System;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Lifestyles;
using IfInjector.Bindings.Fluent;
using IfInjector.Errors;

namespace IfInjector.Bindings.Fluent.OpenGeneric
{
	/// <summary>
	/// Open generic binding.
	/// </summary>
	internal class BoundOpenGenericBinding : IBoundOpenGenericBinding, IBindingInternal {
		public IBindingConfig BindingConfig { get; private set; }
		public BindingKey BindingKey { get; private set; }
		public Type ConcreteType { get; private set; }

		internal BoundOpenGenericBinding(Type bindingType, Type concreteType) {
			ValidateGenericType (bindingType);
			ValidateGenericType (concreteType);

			BindingConfig = new BindingConfig (concreteType);
			BindingKey = BindingKey.Get (bindingType);
			ConcreteType = concreteType;
		}

		public IBoundOpenGenericBinding SetLifestyle (Lifestyle lifestyle) {
			BindingConfig.Lifestyle = lifestyle;
			return this;
		}

		protected void ValidateGenericType(Type type) {
			if (!type.IsGenericType) {
				throw InjectorErrors.ErrorGenericsCannotCreateBindingForNonGeneric.FormatEx (type);
			}

			if (!type.IsGenericTypeDefinition) {
				throw InjectorErrors.ErrorGenericsCannotCreateBindingForClosedGeneric.FormatEx (type);
			}
		}
	}
}
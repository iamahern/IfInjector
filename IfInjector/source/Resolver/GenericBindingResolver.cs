using System;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;
using IfInjector.Bindings.Fluent.Concrete;
using IfInjector.Errors;
using IfInjector.Util;

namespace IfInjector.Resolver
{
	/// <summary>
	/// Generic binding resolver. This class will auto-register explicit open-generic bindings as needed.
	/// </summary>
	internal class GenericBindingResolver : IBindingResolver
	{
		/// <summary>
		/// Internal binding class for generic resolved binding.
		/// </summary>
		private class GenericBinding : IBindingInternal, IBinding {
			/// <inheritdoc/>
			public IBindingConfig BindingConfig { get; set; }

			/// <inheritdoc/>
			public BindingKey BindingKey { get; set; }

			/// <inheritdoc/>
			public Type ConcreteType { get; set; }
		}


		private readonly SafeDictionary<BindingKey, IBindingConfig> allGenericResolvers;
		private readonly IInjector injector;

		internal GenericBindingResolver (IInjector injector, object syncLock)
		{
			this.injector = injector;
			this.allGenericResolvers = new SafeDictionary<BindingKey, IBindingConfig> (syncLock);
		}

		/// <summary>
		/// Register the specified openGeneric binding.
		/// </summary>
		/// <param name="openGenericBinding">Open generic binding.</param>
		internal void Register (IBindingInternal openGenericBinding) {
			allGenericResolvers.Add (openGenericBinding.BindingKey, openGenericBinding.BindingConfig);
		}

		/// <inheritdoc/>
		public BindingKey ResolveBinding (BindingKey explicitKey) {
			var bindingType = explicitKey.BindingType;

			if (bindingType.IsGenericType) {
				if (bindingType.IsGenericTypeDefinition) {
					throw InjectorErrors.ErrorGenericsCannotResolveOpenType.FormatEx (bindingType);
				}

				return ResolveBindingForGeneric (explicitKey, bindingType);
			}

			return null;
		}

		private BindingKey ResolveBindingForGeneric(BindingKey explicitKey, Type bindingType) {
			var genericBindingKey = BindingKey.Get (bindingType.GetGenericTypeDefinition (), explicitKey.Qualifier);
			var genericBindingType = bindingType.GetGenericTypeDefinition ();
			var genericTypeArguments = bindingType.GetGenericArguments ();

			IBindingConfig genericBindingConfig;
			Type genericConcreteType = GetGenericImplementation (genericBindingKey, genericBindingType, out genericBindingConfig);

			// Have 'implementedBy OR explicit binding'
			if (genericConcreteType != null) {
				OpenGenericBinding.For (genericBindingType).To (genericConcreteType); // validate binding
				Type concreteType = genericConcreteType.MakeGenericType (genericTypeArguments);

				var binding = new GenericBinding () {
					BindingConfig = new BindingConfig(concreteType),
					BindingKey = explicitKey.ToImplicit(),
					ConcreteType = concreteType
				};

				if (genericBindingConfig != null) {
					binding.BindingConfig.Lifestyle = genericBindingConfig.Lifestyle;
				}

				injector.Register (binding);

				return binding.BindingKey;
			}

			return null;
		}

		/// <summary>
		/// Gets the generic implementation.
		/// </summary>
		/// <returns>The generic implementation.</returns>
		/// <param name="genericBindingKey">Generic binding key.</param>
		/// <param name="genericBindingType">Generic binding type.</param>
		/// <param name="genericBindingConfig">Generic binding config.</param>
		private Type GetGenericImplementation (BindingKey genericBindingKey, Type genericBindingType, out IBindingConfig genericBindingConfig) {
			genericBindingConfig = null;

			// Try registrations
			if (allGenericResolvers.TryGetValue (genericBindingKey, out genericBindingConfig)) {
				return genericBindingConfig.ConcreteType;
			}

			// Try implicit
			return BindingAttributeUtils.GetImplementedBy (genericBindingType);
		}
	}
}
using System;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent.OpenGeneric;
using IfInjector.Util;

namespace IfInjector.Resolver
{
	internal class GenericBindingResolver : IBindingResolver
	{
		private readonly Injector injector;

		internal GenericBindingResolver (Injector injector)
		{
			this.injector = injector;
		}

		/// <inheritdoc/>
		public BindingKey ResolveBinding (BindingKey explicitKey) {

			return null;
		}

		/// <summary>
		/// Register the specified openGeneric binding.
		/// </summary>
		/// <param name="openGenericBinding">Open generic binding.</param>
		internal void Register (IOpenGenericBinding openGenericBinding) {

		}
	}
}


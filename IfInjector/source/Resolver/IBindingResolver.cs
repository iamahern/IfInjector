using System;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent.Concrete;

namespace IfInjector.Resolver
{
	/// <summary>
	/// Helper type for implicit resolution.
	/// </summary>
	internal interface IBindingResolver
	{
		/// <summary>
		/// Resolves the binding.
		/// </summary>
		/// <returns>The binding.</returns>
		/// <param name="explicitKey">Explicit key.</param>
		BindingKey ResolveBinding (BindingKey explicitKey);
	}
}
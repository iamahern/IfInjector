using System;
using IfInjector.Bindings.Config;

namespace IfInjector.Bindings.Fluent
{
	/// <summary>
	/// Internal interface for working with binding objects.
	/// </summary>
	internal interface IBindingInternal {
		/// <summary>
		/// Gets the binding config.
		/// </summary>
		/// <value>The binding config.</value>
		IBindingConfig BindingConfig { get; }

		/// <summary>
		/// Gets the binding key.
		/// </summary>
		/// <value>The binding key.</value>
		BindingKey BindingKey { get; }

		/// <summary>
		/// Gets the type of the concrete implementation.
		/// </summary>
		/// <value>The type of the bind to.</value>
		Type ConcreteType { get; }
	}
}
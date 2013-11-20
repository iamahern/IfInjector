using System;

using IfInjector.Bindings.Fluent.Concrete;
using IfInjector.Bindings.Fluent.OpenGeneric;
using IfInjector.Bindings.Fluent.Properties;

namespace IfInjector
{
	/// <summary>
	/// Injector Interface.
	/// </summary>
	public interface IInjector {
		/// <summary>
		/// Resolve this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		T Resolve<T> () where T : class; 

		/// <summary>
		/// Resolve the specified type.
		/// </summary>
		/// <param name="type">Type.</param>
		object Resolve (Type type);

		/// <summary>
		/// Injects the properties of an instance. By default, this will only inject 'implicitly' bound properties (ones bound by annotation). You may choose to allow this to use explicit bindings if desired.
		/// </summary>
		/// <returns>The properties.</returns>
		/// <param name="instance">Instance.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		T InjectProperties<T> (T instance)
			where T : class;

		/// <summary>
		/// Bind the specified binding.
		/// </summary>
		/// <param name="binding">Binding.</param>
		void Register (IBinding binding);

		/// <summary>
		/// Register the specified properties binding.
		/// </summary>
		/// <param name="propertiesBinding">Properties binding.</param>
		void Register (IPropertiesBinding propertiesBinding);

		/// <summary>
		/// Register the specified openGenericBinding.
		/// </summary>
		/// <param name="openGenericBinding">Open generic binding.</param>
		void Register (IOpenGenericBinding openGenericBinding);

		/// <summary>
		/// Verify that all bindings all valid.
		/// </summary>
		void Verify ();
	}
}


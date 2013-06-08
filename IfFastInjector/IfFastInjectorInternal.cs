using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

/// <summary>
/// NOTE: no actual implementation
/// I was was coding these interfaces before rebasing on top of 'fFastInjector' to use as a starting point.
/// 
/// I intend to merge the good parts of this with IFFastInjector
/// </summary>
namespace FastInjectorFuture
{
	/// <summary>
	/// Attribute for interfaces to allow implicit binding.
	/// </summary>
	[AttributeUsage(AttributeTargets.Interface)]
	public class ImplementedByAttribute : System.Attribute {
		private readonly Type implementation;

		public ImplementedByAttribute(Type implementation) {
			this.implementation = implementation;
		}

		public Type Implementation {
			get {
				return implementation;
			}
		}
	}

	/// <summary>
	/// Marker attribute for concrete classes to flag them as singletons.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class SingletonAttribute : System.Attribute {} 

	/// <summary>
	/// Marker for constructors, fields and properties to request injection.
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field)]
	public class InjectAttribute : System.Attribute {} 


	/// <summary>
	/// I provider.
	/// </summary>
	public interface IProvider<KeyType> {
		KeyType Get();
	}

	/// <summary>
	/// Binding modifiers.
	/// </summary>
	public interface IBinderModifiers {
		void AsSingleton ();
	}

	/// <summary>
	/// The binder interface. The binder is responsible for binding  
	/// </summary>
	public interface IBinder {
		IBinderModifiers Bind<KeyType, ConcreteType> () 
			where ConcreteType : class, KeyType;

		/// <summary>
		/// Bind The concrete type.
		/// </summary>
		/// <typeparam name="ConcreteType">The 1st type parameter.</typeparam>
		IBinderModifiers Bind<ConcreteType> ()
			where ConcreteType : class;

		/// <summary>
		/// Binds the provider.
		/// </summary>
		/// <returns>The provider.</returns>
		/// <param name="provider">Provider.</param>
		/// <typeparam name="KeyType">The 1st type parameter.</typeparam>
		IBinderModifiers BindProvider<KeyType> (IProvider<KeyType> provider);

		/// <summary>
		/// Binds the instance using the specified 'KeyType'
		/// </summary>
		/// <param name="instance">Instance.</param>
		/// <typeparam name="KeyType">The 1st type parameter.</typeparam>
		/// <typeparam name="ConcreteType">The 2nd type parameter.</typeparam>
		void BindInstance<KeyType, ConcreteType>(ConcreteType instance)
			where ConcreteType : class, KeyType;

		/// <summary>
		/// Binds the instance.
		/// </summary>
		/// <param name="instance">Instance.</param>
		/// <typeparam name="ConcreteType">The 1st type parameter.</typeparam>
		void BindInstance<ConcreteType>(ConcreteType instance)
			where ConcreteType : class;
	}

	/// <summary>
	/// The injector interface. Injectors are responsible for resolving new types or 
	/// </summary>
	public interface IInjector
	{
		/// <summary>
		/// Resolve this instance.
		/// </summary>
		/// <typeparam name="TP">The 1st type parameter.</typeparam>
		TP Resolve<TP> ();

		/// <summary>
		/// Resolves the provider.
		/// </summary>
		/// <returns>The provider.</returns>
		/// <typeparam name="TP">The 1st type parameter.</typeparam>
		Func<TP> ResolveProvider<TP> ();

		/// <summary>
		/// Injects the members of this instance.
		/// </summary>
		/// <param name="instance">Instance.</param>
		void InjectMembers(object instance);

		/// <summary>
		/// Injects the static members of the provided type.
		/// </summary>
		/// <param name="type">Type.</param>
		void InjectStaticMembers (Type type);
	}

	/// <summary>
	/// Base injector exception.
	/// </summary>
	public abstract class BaseInjectorException : Exception {
		public BaseInjectorException () : base() {}
		public BaseInjectorException (string message) : base(message) {}
		public BaseInjectorException (string message, Exception innerException) : base (message, innerException) {}
	}

	/// <summary>
	/// Injection exception.
	/// </summary>
	public class InjectionException : BaseInjectorException {
		public InjectionException () : base() {}
		public InjectionException (string message) : base(message) {}
		public InjectionException (string message, Exception innerException) : base (message, innerException) {}
	}

	/// <summary>
	/// Binding exception.
	/// </summary>
	public class BindingException : BaseInjectorException {
		public BindingException () : base() {}
		public BindingException (string message) : base(message) {}
		public BindingException (string message, Exception innerException) : base (message, innerException) {}
	}
}
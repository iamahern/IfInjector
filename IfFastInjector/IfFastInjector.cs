using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IfFastInjector
{
	/// <summary>
	/// Inject attribute. Used to flag constructors for preferred injection. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor)]
	public class IfInjectAttribute : Attribute {}

	/// <summary>
	/// Ignore constructor attribute. Used to flage constructors to be ignored.
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor)]
	public class IfIgnoreConstructorAttribute : Attribute {}

	/// <summary>
	/// Injector.
	/// </summary>
	public abstract class IfInjector
	{
		/// <summary>
		/// News the instance.
		/// </summary>
		/// <returns>The instance.</returns>
		public static IfInjector NewInstance ()
		{
			return new IfFastInjectorInternal.InjectorInternal ();
		}

		/// <summary>
		/// Resolve this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public abstract T Resolve<T> () where T : class;

		/// <summary>
		/// Resolve the specified type.
		/// </summary>
		/// <param name="type">Type.</param>
		public abstract object Resolve (Type type);

		/// <summary>
		/// Sets the resolver.
		/// </summary>
		/// <returns>The resolver.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <typeparam name="TConcreteType">The 2nd type parameter.</typeparam>
		public abstract IfInjectorTypes.IfFastInjectorBinding<TConcreteType> Bind<T, TConcreteType> ()
			where T : class
			where TConcreteType : class, T;

		/// <summary>
		/// Bind this instance.
		/// </summary>
		/// <typeparam name="TConcreteType">The 1st type parameter.</typeparam>
		public abstract IfInjectorTypes.IfFastInjectorBinding<TConcreteType> Bind<TConcreteType> ()
			where TConcreteType : class;

		/// <summary>
		/// Sets the resolver.
		/// </summary>
		/// <returns>The resolver.</returns>
		/// <param name="factoryExpression">Factory expression.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public abstract IfInjectorTypes.IfFastInjectorBinding<T> Bind<T> (Expression<Func<T>> factoryExpression)
			where T : class;
	}

	/// <summary>
	/// Holder of secondary IfInjector types and interfaces. Most API users will not need to access these types directly.
	/// </summary>
	namespace IfInjectorTypes {
		/// <summary>
		/// If fast injector errors.
		/// </summary>
		public static class IfFastInjectorErrors
		{
			public const string ErrorResolutionRecursionDetected = "Resolution recursion detected.  Resolve<{0}> is called by a dependency of Resolve<{0}> leading to an infinite loop.";
			public const string ErrorUnableToResultInterface = "Error on {0}. Unable to resolve Interface and Abstract classes without a configuration.";
			public const string ErrorMustContainMemberExpression = "Must contain a MemberExpression";
			public const string ErrorAmbiguousBinding = "Multiple implicit bindings exist for type: {0}. Please disambiguate by adding an explicit binding for this type.";
		}

		/// <summary>
		/// F fast injector exception.
		/// </summary>
		public class IfFastInjectorException : Exception
		{
			public IfFastInjectorException () : base() {}
			public IfFastInjectorException (string message) : base(message) {}
			public IfFastInjectorException (string message, Exception innerException) : base(message, innerException) {}
		}
				
		/// <summary>
		/// The fluent class is really only important to give the extension methods the type for T. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public interface IfFastInjectorBinding<T> where T : class
		{ 
			IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression) 
				where TPropertyType : class;

			IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
				where TPropertyType : class;

			void AsSingleton ();
		}
	}
}
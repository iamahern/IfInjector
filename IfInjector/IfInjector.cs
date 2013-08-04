using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IfInjector
{
	/// <summary>
	/// Inject attribute. Used to flag constructors for preferred injection. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field)]
	public class InjectAttribute : Attribute {}

	/// <summary>
	/// Ignore constructor attribute. Used to flage constructors to be ignored.
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor)]
	public class IgnoreConstructorAttribute : Attribute {}

	/// <summary>
	/// Implemented by attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
	public class ImplementedByAttribute : Attribute {
		private readonly Type implementor;
		public ImplementedByAttribute(Type implementor) {
			this.implementor = implementor;
		}

		public Type Implementor { get { return implementor; } }
	}

	/// <summary>
	/// Singleton attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	public class SingletonAttribute : Attribute {}

	/// <summary>
	/// Injector.
	/// </summary>
	public abstract class Injector
	{
		/// <summary>
		/// News the instance.
		/// </summary>
		/// <returns>The instance.</returns>
		public static Injector NewInstance ()
		{
			return new InjectorInternal.InjectorImpl ();
		}

		/// <summary>
		/// Resolve this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T Resolve<T> () where T : class {
			return (T) Resolve(typeof(T));
		}

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
		public abstract IfInjectorTypes.IInjectorBinding<TConcreteType> Bind<T, TConcreteType> ()
			where T : class
			where TConcreteType : class, T;

		/// <summary>
		/// Sets the resolver.
		/// </summary>
		/// <typeparam name="TConcreteType">The 1st type parameter.</typeparam>
		public IfInjectorTypes.IInjectorBinding<TConcreteType> Bind<TConcreteType> ()
			where TConcreteType : class
		{
			return Bind<TConcreteType, TConcreteType> ();
		}

		/// <summary>
		/// Bind the specified factoryExpression.
		/// </summary>
		/// <param name="factoryExpression">Factory expression.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <typeparam name="CT">The 2nd type parameter. This parameter is required to allow for auto-injection of factory provided object.</typeparam>
		public IfInjectorTypes.IInjectorBinding<CT> Bind<T,CT> (Expression<Func<CT>> factoryExpression)
			where T : class
			where CT : class, T
		{
			return Bind<T,CT> (factoryExpression as LambdaExpression);
		}
		
		/// <summary>
		/// Bind the specified factoryExpression.
		/// </summary>
		/// <param name="factoryExpression">Factory expression.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <typeparam name="CT">The 2nd type parameter.</typeparam>
		protected abstract IfInjectorTypes.IInjectorBinding<CT> Bind<T,CT> (LambdaExpression factoryExpression)
			where T : class
			where CT : class, T;

		/// <summary>
		/// Injects the properties of an instance.
		/// </summary>
		/// <returns>The properties.</returns>
		/// <param name="instance">Instance.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public abstract T InjectProperties<T> (T instance)
			where T : class;

		/// <summary>
		/// Binds the lambda factory. Do not use thid directly, but instead create extension methods that take N-input Func<> methods.
		/// </summary>
		/// <returns>The lambda factory.</returns>
		/// <param name="injector">Injector.</param>
		/// <param name="factoryExpression">Factory expression.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <typeparam name="CT">The 2nd type parameter.</typeparam>
		public static IfInjectorTypes.IInjectorBinding<CT> BindFactory<T,CT> (Injector injector, LambdaExpression factoryExpression) 
			where T : class
			where CT : class, T
		{
			return injector.Bind<T,CT> (factoryExpression);
		}
	}

	namespace IfInjectorExtensions {
		/// <summary>
		/// Provide extension methods up to Func<P1..P4,CT>
		/// </summary>
		public static class InjectorBindingExtensions {
			public static IfInjectorTypes.IInjectorBinding<CT> Bind<T,P1,CT>(this Injector injector, Expression<Func<P1,CT>> factoryExpression) 
				where T : class
				where CT : class, T
				where P1 : class
			{
				return Injector.BindFactory<T,CT> (injector, factoryExpression as LambdaExpression);
			}

			public static IfInjectorTypes.IInjectorBinding<CT> Bind<T,P1,P2,CT>(this Injector injector, Expression<Func<P1,P2,CT>> factoryExpression) 
				where T : class
				where CT : class, T
				where P1 : class
				where P2 : class
			{
				return Injector.BindFactory<T,CT> (injector, factoryExpression as LambdaExpression);
			}

			public static IfInjectorTypes.IInjectorBinding<CT> Bind<T,P1,P2,P3,CT>(this Injector injector, Expression<Func<P1,P2,P3,CT>> factoryExpression) 
				where T : class
				where CT : class, T
				where P1 : class
				where P2 : class
				where P3 : class
			{
				return Injector.BindFactory<T,CT> (injector, factoryExpression as LambdaExpression);
			}

			public static IfInjectorTypes.IInjectorBinding<CT> Bind<T,P1,P2,P3,P4,CT>(this Injector injector, Expression<Func<P1,P2,P3,P4,CT>> factoryExpression) 
				where T : class
				where CT : class, T
				where P1 : class
				where P2 : class
				where P3 : class
				where P4 : class
			{
				return Injector.BindFactory<T,CT> (injector, factoryExpression as LambdaExpression);
			}
		}
	}

	/// <summary>
	/// Holder of secondary IfInjector types and interfaces. Most API users will not need to access these types directly.
	/// </summary>
	namespace IfInjectorTypes {
		/// <summary>
		/// Represents an error code constant.
		/// </summary>
		public class InjectorError {
			internal InjectorError(int messageCode, string messageTemplate) {
				MessageCode = string.Format ("IF{0:D4}", messageCode);
				MessageTemplate = messageTemplate;
			}

			public string MessageCode { get; private set; }
			public string MessageTemplate { get; private set; }

			public InjectorException FormatEx(params object[] args) {
				var msgFormatted = string.Format (MessageTemplate, args);
				return new InjectorException (this, msgFormatted);
			}

			public InjectorException FormatEx(Exception innerException, params object[] args) {
				var msgFormatted = string.Format (MessageTemplate, args);
				return new InjectorException (this, msgFormatted, innerException);
			}
		}

		/// <summary>
		/// If fast injector errors.
		/// </summary>
		public static class InjectorErrors
		{
			public static readonly InjectorError ErrorResolutionRecursionDetected = new InjectorError(1, "Resolution recursion detected.  Resolve<{0}> is called by a dependency of Resolve<{0}> leading to an infinite loop.");
			public static readonly InjectorError ErrorUnableToResultInterface = new InjectorError(2, "Error on {0}. Unable to resolve Interface and Abstract classes without a configuration.");
			public static readonly InjectorError ErrorMustContainMemberExpression = new InjectorError(3, "Must contain a MemberExpression");
			public static readonly InjectorError ErrorAmbiguousBinding =  new InjectorError(4, "Multiple implicit bindings exist for type: {0}. Please disambiguate by adding an explicit binding for this type.");
			public static readonly InjectorError ErrorUnableToBindNonClassFieldsProperties = new InjectorError(5, "Autoinjection is only supported on single instance 'class' fields. Please define a manual binding for the field or property '{0}' on class '{1}'.");
		}

		/// <summary>
		/// If fast injector exception.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
		public class InjectorException : Exception
		{
			public InjectorException (InjectorError errorType, string message) : base(message) {
				ErrorType = errorType;
			}

			public InjectorException (InjectorError errorType, string message, Exception innerException) : base(message, innerException) {
				ErrorType = errorType;
			}

			public InjectorError ErrorType { get; private set; }
		}
				
		/// <summary>
		/// The fluent class is really only important to give the extension methods the type for T. 
		/// </summary>
		/// <typeparam name="T"></typeparam>
		public interface IInjectorBinding<T> where T : class
		{
			IInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression) 
				where TPropertyType : class;

			IInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);

			IInjectorBinding<T> AsSingleton (bool singlton = true);
		}
	}
}
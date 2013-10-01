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
	[AttributeUsage(AttributeTargets.Class, Inherited=true)]
	public class SingletonAttribute : Attribute {}

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
		/// <param name="useExplicitBinding">If set to <c>true</c> use explicit binding.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		T InjectProperties<T> (T instance, bool useExplicitBinding = false)
			where T : class;
		
		/// <summary>
		/// Sets the resolver.
		/// </summary>
		/// <returns>The resolver.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		/// <typeparam name="TConcreteType">The 2nd type parameter.</typeparam>
		IfCore.IfBinding.IInjectorBinding<CType> Bind<BType, CType> ()
			where BType : class
			where CType : class, BType;

		/// <summary>
		/// Sets the resolver.
		/// </summary>
		/// <typeparam name="TConcreteType">The 1st type parameter.</typeparam>
		IfCore.IfBinding.IInjectorBinding<CType> Bind<CType> ()
			where CType : class;

		/// <summary>
		/// Binds the map.
		/// </summary>
		/// <returns>The map.</returns>
		/// <typeparam name="BType">The binding type.</typeparam>
		/// <typeparam name="KeyType">The 2nd type parameter.</typeparam>
		IfCore.IfBinding.IDictionaryBinding<KeyType, BType> BindDictionary<KeyType, BType> ()
			where BType : class;

		/// <summary>
		/// Verify that all bindings all valid.
		/// </summary>
		void Verify ();
	}

	/// <summary>
	/// Injector implementation.
	/// </summary>
 	public partial class Injector : IInjector
	{
		/// <summary>
		/// News the instance.
		/// </summary>
		/// <returns>The instance.</returns>
		[Obsolete("use new Injector()", false)]
		public static Injector NewInstance ()
		{
			return new Injector ();
		}
	}

	/// <summary>
	/// Provide extension methods for Func<P1..P4,CT>
	/// </summary>
	public static class InjectorBindingExtensions {
		public static IfCore.IfBinding.IInjectorBinding<CT> SetFactory<CT>(this IfCore.IfBinding.IInjectorBinding<CT> binding, Expression<Func<CT>> factoryExpression) 
			where CT : class
		{
			return binding.SetFactoryLambda (factoryExpression);
		}

		public static IfCore.IfBinding.IInjectorBinding<CT> SetFactory<P1,CT>(this IfCore.IfBinding.IInjectorBinding<CT> binding, Expression<Func<P1,CT>> factoryExpression) 
			where CT : class
			where P1 : class
		{
			return binding.SetFactoryLambda (factoryExpression);
		}

		public static IfCore.IfBinding.IInjectorBinding<CT> SetFactory<P1,P2,CT>(this IfCore.IfBinding.IInjectorBinding<CT> binding, Expression<Func<P1,P2,CT>> factoryExpression) 
			where CT : class
			where P1 : class
			where P2 : class
		{
			return binding.SetFactoryLambda (factoryExpression);
		}

		public static IfCore.IfBinding.IInjectorBinding<CT> SetFactory<P1,P2,P3,CT>(this IfCore.IfBinding.IInjectorBinding<CT> binding, Expression<Func<P1,P2,P3,CT>> factoryExpression) 
			where CT : class
			where P1 : class
			where P2 : class
			where P3 : class
		{
			return binding.SetFactoryLambda (factoryExpression);
		}

		public static IfCore.IfBinding.IInjectorBinding<CT> SetFactory<P1,P2,P3,P4,CT>(this IfCore.IfBinding.IInjectorBinding<CT> binding, Expression<Func<P1,P2,P3,P4,CT>> factoryExpression) 
			where CT : class
			where P1 : class
			where P2 : class
			where P3 : class
			where P4 : class
		{
			return binding.SetFactoryLambda (factoryExpression);
		}
	}

	/// <summary>
	/// Holder of secondary IfInjector types and interfaces. This namespace contains error types and binding interfaces.
	/// 
	/// Most API users will not need to access these types directly.
	/// </summary>
	namespace IfCore {

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
			public static readonly InjectorError ErrorNoAppropriateConstructor = new InjectorError (6, "No appropriate constructor for type: {0}.");
			public static readonly InjectorError ErrorMayNotBindInjector = new InjectorError (7, "Binding 'Injector' types is not permitted.");
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

		namespace IfBinding {
			using IfPlatform;

			/// <summary>
			/// The binding key object is used to
			/// </summary>
			internal class BindingKey {
				private static readonly string PAREN_OPEN = "(";
				private static readonly string PAREN_CLOSE = ")";
				private static readonly string DELIM = "|";
				private static readonly string TYPE = "Type=";
				private static readonly string IMPLICIT = "Implicit=";
				private static readonly string PARENT = "Parent=";

				private static object syncLock = new object();
				private static SafeDictionary<string, BindingKey> bindingKeys = new SafeDictionary<string, BindingKey>(syncLock);

				private string KeyString { get; set; }
				public BindingKey Container { get; private set; }
				public Type BindingType { get; private set; }
				public bool Implicit { get; private set; }

				public static BindingKey Get<T>()
					where T : class 
				{
					return Get<T> (false);
				}

				internal static BindingKey Get<T>(bool isImplicit) where T : class {
					return isImplicit ? 
						BindingKeyInternal<T>.IMPLICIT : 
						BindingKeyInternal<T>.EXPLICIT;
				}

				public static BindingKey Get(Type keyType) {
					return Get (keyType, false);
				}

				/// <summary>
				/// Used to get a binding key for a collection
				/// </summary>
				internal static BindingKey Get(BindingKey container, Type keyType) {
					return Get (container, keyType, false);
				}

				internal static BindingKey Get(Type keyType, bool isImplicit) {
					return Get (null, keyType, isImplicit);
				}

				private static BindingKey Get(BindingKey container, Type keyType, bool isImplicit) {
					string keyString = 
						PARENT + PAREN_OPEN + (container == null ? "" : container.KeyString) + PAREN_CLOSE 
						+ DELIM + TYPE + keyType.FullName + DELIM + IMPLICIT + isImplicit;

					BindingKey key;
					if (!bindingKeys.UnsyncedTryGetValue (keyString, out key)) {
						lock (syncLock) {
							if (!bindingKeys.TryGetValue (keyString, out key)) {
								key = new BindingKey () { 
									KeyString = keyString,
									Container = container,
									BindingType = keyType,
									Implicit = isImplicit
								};
								bindingKeys.Add (keyString, key);
							}
						}
					}
					return key;
				}

				private static class BindingKeyInternal<T> where T : class {
					public static readonly BindingKey EXPLICIT = BindingKey.Get (typeof(T), false);
					public static readonly BindingKey IMPLICIT = BindingKey.Get (typeof(T), true);
				}
			}

			/// <summary>
			/// Map binding. Callers specify a key and value (BType).
			/// </summary>
			public interface IDictionaryBinding<KeyType, BType> where BType : class
			{
				/// <summary>
				/// Adds the binding.
				/// </summary>
				/// <returns>The binding.</returns>
				/// <param name="keyValue">Key value.</param>
				/// <typeparam name="CType">The 1st type parameter.</typeparam>
				IInjectorBinding<CType> AddBinding<CType> (KeyType keyValue)
					where CType : class, BType;
			}

			/// <summary>
			/// The fluent class is really only important to give the extension methods the type for T. 
			/// </summary>
			/// <typeparam name="T"></typeparam>
			public interface IInjectorBinding<CType> where CType : class
			{
				IInjectorBinding<CType> SetFactoryLambda (LambdaExpression factoryExpression);

				IInjectorBinding<CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
					where TPropertyType : class;

				IInjectorBinding<CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);

				IInjectorBinding<CType> AsSingleton (bool singleton = true);
			}
		}
	}
}
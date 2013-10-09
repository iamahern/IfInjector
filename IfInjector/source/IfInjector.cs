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
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		T InjectProperties<T> (T instance)
			where T : class;
		
		/// <summary>
		/// Bind the specified binding.
		/// </summary>
		/// <param name="binding">Binding.</param>
		void Bind (IfCore.IfBinding.IBinding binding);

		/// <summary>
		/// Binds the instance injector.
		/// </summary>
		/// <returns>The instance injector.</returns>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		IfCore.IfBinding.IInstanceInjectorBinding<CType> BindInstanceInjector<CType> () 
			where CType : class;
		
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
		public static IfCore.IfBinding.IBinding AsSingleton<BT,CT>(this IfCore.IfBinding.IBoundBinding<BT,CT> binding, bool singleton = true) 
			where BT : class
			where CT : class, BT
		{
			if (singleton) {
				return binding.SetLifestyle (IfCore.IfLifestyle.Lifestyle.Singleton);
			} else {
				return binding.SetLifestyle (IfCore.IfLifestyle.Lifestyle.Transient);
			}
		}

		public static IfCore.IfBinding.IBoundBinding<BT,CT> SetFactory<BT, CT>(this IfCore.IfBinding.IOngoingBinding<BT> binding, Expression<Func<CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
		{
			return binding.SetFactoryLambda<CT> (factoryExpression);
		}
		
		public static IfCore.IfBinding.IBoundBinding<BT,CT> SetFactory<P1,BT,CT>(this IfCore.IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
		{
			return binding.SetFactoryLambda<CT> (factoryExpression);
		}

		public static IfCore.IfBinding.IBoundBinding<BT,CT> SetFactory<P1,P2,BT,CT>(this IfCore.IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,P2,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
		{
			return binding.SetFactoryLambda<CT> (factoryExpression);
		}

		public static IfCore.IfBinding.IBoundBinding<BT,CT> SetFactory<P1,P2,P3,BT,CT>(this IfCore.IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,P2,P3,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
			where P3 : class
		{
			return binding.SetFactoryLambda<CT> (factoryExpression);
		}

		public static IfCore.IfBinding.IBoundBinding<BT,CT> SetFactory<P1,P2,P3,P4,BT,CT>(this IfCore.IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,P2,P3,P4,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
			where P3 : class
			where P4 : class
		{
			return binding.SetFactoryLambda<CT> (factoryExpression);
		}
	}

	/// <summary>
	/// Binding factory class.
	/// </summary>
	public static class Binding {

		/// <summary>
		/// Create a binding.
		/// </summary>
		/// <typeparam name="BType">The 1st type parameter.</typeparam>
		public static IfCore.IfBinding.IOngoingBinding<BType> For<BType>() where BType : class {
			return new IfCore.IfBinding.OngoingBinding<BType>(); 
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
			internal sealed class BindingKey : IEquatable<BindingKey> {
				private static readonly string DELIM = "|";
				private static readonly string TYPE = "Type=";
				private static readonly string MEMBER = "Member=";

				private static object syncLock = new object();
				private static SafeDictionary<string, BindingKey> bindingKeys = new SafeDictionary<string, BindingKey>(syncLock);

				private string KeyString { get; set; }
				public Type BindingType { get; private set; }
				public bool Member { get; private set; }

				public override int GetHashCode() {
					return KeyString.GetHashCode();
				}

				public override bool Equals(object obj) {
					return Equals(obj as BindingKey);
				}

				public bool Equals(BindingKey obj) {
					return obj != null && obj.KeyString == KeyString;
				}

				public static BindingKey Get<T>() where T : class {
					return BindingKeyInternal<T>.INSTANCE;
				}

				internal static BindingKey GetMemberInjector<T>() where T : class {
					return BindingKeyInternal<T>.MEMBER;
				}

				public static BindingKey Get(Type keyType) {
					return GetInternal (keyType, false);
				}

				internal static BindingKey GetInternal(Type keyType, bool isMember) {
					string keyString = 
						TYPE + keyType.FullName + DELIM + MEMBER + isMember;

					BindingKey key;
					if (!bindingKeys.UnsyncedTryGetValue (keyString, out key)) {
						lock (syncLock) {
							if (!bindingKeys.TryGetValue (keyString, out key)) {
								key = new BindingKey () { 
									KeyString = keyString,
									BindingType = keyType,
									Member = isMember
								};
								bindingKeys.Add (keyString, key);
							}
						}
					}
					return key;
				}

				private static class BindingKeyInternal<T> where T : class {
					public static readonly BindingKey INSTANCE = BindingKey.GetInternal (typeof(T), false);
					public static readonly BindingKey MEMBER = BindingKey.GetInternal (typeof(T), true);
				}
			}

			/// <summary>
			/// Base binding type. This represents a closed binding object.
			/// </summary>
			public interface IBinding {}

			/// <summary>
			/// Closed binding with options
			/// </summary>
			public interface ILifestyleScopeableBinding : IBinding
			{
				IBinding SetLifestyle (IfLifestyle.Lifestyle lifestyle);
			}

			public interface IBoundBinding<BType, CType> : ILifestyleScopeableBinding
				where BType : class
				where CType : class, BType
			{
				IBoundBinding<BType, CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
					where TPropertyType : class;

				IBoundBinding<BType, CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);
			}

			public interface IOngoingBinding<BType> : IBoundBinding<BType, BType>
				where BType : class
			{
				IBoundBinding<BType, CType> To<CType> () 
					where CType : class, BType;

				IBoundBinding<BType, CType> SetFactoryLambda<CType>(LambdaExpression factoryExpression)
					where CType : class, BType;
			}

			/// <summary>
			/// Instance injector binding.
			/// </summary>
			/// <typeparam name="CType"></typeparam>
			public interface IInstanceInjectorBinding<CType> 
				where CType : class
			{
				IInstanceInjectorBinding<CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
					where TPropertyType : class;

				IInstanceInjectorBinding<CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);
			}
		}
	}
}
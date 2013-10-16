﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using IfInjector.IfBinding;
using IfInjector.IfBinding.IfInternal;

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
	/// Injector implementation.
	/// </summary>
 	public partial class Injector : IfCore.IInjector
	{
	}

	/// <summary>
	/// Provide extension methods for Func<P1..P4,CT>
	/// </summary>
	public static class InjectorBindingExtensions {

		/// <summary>
		/// Set the binding singleton.
		/// </summary>
		/// <returns>The singleton.</returns>
		/// <param name="binding">Binding.</param>
		/// <param name="singleton">If set to <c>true</c> singleton.</param>
		/// <typeparam name="BT">The 1st type parameter.</typeparam>
		/// <typeparam name="CT">The 2nd type parameter.</typeparam>
		public static IfBinding.IBinding<BT,CT> AsSingleton<BT,CT>(this IfBinding.IBinding<BT,CT> binding, bool singleton = true) 
			where BT : class
			where CT : class, BT
		{
			if (singleton) {
				return binding.SetLifestyle (IfLifestyle.Lifestyle.Singleton);
			} else {
				return binding.SetLifestyle (IfLifestyle.Lifestyle.Transient);
			}
		}

		/// <summary>
		/// Helper method to allow for setting of factories expressions.
		/// </summary>
		/// <returns>The factory helper.</returns>
		/// <param name="binding">Binding.</param>
		/// <param name="factoryExpression">Factory expression.</param>
		public static IfBinding.IBinding<BT,CT> SetFactoryHelper<BT, CT>(IfBinding.IOngoingBinding<BT> binding, LambdaExpression factoryExpression) 
			where BT : class
			where CT : class, BT
		{
			var internalBinding = (IfInjector.IfBinding.IfInternal.IOngoingBindingInternal<BT>)binding;
			return internalBinding.SetFactoryLambda<CT> (factoryExpression);
		}
		
		public static IfBinding.IBinding<BT,CT> SetFactory<BT, CT>(this IfBinding.IOngoingBinding<BT> binding, Expression<Func<CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}
		
		public static IfBinding.IBinding<BT,CT> SetFactory<P1,BT,CT>(this IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IfBinding.IBinding<BT,CT> SetFactory<P1,P2,BT,CT>(this IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,P2,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IfBinding.IBinding<BT,CT> SetFactory<P1,P2,P3,BT,CT>(this IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,P2,P3,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
			where P3 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IfBinding.IBinding<BT,CT> SetFactory<P1,P2,P3,P4,BT,CT>(this IfBinding.IOngoingBinding<BT> binding, Expression<Func<P1,P2,P3,P4,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
			where P3 : class
			where P4 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
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
		public static IfBinding.IOngoingBinding<BType> For<BType>() where BType : class {
			return new OngoingBindingInternal<BType>(); 
		}
	}

	/// <summary>
	/// Member binding factory class.
	/// </summary>
	public static class PropertiesBinding {

		/// <summary>
		/// Create a binding.
		/// </summary>
		/// <typeparam name="BType">The 1st type parameter.</typeparam>
		public static IfBinding.IPropertiesBinding<BType> For<BType>() where BType : class {
			return new PropertiesBindingInternal<BType>(); 
		}
	}

	/// <summary>
	/// Holder of secondary IfInjector types and interfaces. This namespace contains error types and binding interfaces.
	/// 
	/// Most API users will not need to access these types directly.
	/// </summary>
	namespace IfCore {
		using IfInjector.IfBinding;

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
			/// Verify that all bindings all valid.
			/// </summary>
			void Verify ();
		}

		/// <summary>
		/// Represents an error code constant.
		/// </summary>
		public class InjectorError {
			internal InjectorError(int messageCode, string messageTemplate) {
				MessageCode = string.Format ("IF{0:D4}", messageCode);
				MessageTemplate = messageTemplate;
			}

			/// <summary>
			/// Gets the message code.
			/// </summary>
			/// <value>The message code.</value>
			public string MessageCode { get; private set; }

			/// <summary>
			/// Gets the message template.
			/// </summary>
			/// <value>The message template.</value>
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
			public static readonly InjectorError ErrorBindingRegistrationNotPermitted = new InjectorError (8, "Injector is in resolved state. Explicit binding registration is no longer permitted.");
		}

		/// <summary>
		/// If fast injector exception.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2237:MarkISerializableTypesWithSerializable")]
		public class InjectorException : Exception
		{
			internal InjectorException (InjectorError errorType, string message) : base(message) {
				ErrorType = errorType;
			}

			internal InjectorException (InjectorError errorType, string message, Exception innerException) : base(message, innerException) {
				ErrorType = errorType;
			}

			/// <summary>
			/// Gets the type of the error.
			/// </summary>
			/// <value>The type of the error.</value>
			public InjectorError ErrorType { get; private set; }
		}
	}
}
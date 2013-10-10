using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;

using IfInjector.IfCore;
using IfInjector.IfCore.IfPlatform;
using IfInjector.IfLifestyle;
using System.Linq;

namespace IfInjector.IfBinding
{
	/// <summary>
	/// The binding key object is an immutable type used to index bindings.
	/// </summary>
	internal sealed class BindingKey : IEquatable<BindingKey> {
		private static readonly string DELIM = "|";
		private static readonly string TYPE = "Type=";
		private static readonly string PROPERTY = "Property=";

		private static object syncLock = new object();
		private static SafeDictionary<string, BindingKey> bindingKeys = new SafeDictionary<string, BindingKey>(syncLock);

		private string KeyString { get; set; }

		/// <summary>
		/// Gets the type of the binding.
		/// </summary>
		/// <value>The type of the binding.</value>
		public Type BindingType { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="IfInjector.IfBinding.BindingKey"/> is a property-only binding.
		/// </summary>
		/// <value><c>true</c> if member; otherwise, <c>false</c>.</value>
		public bool Member { get; private set; }

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode() {
			return KeyString.GetHashCode();
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="IfInjector.IfBinding.BindingKey"/>.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="IfInjector.IfBinding.BindingKey"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to the current
		/// <see cref="IfInjector.IfBinding.BindingKey"/>; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj) {
			return Equals(obj as BindingKey);
		}

		/// <summary>
		/// Determines whether the specified <see cref="IfInjector.IfBinding.BindingKey"/> is equal to the current <see cref="IfInjector.IfBinding.BindingKey"/>.
		/// </summary>
		/// <param name="obj">The <see cref="IfInjector.IfBinding.BindingKey"/> to compare with the current <see cref="IfInjector.IfBinding.BindingKey"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="IfInjector.IfBinding.BindingKey"/> is equal to the current
		/// <see cref="IfInjector.IfBinding.BindingKey"/>; otherwise, <c>false</c>.</returns>
		public bool Equals(BindingKey obj) {
			return obj != null && obj.KeyString == KeyString;
		}

		/// <summary>
		/// Get this instance of the binding key.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static BindingKey Get<T>() where T : class {
			return BindingKeyInternal<T>.INSTANCE;
		}

		/// <summary>
		/// Get the instance injector.
		/// </summary>
		/// <param name="keyType">Key type.</param>
		public static BindingKey Get(Type keyType) {
			return GetInternal (keyType, false);
		}

		/// <summary>
		/// Gets the properties injector.
		/// </summary>
		/// <returns>The properties injector.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		internal static BindingKey GetPropertiesInjector<T>() where T : class {
			return BindingKeyInternal<T>.PROPERTIES;
		}

		private static BindingKey GetInternal(Type keyType, bool isMember) {
			string keyString = 
				TYPE + keyType.FullName + DELIM + PROPERTY + isMember;

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
			public static readonly BindingKey PROPERTIES = BindingKey.GetInternal (typeof(T), true);
		}
	}

	/// <summary>
	/// Base binding type. This represents a closed binding object.
	/// </summary>
	public interface IBinding {}

	/// <summary>
	/// Bbound binding.
	/// </summary>
	public interface IBinding<BType, CType> : IBinding
		where BType : class
		where CType : class, BType
		{
			/// <summary>
			/// Sets the lifestyle.
			/// </summary>
			/// <returns>The lifestyle.</returns>
			/// <param name="lifestyle">Lifestyle.</param>
			IBinding<BType, CType> SetLifestyle (Lifestyle lifestyle);

			/// <summary>
			/// Indicate that the referenced property should be injected.
			/// </summary>
			/// <returns>The property.</returns>
			/// <param name="propertyExpression">Property expression.</param>
			/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
			IBinding<BType, CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
				where TPropertyType : class;

			/// <summary>
			/// Indicate that the referenced property should be injected using the specified setter expression.
			/// </summary>
			/// <returns>The property.</returns>
			/// <param name="propertyExpression">Property expression.</param>
			/// <param name="setter">Setter.</param>
			/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
			IBinding<BType, CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);
		}

	/// <summary>
	/// Ongoing binding.
	/// </summary>
	public interface IOngoingBinding<BType> : IBinding<BType, BType>
		where BType : class
	{
		/// <summary>
		/// Associate this binding with the specified implementation type.
		/// </summary>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		IBinding<BType, CType> To<CType> () 
			where CType : class, BType;
	}

	/// <summary>
	/// Base binding type. This represents a closed member binding object.
	/// </summary>
	public interface IPropertiesBinding {}

	/// <summary>
	/// Members binding.
	/// </summary>
	public interface IPropertiesBinding<CType> : IPropertiesBinding
		where CType : class
	{
		/// <summary>
		/// Indicate that the referenced property should be injected.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="propertyExpression">Property expression.</param>
		/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
		IPropertiesBinding<CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
			where TPropertyType : class;

		/// <summary>
		/// Indicate that the referenced property should be injected using the specified setter expression.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="propertyExpression">Property expression.</param>
		/// <param name="setter">Setter.</param>
		/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
		IPropertiesBinding<CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);
	}

	/// <summary>
	/// Internal implementation classes
	/// </summary>
	namespace IfInternal {
		/// <summary>
		/// Internal interface for working with binding objects.
		/// </summary>
		internal interface IInternalBinding {
			/// <summary>
			/// Gets the binding config.
			/// </summary>
			/// <value>The binding config.</value>
			BindingConfig BindingConfig { get; }

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

		/// <summary>
		/// Internal binding implementation.
		/// </summary>
		internal class BindingInternal<BType, CType> : IBinding<BType, CType>, IInternalBinding
			where BType : class
			where CType : class, BType
		{
			public BindingConfig BindingConfig { get; private set; }
			public BindingKey BindingKey { get; private set; }
			public Type ConcreteType { get { return typeof(CType); } }

			internal BindingInternal(BindingConfig bindingConfig) {
				BindingConfig = bindingConfig;
				BindingKey = BindingKey.Get<BType> ();
				Injector.ImplicitTypeUtilities.SetupImplicitPropResolvers<CType> (bindingConfig, this);
			}

			public IBinding<BType, CType> SetLifestyle (Lifestyle lifestyle) {
				BindingConfig.Lifestyle = lifestyle;
				return this;
			}

			public IBinding<BType, CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
				where TPropertyType : class
			{
				return AddPropertyInjectorInner<TPropertyType> (propertyExpression, null);
			}

			public IBinding<BType, CType> InjectProperty<TPropertyType> (
				Expression<Func<CType, TPropertyType>> propertyExpression, 
				Expression<Func<TPropertyType>> setter)
			{
				return AddPropertyInjectorInner<TPropertyType> (propertyExpression, setter);
			}

			private IBinding<BType, CType> AddPropertyInjectorInner<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter) {
				BindingUtil.AddPropertyInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, propertyExpression, setter);
				return this;
			}
		}

		/// <summary>
		/// Internal interface for ongoing bindings.
		/// </summary>
		internal interface IOngoingBindingInternal<BType> 
			where BType : class
		{
			/// <summary>
			/// Sets the factory lambda.
			/// </summary>
			/// <returns>The factory lambda.</returns>
			/// <param name="factoryExpression">Factory expression.</param>
			/// <typeparam name="CType">The 1st type parameter.</typeparam>
			IBinding<BType, CType> SetFactoryLambda<CType>(LambdaExpression factoryExpression)
				where CType : class, BType;
		}

		/// <summary>
		/// Ongoing binding implementation.
		/// </summary>
		internal class OngoingBindingInternal<BType> : BindingInternal<BType, BType>, IOngoingBinding<BType>, IOngoingBindingInternal<BType>
			where BType : class
		{
			internal OngoingBindingInternal() : base(new BindingConfig<BType>()) {}

			public IBinding<BType, CType> To<CType> () 
				where CType : class, BType
			{
				return new BindingInternal<BType, CType> (new BindingConfig<CType>());
			}

			public IBinding<BType, CType> SetFactoryLambda<CType>(LambdaExpression factoryExpression)
				where CType : class, BType
			{
				var bindingConfig = new BindingConfig<CType> ();
				var boundBinding = new BindingInternal<BType, CType> (bindingConfig); 
				bindingConfig.FactoryExpression = factoryExpression;
				return boundBinding;
			}
		}

		/// <summary>
		/// Properties binding implementation.
		/// </summary>
		internal class PropertiesBindingInternal<CType> : IPropertiesBinding<CType>, IInternalBinding
			where CType : class
		{
			public BindingConfig BindingConfig { get; private set; }
			public BindingKey BindingKey { get; private set; }
			public Type ConcreteType { get { return typeof(CType); } }

			internal PropertiesBindingInternal() {
				BindingConfig = new BindingConfig<CType>();
				BindingKey = BindingKey.GetPropertiesInjector<CType> ();
				Injector.ImplicitTypeUtilities.SetupImplicitPropResolvers<CType> (BindingConfig, this);
			}

			public IPropertiesBinding<CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
				where TPropertyType : class
			{
				return InjectProperty<TPropertyType> (propertyExpression, null);
			}

			public IPropertiesBinding<CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				BindingUtil.AddPropertyInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, propertyExpression, setter);
				return this;
			}
		}

		/// <summary>
		/// Binding config setter expression.
		/// </summary>
		internal interface IMemberSetterConfig<MTInfo> where MTInfo : MemberInfo
		{
			/// <summary>
			/// Gets the setter.
			/// </summary>
			/// <value>The setter.</value>
			LambdaExpression MemberSetter { get; }

			/// <summary>
			/// Gets the info.
			/// </summary>
			/// <value>The info.</value>
			MTInfo MemberInfo { get; }

			/// <summary>
			/// Gets the type of the member.
			/// </summary>
			/// <value>The type of the member.</value>
			Type MemberType { get; }
		}

		/// <summary>
		/// Binding config. At present time this is not part of the public API. In the future it may be open to allow.
		/// 
		/// Caller's must synchronize access via the syncLock.
		/// </summary>
		internal abstract class BindingConfig {
			///////////////
			// Setter Config
			///////////////
			private class MemberSetterConfig<MTInfo> : IMemberSetterConfig<MTInfo> where MTInfo : MemberInfo
			{
				public Type ConcreteType { get; set; }
				public LambdaExpression MemberSetter { get; set; }
				public MTInfo MemberInfo { get; set; }
				public Type MemberType { get; set; }

				public MemberSetterConfig<MTInfo> Validate() {
					if (MemberSetter == null && !MemberType.IsClass && !MemberType.IsInterface) {
						throw InjectorErrors.ErrorUnableToBindNonClassFieldsProperties.FormatEx(MemberInfo.Name, ConcreteType.FullName);
					}
					return this;
				}
			}

			private readonly Dictionary<PropertyInfo, IMemberSetterConfig<PropertyInfo>> propertyInjectors 
				= new Dictionary<PropertyInfo, IMemberSetterConfig<PropertyInfo>>();
			private readonly Dictionary<FieldInfo, IMemberSetterConfig<FieldInfo>> fieldInjectors 
				= new Dictionary<FieldInfo, IMemberSetterConfig<FieldInfo>>();

			/// <summary>
			/// Gets or sets the concrete type.
			/// </summary>
			/// <value>The type of the C.</value>
			protected abstract Type ConcreteType { get; }

			/// <summary>
			/// Gets or sets the lifestyle.
			/// </summary>
			/// <value>The lifestyle.</value>
			internal Lifestyle Lifestyle { get; set; }

			/// <summary>
			/// Gets or sets the constructor.
			/// </summary>
			/// <value>The constructor.</value>
			internal abstract ConstructorInfo Constructor { get; set; }

			/// <summary>
			/// Gets or sets the factory expression.
			/// </summary>
			/// <value>The factory expression.</value>
			internal abstract LambdaExpression FactoryExpression { get; set; }

			/// <summary>
			/// Adds the property info setter.
			/// </summary>
			/// <param name="propertyInfo">Property info.</param>
			/// <param name="setter">This may be null to allow for an implicit binding or a 0-arg LambdaExpression.</param>
			internal void SetPropertyInfoSetter (PropertyInfo propertyInfo, LambdaExpression setter) {
				propertyInjectors [propertyInfo] = new MemberSetterConfig<PropertyInfo> {
					ConcreteType = ConcreteType,
					MemberInfo = propertyInfo,
					MemberType = propertyInfo.PropertyType,
					MemberSetter = setter
				}.Validate();
			}

			/// <summary>
			/// Gets the property info setters.
			/// </summary>
			/// <returns>The property info setters.</returns>
			internal IMemberSetterConfig<PropertyInfo>[] GetPropertyInfoSetters() {
				return propertyInjectors.Values.ToArray ();
			}

			/// <summary>
			/// Adds the field info setter.
			/// </summary>
			/// <param name="fieldInfo">Field info.</param>
			/// <param name="setter">This may be null to allow for an implicit binding or a 0-arg LambdaExpression.</param>
			internal void SetFieldInfoSetter (FieldInfo fieldInfo, LambdaExpression setter) {
				fieldInjectors [fieldInfo] = new MemberSetterConfig<FieldInfo> {
					ConcreteType = ConcreteType,
					MemberInfo = fieldInfo,
					MemberType = fieldInfo.FieldType,
					MemberSetter = setter
				}.Validate();
			}

			/// <summary>
			/// Gets the field info setters.
			/// </summary>
			/// <returns>The field info setters.</returns>
			internal IMemberSetterConfig<FieldInfo>[] GetFieldInfoSetters() {
				return fieldInjectors.Values.ToArray ();
			}
		}

		/// <summary>
		/// Binding config implementation.
		/// </summary>
		internal class BindingConfig<CType> : BindingConfig 
			where CType : class 
		{
			private readonly Type cType = typeof(CType);
			private ConstructorInfo constructor;
			private LambdaExpression factoryExpression;

			internal BindingConfig()
			{
				EnsureConstructoryOrFactory();
				Lifestyle = Lifestyle.Transient;
			}

			protected override Type ConcreteType {
				get {
					return typeof(CType);
				}
			}

			//
			// Attributes and Methods
			//
			internal override ConstructorInfo Constructor { 
				get { 
					return constructor; 
				}
				set { 
					constructor = value; 
					EnsureConstructoryOrFactory();
				} 
			}

			internal override LambdaExpression FactoryExpression { 
				get { 
					return factoryExpression;
				}
				set { 
					factoryExpression = value;
					EnsureConstructoryOrFactory();
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="IfInjector.IfPlatform.BindingConfig`1"/> class, including determining the initial constructor.
			/// </summary>
			private void EnsureConstructoryOrFactory() {
				// Do not trigger property change
				if (factoryExpression == null && constructor == null) {
					if (ConcreteType.IsInterface || ConcreteType.IsAbstract) {
						// if we can not instantiate, set the resolver to throw an exception.
						Expression<Func<CType>> throwEx = () => ThrowInterfaceException ();
						factoryExpression = throwEx;
					} else {
						// try to find the default constructor and create a default resolver from it
						var ctor = cType.GetConstructors ()
							.OrderBy (v => Attribute.IsDefined (v, typeof(InjectAttribute)) ? 0 : 1)
								.ThenBy (v => v.GetParameters ().Count ())
								.FirstOrDefault ();

						if (ctor != null) {
							constructor = ctor;
						} else {
							Expression<Func<CType>> throwEx = () => ThrowConstructorException ();
							factoryExpression = throwEx;
						}
					}
				}
			}

			private CType ThrowConstructorException() {
				throw InjectorErrors.ErrorNoAppropriateConstructor.FormatEx (cType.FullName);
			}

			private CType ThrowInterfaceException() {
				throw InjectorErrors.ErrorUnableToResultInterface.FormatEx(cType.FullName);
			}
		}

		/// <summary>
		/// Internal utilities for binding classes.
		/// </summary>
		internal static class BindingUtil {
			internal static void AddPropertyInjectorToBindingConfig<CType, TPropertyType>(
				BindingConfig bindingConfig,
				Expression<Func<CType, TPropertyType>> propertyExpression, 
				Expression<Func<TPropertyType>> setter) 
				where CType : class
			{
				var propertyMemberExpression = propertyExpression.Body as MemberExpression;
				if (propertyMemberExpression == null) {
					throw InjectorErrors.ErrorMustContainMemberExpression.FormatEx ("propertyExpression");
				}

				var member = propertyMemberExpression.Member;
				if (member is PropertyInfo) {
					bindingConfig.SetPropertyInfoSetter (member as PropertyInfo, setter);
				} else if (member is FieldInfo) {
					bindingConfig.SetFieldInfoSetter (member as FieldInfo, setter);
				} else {
					// Should not be reachable.
					throw InjectorErrors.ErrorMustContainMemberExpression.FormatEx ("propertyExpression");
				}
			}
		}
	}
}
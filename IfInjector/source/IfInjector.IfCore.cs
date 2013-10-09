using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using IfInjector.IfCore;

/// <summary>
/// Internal core namespace.
/// </summary>
namespace IfInjector.IfCore 
{
	/// <summary>
	/// Utility classes and types to deal with platform limitations. 
	/// 
	/// At this point in time, these types are internal.
	/// </summary>
	namespace IfPlatform
	{
		/// <summary>
		/// Substitute for HashSet on Windows Phone. This object must be locked by the API user.
		/// </summary>
		internal class SetShim<T> : IEnumerable<T>, IEnumerable
		{
			private readonly Dictionary<T, bool> data = new Dictionary<T, bool>();

			public SetShim(IEnumerable<T> collection = null) {
				UnionWith (collection);
			}

			public int Count { get { return data.Count; } }

			public void Add(T item) {
				data [item] = true;
			}

			public bool Remove(T item) {
				return data.Remove (item);
			}

			public void Clear() {
				data.Clear();
			}

			public bool Contains(T item) {
				return data.ContainsKey(item);
			}

			public void CopyTo(T[] array) {
				UnionWith(array);
			}

			public IEnumerator<T> GetEnumerator() {
				return data.Keys.GetEnumerator();
			}

			public void UnionWith(IEnumerable<T> items) {
				if (items != null) {
					foreach (var item in items) {
						Add (item);
					}
				}
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return data.Keys.GetEnumerator();
			}
		}

		/// <summary>
		/// Thread safe dictionary wrapper. Users supply a synchronization lock to allow for course-grained locking. Course grained locking helps to prevent obscure deadlock issues. 
		/// 
		/// For performance, the dictionary supports [optional] unsynced read operations.
		/// </summary>
		internal class SafeDictionary<TKey,TValue> {
			private readonly object syncLock;
			private readonly Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
			private Dictionary<TKey, TValue> unsyncDict = new Dictionary<TKey, TValue> ();

			public SafeDictionary(object syncLock) {
				this.syncLock = syncLock;
			}

			public void Add(TKey key, TValue value) {
				lock (syncLock) {
					dict.Add(key, value);
					unsyncDict = new Dictionary<TKey, TValue> (dict);
				}
			}

			public bool TryGetValue(TKey key, out TValue value) {
				lock (syncLock) {
					return dict.TryGetValue (key, out value);
				}
			}

			public bool UnsyncedTryGetValue(TKey key, out TValue value) {
				return unsyncDict.TryGetValue (key, out value);
			}

			public IEnumerable<KeyValuePair<TKey, TValue>> UnsyncedEnumerate() {
				return unsyncDict;
			}

			public bool ContainsKey(TKey key) {
				lock (syncLock) {
					return dict.ContainsKey(key);
				}
			}

			public IEnumerable<TValue> Values {
				get {
					lock (syncLock) {
						// use unsync since that is a copy on write object.
						return unsyncDict.Values;
					}
				}
			}

			public bool Remove(TKey key) {
				lock (syncLock) {
					bool res = dict.Remove (key);
					if (res) {
						unsyncDict = new Dictionary<TKey, TValue> (dict);
					}
					return res;
				}
			}
		}
	}

	/// <summary>
	/// Binding types for lifestyles
	/// </summary>
	namespace IfLifestyle
	{
		/// <summary>
		/// Lifestyle resolver.
		/// </summary>
		internal abstract class LifestyleResolver<CType> where CType : class {
			private readonly Expression<Func<CType>> resolveExpression;

			internal LifestyleResolver(Expression<Func<CType>> resolveExpression) {
				this.resolveExpression = resolveExpression;
			}

			internal LifestyleResolver() {
				this.resolveExpression = () => Resolve();
			}

			internal Expression<Func<CType>> ResolveExpression {
				get {
					return resolveExpression;
				}
			}

			internal abstract CType Resolve();
		}

		/// <summary>
		/// Base lifestyle class
		/// </summary>
		public abstract class Lifestyle {
			public static readonly Lifestyle Singleton = new SingletonLifestyle();
			public static readonly Lifestyle Transient = new TransientLifestyle();

			/// <summary>
			/// Gets the lifestyle resolver.
			/// </summary>
			/// <returns>The lifestyle resolver.</returns>
			/// <param name="syncLock">Sync lock.</param>
			/// <param name="resolverExpression">Resolver expression.</param>
			/// <param name="resolverExpressionCompiled">Resolver expression compiled.</param>
			/// <param name="testInstance">Test instance.</param>
			/// <typeparam name="CType">The 1st type parameter.</typeparam>
			internal abstract LifestyleResolver<CType> GetLifestyleResolver<CType>(
					object syncLock, 
					Expression<Func<CType>> resolverExpression,
					Func<CType> resolverExpressionCompiled,
					CType testInstance) 
				where CType : class;

			/// <summary>
			/// Creates a custom lifestyle.
			/// </summary>
			/// <returns>The custom lifestyle delegate.</returns>
			/// <param name="customLifestyle">The custom lifestyle.</param>
			public static Lifestyle CreateCustom(CustomLifestyleDelegate customLifestyle) {
				return new CustomLifestyle (customLifestyle);
			}

			/////////
			// Internal impl for singleton
			private class SingletonLifestyle : Lifestyle {
				internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
					object syncLock, 
					Expression<Func<CType>> resolverExpression,
					Func<CType> resolverExpressionCompiled,
					CType testInstance)
				{
					return new SingletonLifestyleResolver<CType>(resolverExpression, testInstance);
				}

				private class SingletonLifestyleResolver<CType> : LifestyleResolver<CType> where CType : class {
					private readonly CType instance;

					internal SingletonLifestyleResolver(Expression<Func<CType>> resolveExpression, CType instance) :
						base(resolveExpression)
					{
						this.instance = instance;
					}

					internal override CType Resolve() {
						return instance;
					}
				}
			}

			/////////
			// Internal impl for transient
			private class TransientLifestyle : Lifestyle {
				internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
					object syncLock, 
					Expression<Func<CType>> resolverExpression,
					Func<CType> resolverExpressionCompiled,
					CType testInstance)
				{
					return new TransientLifestyleResolver<CType>(resolverExpression, resolverExpressionCompiled);
				}

				private class TransientLifestyleResolver<CType> : LifestyleResolver<CType> where CType : class {
					private readonly Func<CType> resolverExpressionCompiled;

					internal TransientLifestyleResolver(Expression<Func<CType>> resolveExpression, Func<CType> resolverExpressionCompiled) : base(resolveExpression) {
						this.resolverExpressionCompiled = resolverExpressionCompiled;
					}

					internal override CType Resolve() {
						return resolverExpressionCompiled();
					}
				}
			}

			/// <summary>
			/// Used to create custom lifestyle.
			/// </summary>
			public delegate Func<object> CustomLifestyleDelegate(Func<object> instanceCreator);

			public class CustomLifestyle : Lifestyle {
				private readonly CustomLifestyleDelegate lifestyleDelegate;

				internal CustomLifestyle(CustomLifestyleDelegate lifestyleDelegate) {
					this.lifestyleDelegate = lifestyleDelegate;
				}

				internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
					object syncLock, 
					Expression<Func<CType>> resolverExpression,
					Func<CType> resolverExpressionCompiled,
					CType testInstance)
				{
					Func<object> instanceCreator = () => resolverExpressionCompiled ();
					return new BaseCustomLifecyle<CType>(resolverExpression, lifestyleDelegate(instanceCreator));
				}

				private class BaseCustomLifecyle<CType> : LifestyleResolver<CType> where CType : class {
					private readonly Func<object> instanceCreator;

					internal BaseCustomLifecyle(
						Expression<Func<CType>> resolveExpression, 
						Func<object> instanceCreator) : base(resolveExpression) 
					{
						this.instanceCreator = instanceCreator;
					}

					internal override CType Resolve() {
						return (CType) instanceCreator();
					}
				}
			}
		}
	}

	/// <summary>
	/// Binding interfaces and core implementation.
	/// 
	/// At this point in time, these types are internal.
	/// </summary>
	namespace IfBinding
	{
		using IfLifestyle;

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
		/// Internal utilities for binding classes.
		/// </summary>
		internal static class BindingUtil {
			internal static void AddPropertyInjectorToBindingConfig<CType, TPropertyType>(
				IBindingConfig bindingConfig,
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

		/// <summary>
		/// Internal interface for working with binding objects.
		/// </summary>
		internal interface IInternalBinding {
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
		
		internal class BoundBinding<BType, CType> : IBoundBinding<BType, CType>, IInternalBinding
			where BType : class
			where CType : class, BType
		{
			public IBindingConfig BindingConfig { get; private set; }
			public BindingKey BindingKey { get; private set; }
			public Type ConcreteType { get { return typeof(CType); } }

			internal BoundBinding(IBindingConfig bindingConfig) {
				BindingConfig = bindingConfig;
				BindingKey = BindingKey.Get<BType> ();
				Injector.ImplicitTypeUtilities.SetupImplicitPropResolvers<CType> (bindingConfig, this);
			}

			public IBinding SetLifestyle (IfLifestyle.Lifestyle lifestyle) {
				BindingConfig.Lifestyle = lifestyle;
				return this;
			}

			public IBoundBinding<BType, CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
					where TPropertyType : class
			{
				return AddPropertyInjectorInner<TPropertyType> (propertyExpression, null);
			}
			
			public IBoundBinding<BType, CType> AddPropertyInjector<TPropertyType> (
				Expression<Func<CType, TPropertyType>> propertyExpression, 
				Expression<Func<TPropertyType>> setter)
			{
				return AddPropertyInjectorInner<TPropertyType> (propertyExpression, setter);
			}

			private IBoundBinding<BType, CType> AddPropertyInjectorInner<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter) {
				BindingUtil.AddPropertyInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, propertyExpression, setter);
				return this;
			}
		}

		internal class OngoingBinding<BType> : BoundBinding<BType, BType>, IOngoingBinding<BType>
			where BType : class
		{
			internal OngoingBinding() : base(new BindingConfig<BType>()) {}

			public IBoundBinding<BType, CType> To<CType> () 
				where CType : class, BType
			{
				return new BoundBinding<BType, CType> (new BindingConfig<CType>());
			}

			public IBoundBinding<BType, CType> SetFactoryLambda<CType>(LambdaExpression factoryExpression)
				where CType : class, BType
			{
				var bindingConfig = new BindingConfig<CType> ();
				var boundBinding = new BoundBinding<BType, CType> (bindingConfig); 
				bindingConfig.FactoryExpression = factoryExpression;
				return boundBinding;
			}
		}

		/// <summary>
		/// Binding config change handler.
		/// </summary>
		public delegate void BindingConfigEventHandler(object sender, EventArgs e);

		/// <summary>
		/// Binding config. At present time this is not part of the public API. In the future it may be open to allow.
		/// 
		/// Caller's must synchronize access via the syncLock.
		/// </summary>
		internal interface IBindingConfig {

			/// <summary>
			/// Occurs when binding changed.
			/// </summary>
			event BindingConfigEventHandler Changed;

			/// <summary>
			/// Gets or sets the lifestyle.
			/// </summary>
			/// <value>The lifestyle.</value>
			Lifestyle Lifestyle { get; set; }

			/// <summary>
			/// Gets or sets the constructor.
			/// </summary>
			/// <value>The constructor.</value>
			ConstructorInfo Constructor { get; set; }

			/// <summary>
			/// Gets or sets the factory expression.
			/// </summary>
			/// <value>The factory expression.</value>
			LambdaExpression FactoryExpression { get; set; }

			/// <summary>
			/// Adds the property info setter.
			/// </summary>
			/// <param name="propertyInfo">Property info.</param>
			/// <param name="setter">This may be null to allow for an implicit binding or a 0-arg LambdaExpression.</param>
			void SetPropertyInfoSetter (PropertyInfo propertyInfo, LambdaExpression setter);

			/// <summary>
			/// Gets the property info setters.
			/// </summary>
			/// <returns>The property info setters.</returns>
			IMemberSetterConfig<PropertyInfo>[] GetPropertyInfoSetters();

			/// <summary>
			/// Adds the field info setter.
			/// </summary>
			/// <param name="fieldInfo">Field info.</param>
			/// <param name="setter">This may be null to allow for an implicit binding or a 0-arg LambdaExpression.</param>
			void SetFieldInfoSetter (FieldInfo fieldInfo, LambdaExpression setter);

			/// <summary>
			/// Gets the field info setters.
			/// </summary>
			/// <returns>The field info setters.</returns>
			IMemberSetterConfig<FieldInfo>[] GetFieldInfoSetters();
		}

		/// <summary>
		/// Binding config implementation.
		/// </summary>
		internal class BindingConfig<CType> : IBindingConfig 
			where CType : class 
		{
			//
			// Setter Config
			//
			private class MemberSetterConfig<MTInfo> : IMemberSetterConfig<MTInfo> where MTInfo : MemberInfo
			{
				public LambdaExpression MemberSetter { get; protected internal set; }
				public MTInfo MemberInfo { get; protected internal set; }
				public Type MemberType { get; protected internal set; }

				public MemberSetterConfig<MTInfo> Validate() {
					if (MemberSetter == null && !MemberType.IsClass && !MemberType.IsInterface) {
						throw InjectorErrors.ErrorUnableToBindNonClassFieldsProperties.FormatEx(MemberInfo.Name, typeof(CType).FullName);
					}
					return this;
				}
			}

			//
			// Fields
			//
			private readonly Type cType = typeof(CType);
			private Lifestyle lifestyle = Lifestyle.Transient;
			private ConstructorInfo constructor;
			private LambdaExpression factoryExpression;

			private readonly Dictionary<PropertyInfo, IMemberSetterConfig<PropertyInfo>> propertyInjectors 
				= new Dictionary<PropertyInfo, IMemberSetterConfig<PropertyInfo>>();
			private readonly Dictionary<FieldInfo, IMemberSetterConfig<FieldInfo>> fieldInjectors 
				= new Dictionary<FieldInfo, IMemberSetterConfig<FieldInfo>>();

			//
			// Event handler
			//
			public event BindingConfigEventHandler Changed;

			private void OnChange() {
				if (Changed != null) {
					Changed (this, EventArgs.Empty);
				}
			}

			//
			// Attributes and Methods
			//
			public Lifestyle Lifestyle { 
				get { 
					return lifestyle; 
				}
				set {
					this.lifestyle = value;
					OnChange ();
				}
			}

			public ConstructorInfo Constructor { 
				get { 
					return constructor; 
				}
				set { 
					constructor = value; 
					EnsureConstructoryOrFactory();
					OnChange();
				} 
			}

			public LambdaExpression FactoryExpression { 
				get { 
					return factoryExpression;
				}
				set { 
					factoryExpression = value;
					EnsureConstructoryOrFactory();
					OnChange ();
				}
			}

			/// <summary>
			/// Initializes a new instance of the <see cref="IfInjector.IfPlatform.BindingConfig`1"/> class, including determining the initial constructor.
			/// </summary>
			internal BindingConfig()
			{
				EnsureConstructoryOrFactory();
			}

			private void EnsureConstructoryOrFactory() {
				// Do not trigger property change
				if (factoryExpression == null && constructor == null) {
					if (cType.IsInterface || cType.IsAbstract) {
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

			public void SetPropertyInfoSetter (PropertyInfo propertyInfo, LambdaExpression setter) {
				propertyInjectors [propertyInfo] = new MemberSetterConfig<PropertyInfo> {
					MemberInfo = propertyInfo,
					MemberType = propertyInfo.PropertyType,
					MemberSetter = setter
				}.Validate();
			}

			public IMemberSetterConfig<PropertyInfo>[] GetPropertyInfoSetters() {
				return propertyInjectors.Values.ToArray ();
			}

			public void SetFieldInfoSetter (FieldInfo fieldInfo, LambdaExpression setter) {
				fieldInjectors [fieldInfo] = new MemberSetterConfig<FieldInfo> {
					MemberInfo = fieldInfo,
					MemberType = fieldInfo.FieldType,
					MemberSetter = setter
				}.Validate();
			}

			public IMemberSetterConfig<FieldInfo>[] GetFieldInfoSetters() {
				return fieldInjectors.Values.ToArray ();
			}
		}
	}

	/// <summary>
	/// Types relating to expression compilation.
	/// 
	/// At this point in time, these types are internal.
	/// </summary>
	namespace IfExpression
	{
		using IfPlatform;
		using IfBinding;

		/// <summary>
		/// Resolve instance expression.
		/// </summary>
		internal delegate Expression ResolveResolverExpression(BindingKey bindingKey, SetShim<BindingKey> callerDependencies);

		/// <summary>
		/// Expression compiler definition. Synchronization must be managed externally by the API caller.
		/// </summary>
		internal interface IExpressionCompiler<CType> where CType : class {
			/// <summary>
			/// Gets or sets the dependencies.
			/// </summary>
			/// <value>The dependencies.</value>
			SetShim<BindingKey> Dependencies { get; }

			/// <summary>
			/// Compiles the resolver expression.
			/// </summary>
			/// <returns>The resolver expression.</returns>
			Expression<Func<CType>> CompileResolverExpression();

			/// <summary>
			/// Compiles the properties resolver expr.
			/// </summary>
			/// <returns>The properties resolver expr.</returns>
			/// <param name="bindingConfig">Binding config.</param>
			Func<CType, CType> CompilePropertiesResolver ();
		}

		internal class ExpressionCompiler<CType> : IExpressionCompiler<CType> where CType : class {

			private static readonly MethodInfo compileFieldSetterExpressionGeneric;

			static ExpressionCompiler() {
				Expression<Action> val = () => CompileFieldSetterExpressionGeneric<Exception> (null, null, null);
				compileFieldSetterExpressionGeneric = ((MethodCallExpression)val.Body).Method.GetGenericMethodDefinition();
			}

			private readonly IBindingConfig bindingConfig;

			public SetShim<BindingKey> Dependencies { get; private set; }

			public ResolveResolverExpression ResolveResolverExpression { set; private get; }

			/// <summary>
			/// Initializes a new instance of the <see cref="IfInjector.IfPlatform.ExpressionCompiler`1"/> class.
			/// 
			/// The binding is cloned to 
			/// </summary>
			/// <param name="bindingConfig">Binding config.</param>
			internal ExpressionCompiler(IBindingConfig bindingConfig) {
				this.Dependencies = new SetShim<BindingKey> ();
				this.bindingConfig = bindingConfig;
			}

			public Expression<Func<CType>> CompileResolverExpression() {
				if (bindingConfig.FactoryExpression != null) {
					var factoryExpr = CompileFactoryExpr ();
					var fieldInjectors = bindingConfig.GetFieldInfoSetters ();
					var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

					if (fieldInjectors.Any () || propertyInjectors.Any ()) {
						return CompileFactoryExprSetters (factoryExpr);
					} else {
						return factoryExpr;
					}
				} else {
					return CompileConstructorExpr ();
				}
			}

			public Func<CType,CType> CompilePropertiesResolver()
			{
				var fieldInjectors = bindingConfig.GetFieldInfoSetters ();
				var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

				if (fieldInjectors.Length > 0 || propertyInjectors.Length > 0) {
					return CompilePropertiesResolverExpr ().Compile ();
				} else {
					return (CType x) => { return x; };
				}
			}

			private Expression<Func<CType, CType>> CompilePropertiesResolverExpr()
			{
				var instance = Expression.Parameter (typeof(CType), "instanceR");

				var blockExpression = new List<Expression> ();
				AddFieldSetterExpressions(instance, blockExpression);
				AddPropertySetterExpressions(instance, blockExpression);

				var blockFuncs = new List<Action<CType>>(
					from be in blockExpression 
					select Expression.Lambda<Action<CType>>(be, instance).Compile());

				Expression<Func<CType,CType>> setSettersExpr = (CType inst) => CallSetterList (inst, blockFuncs);

				return setSettersExpr;
			}

			private Expression<Func<CType>> CompileFactoryExpr()
			{
				var arguments = CompileArgumentListExprs(bindingConfig.FactoryExpression.Parameters.Select (x => x.Type));
				var callLambdaExpression = Expression.Invoke (bindingConfig.FactoryExpression, arguments.ToArray());
				return ((Expression<Func<CType>>)Expression.Lambda(callLambdaExpression));
			}

			private Expression<Func<CType>> CompileConstructorExpr()
			{
				var arguments = CompileArgumentListExprs(bindingConfig.Constructor.GetParameters().Select(v => v.ParameterType));
				var createInstanceExpression = Expression.New(bindingConfig.Constructor, arguments);

				var fieldInjectors = bindingConfig.GetFieldInfoSetters ();
				var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

				if (fieldInjectors.Any () || propertyInjectors.Any ()) {
					var fields = from iconf in fieldInjectors select Expression.Bind (iconf.MemberInfo, GetSetterValueExpression(iconf));
					var props = from iconf in propertyInjectors select Expression.Bind (iconf.MemberInfo, GetSetterValueExpression(iconf));
					var fullInit = Expression.MemberInit (createInstanceExpression, fields.Union(props).ToArray());

					return ((Expression<Func<CType>>)Expression.Lambda(fullInit));
				} else {
					return ((Expression<Func<CType>>)Expression.Lambda(createInstanceExpression));
				}
			}

			private List<Expression> CompileArgumentListExprs(IEnumerable<Type> args) {
				var argumentsOut = new List<Expression>();

				foreach (var parameterType in args) {
					var argument = GetResolverInvocationExpressionForType(parameterType);
					argumentsOut.Add(argument);
				}

				return argumentsOut;
			}

			private Expression<Func<CType>> CompileFactoryExprSetters(Expression<Func<CType>> factoryExpr)
			{
				Func<CType> factory = factoryExpr.Compile ();
				var propertiesResolver = CompilePropertiesResolver ();
				Expression<Func<CType>> func = () => propertiesResolver(factory());
				return func;
			}

			private Expression GetSetterValueExpression<MIType>(IMemberSetterConfig<MIType> setter) where MIType : MemberInfo {
				if (IsResolve (setter)) {
					return GetResolverInvocationExpressionForType (setter.MemberType);
				} else {
					return setter.MemberSetter.Body;
				}
			}

			private bool IsResolve<MIType>(IMemberSetterConfig<MIType> setter) where MIType : MemberInfo {
				return setter.MemberSetter == null;
			}

			private Expression GetResolverInvocationExpressionForType(Type parameterType) {
				return ResolveResolverExpression(BindingKey.Get(parameterType), Dependencies);
			}

			private void AddPropertySetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) {
				var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

				foreach (var prop in propertyInjectors) {
					var valueExpr =  GetSetterValueExpression (prop);
					var setMethod = prop.MemberInfo.GetSetMethod(true);
					var propAssignExpr = Expression.Call(instanceVar, setMethod, valueExpr);
					blockExpressions.Add (propAssignExpr);
				}
			}

			private static CType CallSetterList(CType instance, List<Action<CType>> setters) {
				foreach (var setter in setters) {
					setter (instance);
				}
				return instance;
			}

			private void AddFieldSetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) 
			{
				var fieldInjectors = bindingConfig.GetFieldInfoSetters ();

				foreach (var field in fieldInjectors) {
					var valueExpr = GetSetterValueExpression (field);
					var fieldExpr = CompileFieldSetterExpression (instanceVar, field.MemberInfo, valueExpr);
					blockExpressions.Add (fieldExpr);
				}
			}

			private static Expression CompileFieldSetterExpression(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
			{
				return (Expression) compileFieldSetterExpressionGeneric.MakeGenericMethod (fieldInfo.FieldType).Invoke (null, new object[]{instanceVar, fieldInfo, valueExpr});
			}

			private static Expression CompileFieldSetterExpressionGeneric<TPropertyType>(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
			{
				var valueFunc = Expression.Lambda<Func<TPropertyType>> (valueExpr).Compile();
				Expression<Action<CType>> setValueExpr = inst => CallSetField<TPropertyType>(inst, fieldInfo, valueFunc);
				return Expression.Invoke(setValueExpr, instanceVar);
			}

			private static void CallSetField<TPropertyType>(CType instance, FieldInfo fieldInfo, Func<TPropertyType> propValue)
			{
				fieldInfo.SetValue (instance, propValue());
			}
		}
	}
}
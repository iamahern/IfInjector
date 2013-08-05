using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IfInjector.IfInjectorTypes;

namespace IfInjector
{
	internal abstract class InjectorInternal
	{
		protected internal interface IResolver {
			/// <summary>
			/// Checks if the 'touched' type is in the dependency list for the current resolver. If it is, clear the resolver.
			/// </summary>
			/// <param name="touched">The type that has been modified.</param>
			void ConditionalClearResolver (Type touched);

			/// <summary>
			/// Resolves the object.
			/// </summary>
			/// <returns>The resolve.</returns>
			object DoResolve ();

			/// <summary>
			/// Injects the properties.
			/// </summary>
			/// <param name="instance">Instance.</param>
			void DoInject (object instance);

			/// <summary>
			/// Gets the resolve expression.
			/// </summary>
			/// <returns>The resolve expression.</returns>
			/// <param name="callerDeps">HashSet of the 'callers' dependencies. IResolver adds itself and its depdendencies to the set.</param>
			Expression GetResolveExpression (HashSet<Type> callerDeps);
		}
		
		/// <summary>
		/// The actual injector implementation.
		/// </summary>
		internal class InjectorImpl : Injector
		{		
			private readonly object syncLock = new object();
			private readonly MethodInfo createResolverInstanceGeneric;

			private readonly SafeDictionary<Type, IResolver> allResolvers;
			private readonly SafeDictionary<Type, IResolver> allImplicitResolvers;
			private readonly SafeDictionary<Type, ISet<Type>> implicitTypeLookup;

			public InjectorImpl() 
			{
				// Init dictionaries
				allResolvers = new SafeDictionary<Type, IResolver>(syncLock);
				allImplicitResolvers = new SafeDictionary<Type, IResolver>(syncLock);
				implicitTypeLookup = new SafeDictionary<Type, ISet<Type>> (syncLock);

				// Init resolver
				Expression<Action> tmpExpr = () => CreateResolverInstanceGeneric<Exception, Exception>(true);
				createResolverInstanceGeneric = ((MethodCallExpression)tmpExpr.Body).Method.GetGenericMethodDefinition();
			}

			public override object Resolve(Type type)
			{
				return ResolveResolver (type).DoResolve ();
			}

			public override T InjectProperties<T> (T instance, bool useExplicitBinding = false)
			{
				var iResolver = useExplicitBinding ? ResolveResolver (typeof(T)) : ResolveImplicitOnlyResolver (typeof(T));
				iResolver.DoInject (instance);
				return instance;
			}

			private IResolver ResolveImplicitOnlyResolver(Type type)
			{
				IResolver resolver;
				if (allImplicitResolvers.UnsyncedTryGetValue (type, out resolver)) {
					return resolver;
				} else {
					return BindImplicitOnly (type);
				}
			}

			protected internal IResolver ResolveResolver(Type type)
			{
				ISet<Type> lookup;
				IResolver resolver;

				if (allResolvers.UnsyncedTryGetValue (type, out resolver)) {
					return resolver;
				} else if (implicitTypeLookup.UnsyncedTryGetValue (type, out lookup) && lookup.Count > 0) {
					if (lookup.Count == 1) {
						return ResolveResolver (lookup.First());
					} else {
						throw InjectorErrors.ErrorAmbiguousBinding.FormatEx(type.Name);
					}
				} 

				return BindImplicit (type);
			}

			public override IInjectorBinding<CType> Bind<BType, CType>()
			{
				var iResolver = BindExplicit<BType, CType> ();
				return new InjectorBinding<CType>(iResolver);
			}

			public override void Verify()
			{
				lock (syncLock) {
					foreach (var resolver in allResolvers.Values) {
						resolver.DoResolve ();
					}
				}
			}

			private Resolver<CType> BindExplicit<BType, CType>()
				where BType : class
				where CType : class, BType
			{
				lock (syncLock) {
					Type bindType = typeof(BType);

					implicitTypeLookup.Remove (bindType);
					allResolvers.Remove (bindType);

					// Add after create resolver
					var resolver = CreateResolverInstanceGeneric<BType, CType> (false);
					AddImplicitTypes (bindType, GetImplicitTypes(bindType));

					ClearDependentResolvers (bindType);

					return (Resolver<CType>) resolver;
				}
			}

			private IResolver BindImplicit(Type bindType) {
				lock (syncLock) {
					IResolver resolver;
					if (allResolvers.TryGetValue (bindType, out resolver)) {
						return resolver;
					}

					// Handle implementedBy
					var implType = GetIfImplementedBy (bindType);
					if (implType != null) {
						resolver = CreateResolverInstance (bindType, implType, true);
					} else {
						resolver = CreateResolverInstance (bindType, bindType, true);
					}

					// NOTE: In theory, implicit bindings should never change the object graph;
					// ... TODO EDGE Case - dynamically created assemblies; clearing as below should prevent issues, need test
					ClearDependentResolvers (bindType);

					return resolver;
				}
			}

			private IResolver BindImplicitOnly(Type bindType) {
				lock (syncLock) {
					IResolver resolver;
					if (allImplicitResolvers.TryGetValue (bindType, out resolver)) {
						return resolver;
					}

					return CreateResolverInstance (bindType, bindType, true);
				}
			}

			private Type GetIfImplementedBy(Type type) {
				var implTypeAttrs = type.GetCustomAttributes(typeof(ImplementedByAttribute), false);
				if (implTypeAttrs.Length > 0) {
					return (implTypeAttrs[0] as ImplementedByAttribute).Implementor;
				}
				return null;
			}

			private IResolver CreateResolverInstance(Type keyType, Type implType, bool isImplicitBinding) {
				try {
					return (IResolver) createResolverInstanceGeneric.MakeGenericMethod(keyType, implType).Invoke(this, new object[]{isImplicitBinding});
				} catch (TargetInvocationException ex) {
					throw ex.InnerException;
				}
			}

			private Resolver<CType> CreateResolverInstanceGeneric<BType, CType>(bool isImplicitBinding) 
				where BType : class
				where CType : class, BType
			{
				var bindType = typeof(BType);
				var resolver = new Resolver<CType> (this, bindType, syncLock);
				
				if (isImplicitBinding) {
					allImplicitResolvers.Add (bindType, resolver);
					if (!allResolvers.ContainsKey (bindType)) {
						allResolvers.Add (bindType, resolver);
					}
				} else {
					allResolvers.Add (bindType, resolver);
				}
				
				SetupImplicitPropResolvers<CType> (resolver);

				return resolver;
			}

			private void AddImplicitTypes(Type boundType, ISet<Type> implicitTypes) {
				lock (syncLock) {
					foreach(Type implicitType in implicitTypes) {
						if (GetIfImplementedBy (implicitType) == null) {
							ISet<Type> newSet, oldSet;

							if (implicitTypeLookup.TryGetValue (implicitType, out oldSet)) {
								implicitTypeLookup.Remove (implicitType);
								newSet = new HashSet<Type> (oldSet);
							} else {
								newSet = new HashSet<Type> ();
							}

							newSet.Add (boundType);
							implicitTypeLookup.Add (implicitType, newSet);
						} else {
							BindImplicit (implicitType);
						}
					}
				}
			}

			/// <summary>
			/// Clears the dependent resolvers.
			/// </summary>
			/// <param name="keyType">Key type.</param>
			protected internal void ClearDependentResolvers(Type keyType) {
				lock (syncLock) {
					foreach (var resolver in allResolvers.Values) {
						resolver.ConditionalClearResolver (keyType);
					}

					foreach (var resolver in allImplicitResolvers.Values) {
						resolver.ConditionalClearResolver (keyType);
					}
				}
			}
		}

		protected internal class Resolver<CType> : IResolver 
			where CType : class 
		{
			private readonly Type cType = typeof(CType);
			private readonly Type keyType;

			private readonly object syncLock;

			private bool singleton;
			private readonly Dictionary<PropertyInfo, SetterExpression> propertyInjectors;
			private readonly Dictionary<FieldInfo, SetterExpression> fieldInjectors;

			private LambdaExpression ResolverFactoryExpression { get; set; }
			private ConstructorInfo MyConstructor { get; set; }

			private bool isVerifiedNotRecursive;
			private bool IsRecursionTestPending { get; set; }

			private Expression<Func<CType>> resolverExpression;
			private Func<CType> resolverExpressionCompiled;

			private Func<CType> resolve;
			private Action<CType> resolveProperties;

			private readonly HashSet<Type> dependencies = new HashSet<Type>();

			private readonly InjectorImpl injector;

			public Resolver(InjectorImpl injector, Type keyType, object syncLock)
			{
				this.keyType = keyType;
				this.syncLock = syncLock;

				this.injector = injector;

				this.propertyInjectors = new Dictionary<PropertyInfo, SetterExpression>();
				this.fieldInjectors = new Dictionary<FieldInfo, SetterExpression>();

				InitInitialResolver();
			}

			public object DoResolve() {
				return DoResolveTyped ();
			}

			private CType DoResolveTyped() {
				if (!IsResolved()) {
					CompileResolver ();
				}

				return resolve ();
			}

			public void DoInject(object instance) {
				if (instance != null) {
					if (resolveProperties == null) {
						lock (this) {
							resolveProperties = CompilePropertiesResolver ();
						}
					}

					resolveProperties (instance as CType);
				}
			}

			public Expression GetResolveExpression (HashSet<Type> callerDeps) {
				lock (syncLock) {
					Expression<Func<CType>> expr;
					if (singleton) {
						var instance = DoResolveTyped ();
						expr = () => instance;
					} else {
						if (!isVerifiedNotRecursive) {
							DoResolveTyped ();
						}
						expr = resolverExpression;
					}

					callerDeps.UnionWith (dependencies);
					callerDeps.Add (keyType);

					return expr.Body;
				}
			}

			private void InitInitialResolver()
			{
				if (cType.IsInterface || cType.IsAbstract)
				{
					// if we can not instantiate, set the resolver to throw an exception.
					Expression<Func<CType>> throwEx = () => ThrowInterfaceException ();
					ResolverFactoryExpression = throwEx;
				}
				else
				{
					// try to find the default constructor and create a default resolver from it
					var constructor = cType.GetConstructors().Where(v => Attribute.IsDefined(v, typeof(IgnoreConstructorAttribute)) == false).OrderBy(v => Attribute.IsDefined(v, typeof(InjectAttribute)) ? 0 : 1).ThenBy(v => v.GetParameters().Count()).FirstOrDefault();

					if (constructor != null) {
						MyConstructor = constructor;
					} else {
						Expression<Func<CType>> throwEx = () => ThrowConstructorException ();
						ResolverFactoryExpression = throwEx;
					}
				}
			}

			private CType ThrowConstructorException() {
				throw InjectorErrors.ErrorNoAppropriateConstructor.FormatEx (cType.FullName);
			}

			private CType ThrowInterfaceException() {
				throw InjectorErrors.ErrorUnableToResultInterface.FormatEx(cType.FullName);
			}

			protected internal void AddMethodInfoSetter(MemberInfo methodInfo, LambdaExpression setter)
			{
				lock (syncLock) {
					if (methodInfo is PropertyInfo) {
						var propertyInfo = methodInfo as PropertyInfo;
						propertyInjectors [propertyInfo] = new SetterExpression { Info = propertyInfo, MemberType = propertyInfo.PropertyType, Setter = setter };
					} else if (methodInfo is FieldInfo) {
						var fieldInfo = methodInfo as FieldInfo;
						fieldInjectors [fieldInfo] = new SetterExpression { Info = fieldInfo, MemberType = fieldInfo.FieldType, Setter = setter };
					} else {
						throw InjectorErrors.ErrorMustContainMemberExpression.FormatEx("propertyExpression");
					}
					ClearResolverAndDependents ();
				}
			}

			public void SetFactory(LambdaExpression factoryExpression)
			{
				lock (syncLock) {
					ResolverFactoryExpression = factoryExpression;
					ClearResolverAndDependents ();
				}
			}

			public void AsSingleton(bool singleton) {
				lock (syncLock) {
					this.singleton = singleton;
					ClearResolverAndDependents ();
				}
			}

			public void ConditionalClearResolver(Type type) {
				lock (syncLock) {
					if (dependencies.Contains(type)) {
						ClearResolver ();
					}
				}
			}

			private void ClearResolverAndDependents() {
				lock (syncLock) {
					injector.ClearDependentResolvers (keyType);
					ClearResolver ();
				}
			}

			private void ClearResolver() {
				lock (syncLock) {
					IsRecursionTestPending = false;
					isVerifiedNotRecursive = false;

					dependencies.Clear ();

					resolve = null;
					resolverExpressionCompiled = null;
					resolverExpression = null;
				}
			}

			private CType ResolveWithRecursionCheck()
			{
				// Lock until executed once; we will compile this away once verified
				lock (syncLock) {
					if (!isVerifiedNotRecursive) {
						if (IsRecursionTestPending) {
							throw InjectorErrors.ErrorResolutionRecursionDetected.FormatEx(cType.Name);
						}
						IsRecursionTestPending = true;
					}

					CType retval = resolverExpressionCompiled();

					isVerifiedNotRecursive = true;
					IsRecursionTestPending = false;

					if (this.singleton) {
						resolve = () => retval;
					} else {
						resolve = resolverExpressionCompiled;
					}
					return retval;
				}
			}

			private void CompileResolver() {
				lock (syncLock) {
					if (!IsResolved()) {
						// START: Handle compile loop
						if (IsRecursionTestPending) {
							throw InjectorErrors.ErrorResolutionRecursionDetected.FormatEx(cType.Name);
						}
						IsRecursionTestPending = true; 

						var constructorExpr = CompileConstructorExpr ();

						if (fieldInjectors.Any() || propertyInjectors.Any()) {
							var instanceVar = Expression.Variable(cType);
							var assignExpression = Expression.Assign(instanceVar, constructorExpr.Body);

							var blockExpression = new List<Expression>();
							blockExpression.Add(assignExpression);

							// setters
							AddPropertySetterExpressions (instanceVar, blockExpression);

							// return value + Func<T>
							blockExpression.Add(instanceVar); 

							var expressionFunc = Expression.Block(new ParameterExpression[] { instanceVar }, blockExpression);
							resolverExpression = (Expression<Func<CType>>)Expression.Lambda(expressionFunc, constructorExpr.Parameters);

						} else {
							resolverExpression = constructorExpr;
						}

						resolverExpressionCompiled = resolverExpression.Compile ();
						resolve = ResolveWithRecursionCheck;

						IsRecursionTestPending = false; // END: Handle compile loop
					}
				}
			}

			private bool IsResolved() {
				return resolve != null;
			}

			public Action<CType> CompilePropertiesResolver() {
				lock (syncLock) {
					if (fieldInjectors.Any() || propertyInjectors.Any()) {
						var instance = Expression.Parameter (typeof(CType), "instance");
						var instanceVar = Expression.Variable(cType);

						var assignExpression = Expression.Assign(instanceVar, instance);

						var blockExpression = new List<Expression> ();
						blockExpression.Add(assignExpression);
						AddPropertySetterExpressions (instanceVar, blockExpression);

						var expression = Expression.Block(new [] { instanceVar }, blockExpression);

						return Expression.Lambda<Action<CType>>(expression, instance).Compile();
					} else {
						return (CType x) => {};
					}
				}
			}

			private Expression<Func<CType>> CompileConstructorExpr()
			{
				if (ResolverFactoryExpression != null) {
					var arguments = CompileArgumentListExprs(ResolverFactoryExpression.Parameters.Select (x => x.Type));
					var callLambdaExpression = Expression.Invoke (ResolverFactoryExpression, arguments.ToArray());
					return ((Expression<Func<CType>>)Expression.Lambda(callLambdaExpression));
				} else {
					var arguments = CompileArgumentListExprs(MyConstructor.GetParameters().Select(v => v.ParameterType));
					Expression createInstanceExpression = Expression.New(MyConstructor, arguments);
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

			private void AddPropertySetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) {
				var fields = from kv in fieldInjectors 
					select new { pfExpr = Expression.Field (instanceVar, kv.Key) as MemberExpression, setter = kv.Value };
				var props = from kv in propertyInjectors
					select new { pfExpr = Expression.Property (instanceVar, kv.Key) as MemberExpression, setter = kv.Value };

				foreach (var pf in fields.Union(props))
				{
					var valueExpr = pf.setter.IsResolve() ? 
						GetResolverInvocationExpressionForType(pf.setter.MemberType) : pf.setter.Setter.Body;
					var propOrFieldExpr = Expression.Assign (pf.pfExpr, valueExpr);
					blockExpressions.Add (propOrFieldExpr);
				}
			}

			private Expression GetResolverInvocationExpressionForType(Type parameterType) {
				return injector.ResolveResolver (parameterType).GetResolveExpression(dependencies);
			}

			private class SetterExpression
			{
				public bool IsResolve() {
					return Setter == null;
				}

				public LambdaExpression Setter { get; set; }
				public MemberInfo Info { get; set; }
				public Type MemberType { get; set; }
			}
		}

		protected internal class InjectorBinding<CType> : IInjectorBinding<CType>
			where CType : class 
		{ 
			private readonly Resolver<CType> resolver;

			protected internal InjectorBinding(Resolver<CType> resolver) {
				this.resolver = resolver;
			}

			public IInjectorBinding<CType> SetFactoryLambda (LambdaExpression factoryExpression) 
			{
				resolver.SetFactory (factoryExpression);
				return this;
			}

			public IInjectorBinding<CType> AddPropertyInjector<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				return AddPropertyInjectorInner (propertyExpression, null);
			}

			public IInjectorBinding<CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				return AddPropertyInjectorInner (propertyExpression, setter);
			}

			private IInjectorBinding<CType> AddPropertyInjectorInner<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter) {
				var propertyMemberExpression = propertyExpression.Body as MemberExpression;
				if (propertyMemberExpression == null) {
					throw InjectorErrors.ErrorMustContainMemberExpression.FormatEx("propertyExpression");
				}

				var member = propertyMemberExpression.Member;
				resolver.AddMethodInfoSetter (member, setter);

				return this;
			}

			public IInjectorBinding<CType> AsSingleton (bool singlton = true) {
				resolver.AsSingleton (singlton);
				return this;
			}
		}

		/// <summary>
		/// Thread safe dictionary wrapper. 
		/// </summary>
		protected internal class SafeDictionary<TKey,TValue> {
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

			public bool ContainsKey(TKey key) {
				lock (syncLock) {
					return dict.ContainsKey(key);
				}
			}

			public IEnumerable<TValue> Values {
				get {
					lock (syncLock) {
						// use unsync since copy on write
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

		////////////////////////////////////////////////////////////////////////////////
		#region ImplicitBindingsHelpers

		protected internal static void SetupImplicitPropResolvers<CType>(Resolver<CType> resolver) where CType : class {
			Type typeT = typeof(CType);
			do {
				var props = from p in typeT.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
							select new { Info = p as MemberInfo, MemType = p.PropertyType };
				var fields = from f in typeT.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
							 select new { Info = f as MemberInfo, MemType = f.FieldType };

				var propsAndFields = props.Union(fields)
						.Where(pf => pf.Info.GetCustomAttributes(typeof(InjectAttribute), true).Length != 0)
						.Where (pf => pf.Info.DeclaringType == typeT);

				foreach (var pf in propsAndFields) {
					InvokeSetupImplicitPropResolvers<CType> (pf.Info, pf.MemType, pf.Info.Name, resolver);
				}
			} while ((typeT = typeT.BaseType) != null && typeT != typeof(object));

			// setup singleton
			if (typeof(CType).GetCustomAttributes (typeof(SingletonAttribute), false).Length > 0) {
				resolver.AsSingleton (true);
			}
		}

		private static void InvokeSetupImplicitPropResolvers<CType>(MemberInfo memberInfo, Type memberType, string memberName, Resolver<CType> resolver) 
			where CType : class 
		{
			if (!memberType.IsClass && !memberType.IsInterface) {
				throw InjectorErrors.ErrorUnableToBindNonClassFieldsProperties.FormatEx(memberName, typeof(CType).FullName);
			}

			resolver.AddMethodInfoSetter (memberInfo, null);
		}

		#endregion // ImplicitBindingsHelpers
		////////////////////////////////////////////////////////////////////////////////

		/// <summary>
		/// Gets the implicit types.
		/// </summary>
		/// <returns>The implicit types.</returns>
		/// <param name="boundType">Bound type.</param>
		protected internal static ISet<Type> GetImplicitTypes(Type boundType) {
			var implicitTypes = new HashSet<Type>();

			foreach (Type iFace in boundType.GetInterfaces()) {
				implicitTypes.Add(iFace);
			}

			Type wTypeChain = boundType;
			while ((wTypeChain = wTypeChain.BaseType) != null && wTypeChain != typeof(object)) {
				implicitTypes.Add(wTypeChain);
			}

			return implicitTypes;
		}

	}
}
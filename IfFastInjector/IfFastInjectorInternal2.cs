using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IfFastInjector.IfInjectorTypes;

namespace IfFastInjector
{
	internal abstract class IfFastInjectorInternal2
	{
		protected internal abstract class BaseRegistration {
			protected readonly object SyncLock;
			public readonly Type KeyType;

			protected BaseRegistration(Type keyType, object syncLock) {
				this.KeyType = keyType;
				this.SyncLock = syncLock;
			}

			public bool IsSingleton { get; set; }

			public abstract object DoResolve ();
			public abstract Expression GetResolveExpr<T> () where T : class;
			public abstract void ClearResolver();
		}

		protected internal class InjectorTypeConstruct {
			public BaseRegistration InternalResolver { get; set; }
			public bool IsRecursionTestPending { get; set; }
			public bool IsInternalResolverPending { get; set; }

			public ISet<Type> ImplicitTypes { get; private set; }

			public InjectorTypeConstruct() {
				IsInternalResolverPending = true;
			}
		}

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

		/// <summary>
		/// The actual injector implementation.
		/// </summary>
		internal class InjectorInternal : IfInjector
		{		
			// Thread safety via lock (internalResolvers) 
			private readonly object syncLock = new object();
			private readonly SafeDictionary<Type, InjectorTypeConstruct> allTypeConstructs;
			private readonly SafeDictionary<Type, ISet<Type>> implicitTypeLookup;

			protected internal readonly MethodInfo GenericResolve;
			private readonly MethodInfo genericResolveClosure;

			public InjectorInternal() 
			{
				// Init dictionaries
				allTypeConstructs = new SafeDictionary<Type, InjectorTypeConstruct>(syncLock);
				implicitTypeLookup = new SafeDictionary<Type, ISet<Type>> (syncLock);

				Expression<Func<Exception>> TmpResolveExpression = () => this.Resolve<Exception>();
				this.GenericResolve = ((MethodCallExpression)TmpResolveExpression.Body).Method.GetGenericMethodDefinition();

				Expression<Func<Expression>> TmpResolveClosureExpression = () => this.ResolveExpression<Exception>();
				this.genericResolveClosure = ((MethodCallExpression)TmpResolveClosureExpression.Body).Method.GetGenericMethodDefinition();
			}

			public override object Resolve(Type type)
			{
				return ResolveResolver (type).DoResolve ();
			}

			public override T InjectProperties<T> (T instance)
			{
				return ((Registration<T>)ResolveResolver (typeof(T))).DoInject (instance);
			}

			protected internal BaseRegistration ResolveResolver(Type type)
			{
				ISet<Type> lookup;
				InjectorTypeConstruct typeInfo;

				if (allTypeConstructs.UnsyncedTryGetValue (type, out typeInfo) && typeInfo.InternalResolver != null) {
					return typeInfo.InternalResolver;
				} else if (implicitTypeLookup.UnsyncedTryGetValue (type, out lookup) && lookup.Count > 0) {
					if (lookup.Count == 1) {
						return ResolveResolver (lookup.First());
					} else {
						throw new IfFastInjectorException (string.Format(IfFastInjectorErrors.ErrorAmbiguousBinding, type.Name));
					}
				} else {
					return BindImplicit (type);
				}
			}

			protected internal Expression ResolveExpression(Type type) {
				return (Expression) genericResolveClosure.MakeGenericMethod(type).Invoke(this, new object[0]);
			}

			private Expression ResolveExpression<T>() where T : class {
				return ResolveResolver (typeof(T)).GetResolveExpr<T>();
			}

			public override IfFastInjectorBinding<TConcreteType> Bind<T, TConcreteType>()
			{
				var iResolver = BindExplicit<T, TConcreteType> ();
				return new InjectorFluent<TConcreteType>(iResolver);
			}

			public override IfInjectorTypes.IfFastInjectorBinding<CT> Bind<T,CT> (Expression<Func<CT>> factoryExpression)
			{
				var iResolver = BindExplicit<T, CT> (factoryExpression);
				return new InjectorFluent<CT>(iResolver);
			}

			private Registration<CType> BindExplicit<BType, CType>(Expression<Func<CType>> factoryExpression = null)
				where BType : class
				where CType : class, BType
			{
				lock (syncLock) {
					Type bindType = typeof(BType);
					InjectorTypeConstruct typeConstruct = new InjectorTypeConstruct ();

					implicitTypeLookup.Remove (bindType);
					allTypeConstructs.Remove (bindType);
					allTypeConstructs.Add (bindType, typeConstruct);
					AddImplicitTypes (bindType, GetImplicitTypes(bindType));

					CreateInternalResolverInstance (bindType, typeof(CType), typeConstruct, factoryExpression);

					ResetResolveClosures (bindType);

					return (Registration<CType>) typeConstruct.InternalResolver;
				}
			}

			private BaseRegistration BindImplicit(Type bindType) {
				lock (syncLock) {
					InjectorTypeConstruct typeConstruct;
					if (allTypeConstructs.TryGetValue (bindType, out typeConstruct)) {
						if (typeConstruct.IsInternalResolverPending) {
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, bindType.Name));
						}
						return typeConstruct.InternalResolver;
					}

					typeConstruct = new InjectorTypeConstruct ();
					allTypeConstructs.Add (bindType, typeConstruct);

					// Handle implementedBy
					var implType = GetIfImplementedBy (bindType);
					if (implType != null) {
						CreateInternalResolverInstance (bindType, implType, typeConstruct);
					} else {
						CreateInternalResolverInstance (bindType, bindType, typeConstruct);
					}

					ResetResolveClosures (bindType);

					return typeConstruct.InternalResolver;
				}
			}

			protected internal Type GetIfImplementedBy(Type type) {
				var implTypeAttrs = type.GetCustomAttributes(typeof(IfImplementedByAttribute), false);
				if (implTypeAttrs.Length > 0) {
					return (implTypeAttrs[0] as IfImplementedByAttribute).Implementor;
				}
				return null;
			}

			private void CreateInternalResolverInstance(Type keyType, Type implType, InjectorTypeConstruct typeConstruct, Expression factoryExpression = null) {
				try {
					Type iResolverType = typeof(Registration<>);
					Type genericType = iResolverType.MakeGenericType(new Type[] { implType });
					if (factoryExpression == null) {
						typeConstruct.InternalResolver = (BaseRegistration) Activator.CreateInstance(genericType, this, typeConstruct, keyType, syncLock);
					} else {
						typeConstruct.InternalResolver = (BaseRegistration) Activator.CreateInstance(genericType, this, typeConstruct, keyType, syncLock, factoryExpression);
					}
					typeConstruct.IsInternalResolverPending = false;

					SetupImplicitPropResolvers (typeConstruct.InternalResolver, implType);
				} catch (TargetInvocationException ex) {
					throw ex.InnerException;
				}
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

			private void ResetResolveClosures(Type bindType) {
				lock (syncLock) {
					// TODO
				}
			}
		}

		protected internal class Registration<T> : BaseRegistration 
			where T : class 
		{
			private readonly Type typeofT = typeof(T);

			private readonly Dictionary<PropertyInfo, SetterExpression> propertyInjectors;
			private readonly Dictionary<FieldInfo, SetterExpression> fieldInjectors;

			private Expression<Func<T>> ResolverFactoryExpression { get; set; }
			private ConstructorInfo MyConstructor { get; set; }

			private bool isVerifiedNotRecursive;

			private Expression<Func<T>> resolverExpression;
			private Func<T> resolverExpressionCompiled;

			private Func<T> resolve;
			private Action<T> resolveProperties;

			private readonly InjectorInternal injector;
			private readonly InjectorTypeConstruct typeConstruct;

			public Registration(InjectorInternal injector, InjectorTypeConstruct typeConstruct, Type keyType, object syncLock) : 
				base(keyType, syncLock) 
			{
				this.injector = injector;
				this.typeConstruct = typeConstruct;

				this.propertyInjectors = new Dictionary<PropertyInfo, SetterExpression>();
				this.fieldInjectors = new Dictionary<FieldInfo, SetterExpression>();

				InitInitialResolver();
			}

			public Registration(InjectorInternal injector, InjectorTypeConstruct typeConstruct, Type keyType, object syncLock, Expression<Func<T>> factoryExpression) : 
				this(injector, typeConstruct, keyType, syncLock) 
			{
				ResolverFactoryExpression = factoryExpression;
			}

			public override object DoResolve() {
				return DoResolveTyped ();
			}

			private T DoResolveTyped() {
				if (!IsResolved()) {
					CompileResolver ();
				}

				return resolve ();
			}

			public T DoInject(T instance) {
				if (instance != null) {
					if (!IsResolved()) {
						CompileResolver ();
					}

					resolveProperties (instance);
				}

				return instance;
			}

			public override Expression GetResolveExpr<ST> () {
				lock (SyncLock) {
					if (IsSingleton) {
						var instance = DoResolveTyped ();
						Expression<Func<T>> expr = () => instance;
						return expr.Body;
					} else {
						if (!isVerifiedNotRecursive) {
							DoResolveTyped ();
						}
						return resolverExpression.Body;
					}
				}
			}

			private void InitInitialResolver()
			{
				if (typeofT.IsInterface || typeofT.IsAbstract)
				{
					// if we can not instantiate, set the resolver to throw an exception.
					ResolverFactoryExpression = () => ThrowInterfaceException();
				}
				else
				{
					// try to find the default constructor and create a default resolver from it
					SetDefaultConstructor();
				}
			}

			private T ThrowInterfaceException()
			{
				throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorUnableToResultInterface, typeof(T).FullName));
			}

			public void AddPropertySetter(PropertyInfo propertyInfo, LambdaExpression setter)
			{
				lock (SyncLock) {
					propertyInjectors [propertyInfo] = new SetterExpression { Info = propertyInfo, MemberType = propertyInfo.PropertyType, Setter = setter };
					ClearResolver ();
				}
			}

			public void AddFieldSetter(FieldInfo fieldInfo, LambdaExpression setter)
			{
				lock (SyncLock) {
					fieldInjectors [fieldInfo] = new SetterExpression { Info = fieldInfo, MemberType = fieldInfo.FieldType, Setter = setter };
					ClearResolver ();
				}
			}

			public void AsSingleton(bool singleton) {
				lock (SyncLock) {
					this.IsSingleton = singleton;
					ClearResolver ();
				}
			}

			public override void ClearResolver() {
				lock (SyncLock) {
					this.IsRecursionTestPending = false;
					this.isVerifiedNotRecursive = false;

					this.resolve = null;
					this.resolverExpressionCompiled = null;
					this.resolverExpression = null;
				}
			}

			private bool IsRecursionTestPending {
				get {
					return typeConstruct.IsRecursionTestPending;
				}
				set {
					typeConstruct.IsRecursionTestPending = value;
				}
			}

			private T ResolveWithRecursionCheck()
			{
				// Lock until executed once; we will compile this away once verified
				lock (SyncLock) {
					if (!isVerifiedNotRecursive) {
						if (IsRecursionTestPending) {
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, typeofT.Name));
						}
						IsRecursionTestPending = true;
					}

					T retval = resolverExpressionCompiled();

					isVerifiedNotRecursive = true;
					IsRecursionTestPending = false;

					if (this.IsSingleton) {
						resolve = () => retval;
					} else {
						resolve = resolverExpressionCompiled;
					}
					return retval;
				}
			}

			/// <summary>
			/// Get the constructor with the fewest number of parameters and create a factory for it
			/// </summary>
			private void SetDefaultConstructor()
			{
				// get first available constructor ordered by parameter count ascending
				var constructor = typeofT.GetConstructors().Where(v => Attribute.IsDefined(v, typeof(IfIgnoreConstructorAttribute)) == false).OrderBy(v => Attribute.IsDefined(v, typeof(IfInjectAttribute)) ? 0 : 1).ThenBy(v => v.GetParameters().Count()).FirstOrDefault();

				if (constructor != null) {
					MyConstructor = constructor;
				}
			}

			private void CompileResolver() {
				lock (SyncLock) {
					if (!IsResolved()) {
						// START: Handle compile loop
						if (IsRecursionTestPending) {
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, typeofT.Name));
						}
						IsRecursionTestPending = true; 

						var constructorExpr = CompileConstructorExpr ();

						if (fieldInjectors.Any() || propertyInjectors.Any()) {
							var instanceVar = Expression.Variable(typeofT);
							var assignExpression = Expression.Assign(instanceVar, constructorExpr.Body);

							var blockExpression = new List<Expression>();
							blockExpression.Add(assignExpression);

							// setters
							AddPropertySetterExpressions (instanceVar, blockExpression);

							// return value + Func<T>
							blockExpression.Add(instanceVar); 

							var expressionFunc = Expression.Block(new ParameterExpression[] { instanceVar }, blockExpression);
							resolverExpression = (Expression<Func<T>>)Expression.Lambda(expressionFunc, constructorExpr.Parameters);

						} else {
							resolverExpression = constructorExpr;
						}

						resolverExpressionCompiled = resolverExpression.Compile ();
						resolve = ResolveWithRecursionCheck;
						resolveProperties = CompilePropertiesResolver ();

						IsRecursionTestPending = false; // END: Handle compile loop
					}
				}
			}

			private bool IsResolved() {
				return resolve != null;
			}

			public Action<T> CompilePropertiesResolver() {
				lock (SyncLock) {
					if (fieldInjectors.Any() || propertyInjectors.Any()) {
						var instance = Expression.Parameter (typeof(T), "instance");
						var instanceVar = Expression.Variable(typeofT);

						var assignExpression = Expression.Assign(instanceVar, instance);

						var blockExpression = new List<Expression> ();
						blockExpression.Add(assignExpression);
						AddPropertySetterExpressions (instanceVar, blockExpression);

						var expression = Expression.Block(new [] { instanceVar }, blockExpression);

						return Expression.Lambda<Action<T>>(expression, instance).Compile();
					} else {
						return (T x) => {};
					}
				}
			}

			private Expression<Func<T>> CompileConstructorExpr()
			{
				if (ResolverFactoryExpression != null) {
					var visitor = new ReplaceMethodCallWithInvocationExpressionVisitor<T>(this);
					var newFactoryExpression = (Expression<Func<T>>)visitor.Visit(ResolverFactoryExpression);

					return ResolverFactoryExpression;
				} else {
					var methodParameters = MyConstructor.GetParameters().Select(v => v.ParameterType).ToArray();
					var arguments = new List<Expression>();

					foreach (var parameterType in methodParameters) {
						var argument = GetResolverInvocationExpressionForType(parameterType);
						arguments.Add(argument);
					}

					Expression createInstanceExpression = Expression.New(MyConstructor, arguments);
					return ((Expression<Func<T>>)Expression.Lambda(createInstanceExpression));
				}
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
				return injector.ResolveExpression (parameterType);
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

			private class ReplaceMethodCallWithInvocationExpressionVisitor<RMT> : ExpressionVisitor
				where RMT : class
			{
				private readonly Registration<RMT> resolver;
				private readonly MethodInfo resolverMethod;

				public ReplaceMethodCallWithInvocationExpressionVisitor(Registration<RMT> resolver)
				{
					this.resolver = resolver;
					this.resolverMethod = resolver.injector.GenericResolve;
				}

				protected override Expression VisitMethodCall(MethodCallExpression node)
				{
					// TODO - fix
					var method = node.Method;
					if (method.IsGenericMethod && method.GetGenericMethodDefinition() == resolverMethod) {
						var parameterType = method.GetGenericArguments()[0];
						return resolver.GetResolverInvocationExpressionForType(parameterType);
					}
					else {
						return base.VisitMethodCall(node);
					}
				}
			}
		}

		protected internal class InjectorFluent<T> : IfFastInjectorBinding<T>
			where T : class 
		{ 
			private readonly Registration<T> resolver;

			protected internal InjectorFluent(Registration<T> resolver) {
				this.resolver = resolver;
			}

			public IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				return AddPropertyInjectorInner (propertyExpression, null);
			}

			public IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				return AddPropertyInjectorInner (propertyExpression, setter);
			}

			private IfFastInjectorBinding<T> AddPropertyInjectorInner<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter) {
				var propertyMemberExpression = propertyExpression.Body as MemberExpression;
				if (propertyMemberExpression == null) {
					throw new ArgumentException(IfFastInjectorErrors.ErrorMustContainMemberExpression, "propertyExpression");
				}

				var member = propertyMemberExpression.Member;
				if (member is PropertyInfo) {
					resolver.AddPropertySetter (member as PropertyInfo, setter);
				} else if (member is FieldInfo) {
					resolver.AddFieldSetter (member as FieldInfo, setter);
				} else {
					// TODO
					throw new IfFastInjectorException ();
				}

				return this;
			}

			public IfFastInjectorBinding<T> AddMethodInjector<TPropertyType> (Expression<Action<T, TPropertyType>> methodExpression)
				where TPropertyType : class
			{
				// REMOVE
				return this;
			}

			public IfFastInjectorBinding<T> AddMethodInjector<TPropertyType> (Expression<Action<T, TPropertyType>> methodExpression, Expression<Func<TPropertyType>> setter)
			{
				// REMOVE
				return this;
			}

			public IfFastInjectorBinding<T> AsSingleton (bool singlton = true) {
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

			public bool TryGetValue(TKey key, out TValue value) 
			{
				lock (syncLock) {
					return dict.TryGetValue (key, out value);
				}
			}

			public bool UnsyncedTryGetValue(TKey key, out TValue value)
			{
				return unsyncDict.TryGetValue (key, out value);
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

		private static readonly MethodInfo GenericSetupImplicitPropResolvers;

		static IfFastInjectorInternal2() {
			Expression<Action<Registration<Exception>>> TmpBindingExpression = (r) => SetupImplicitPropResolvers<Exception>(r);
			GenericSetupImplicitPropResolvers = ((MethodCallExpression)TmpBindingExpression.Body).Method.GetGenericMethodDefinition();
		}			

		protected internal static void SetupImplicitPropResolvers(BaseRegistration resolver, Type implType) {
			GenericSetupImplicitPropResolvers.MakeGenericMethod (implType).Invoke (null, new object[]{resolver});
		}

		protected internal static void SetupImplicitPropResolvers<T>(Registration<T> resolver) where T : class {
			Type typeT = typeof(T);
			do {
				var propsAndFields = 
					(from p in typeT.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
				     select new { Info = p as MemberInfo, MemType = p.PropertyType })
						.Union(from p in typeT.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						  	   select new { Info = p as MemberInfo, MemType = p.PropertyType })
						.Where(pf => pf.Info.GetCustomAttributes(typeof(IfInjectAttribute), true).Length != 0)
						.Where (pf => pf.Info.DeclaringType == typeT);

				foreach (var pf in propsAndFields) {
					InvokeSetupImplicitPropResolvers<T> (pf.Info, pf.MemType, pf.Info.Name, resolver);
				}
			} while ((typeT = typeT.BaseType) != null && typeT != typeof(object));

			// setup singleton
			if (typeof(T).GetCustomAttributes (typeof(IfSingletonAttribute), false).Length > 0) {
				resolver.AsSingleton (true);
			}
		}

		private static void InvokeSetupImplicitPropResolvers<T>(MemberInfo memberInfo, Type memberType, string memberName, Registration<T> resolver) 
			where T : class 
		{
			if (!memberType.IsClass && !memberType.IsInterface) {
				throw new IfFastInjectorException (string.Format(IfFastInjectorErrors.ErrorUnableToBindNonClassFieldsProperties, memberName, typeof(T).Name));
			}

			if (memberInfo is PropertyInfo) {
				resolver.AddPropertySetter (memberInfo as PropertyInfo, null);
			} else {
				resolver.AddFieldSetter (memberInfo as FieldInfo, null);
			}
		}

		#endregion // ImplicitBindingsHelpers
		////////////////////////////////////////////////////////////////////////////////
	}
}
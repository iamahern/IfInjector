using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IfFastInjector.IfInjectorTypes;

/// <summary>
/// NOTE: no actual implementation
/// I was was coding these interfaces before rebasing on top of 'fFastInjector' to use as a starting point.
/// 
/// I intend to merge the good parts of this with IFFastInjector
/// </summary>
namespace IfFastInjector
{
	/// <summary>
	/// Static encapsulation class to restict external access of components.
	/// </summary>
	internal abstract class IfFastInjectorInternal 
	{
		/// <summary>Internal resolver type.</summary>
		protected internal interface IInternalResolver {
			object DoResolve();

			Func<T> GetResolveFunc<T> () where T : class;
		}

		/// <summary>Utility to allow for locking granularity at the type construct level. Also simplifies map management.</summary>
		protected internal class InjectorTypeConstructs {
			public IInternalResolver InternalResolver { get; set; }
			public bool IsRecursionTestPending { get; set; }
			public bool IsInternalResolverPending { get; set; }

			public ISet<Type> ImplicitTypes { get; private set; }

			public InjectorTypeConstructs() {
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
			private readonly SafeDictionary<Type, InjectorTypeConstructs> allTypeConstructs;
			private readonly SafeDictionary<Type, ISet<Type>> implicitTypeLookup;
			private readonly Dictionary<Type, IDisposable> typeResolveClosures;

			protected internal readonly MethodInfo GenericResolve;
			private readonly MethodInfo genericResolveClosure;

			public InjectorInternal() 
			{
				// Init dictionaries
				allTypeConstructs = new SafeDictionary<Type, InjectorTypeConstructs>(syncLock);
				implicitTypeLookup = new SafeDictionary<Type, ISet<Type>> (syncLock);
				typeResolveClosures = new Dictionary<Type, IDisposable>();

				Expression<Func<Exception>> TmpResolveExpression = () => this.Resolve<Exception>();
				this.GenericResolve = ((MethodCallExpression)TmpResolveExpression.Body).Method.GetGenericMethodDefinition();

				Expression<Func<Expression>> TmpResolveClosureExpression = () => this.ResolveClosure<Exception>();
				this.genericResolveClosure = ((MethodCallExpression)TmpResolveClosureExpression.Body).Method.GetGenericMethodDefinition();
			}

			public override object Resolve(Type type)
			{
				return ResolveResolver (type).DoResolve ();
			}

			public override T InjectProperties<T> (T instance)
			{
				return ((InternalResolver<T>)ResolveResolver (typeof(T))).DoInject (instance);
			}

			protected IInternalResolver ResolveResolver(Type type)
			{
				ISet<Type> lookup;
				InjectorTypeConstructs typeInfo;

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

			protected internal Expression ResolveClosure(Type type) {
				return (Expression) genericResolveClosure.MakeGenericMethod(type).Invoke(this, new object[0]);
			}

			private Expression ResolveClosure<T>() where T : class {
				var resolveFactory = ResolveFactoryClosure<T> ();
				return resolveFactory.Body;
			}

			protected internal Expression<Func<T>> ResolveFactoryClosure<T>() where T : class {
				lock (syncLock) {
					TypeResolverClosure<T> ptResolver;
					IDisposable cachedResolver;

					if (!typeResolveClosures.TryGetValue (typeof(T), out cachedResolver)) {
						cachedResolver = ptResolver = new TypeResolverClosure<T> (this);
						typeResolveClosures.Add (typeof(T), ptResolver);
					} else {
						ptResolver = cachedResolver as TypeResolverClosure<T>;
					}
						
					Expression<Func<T>> resolver = () => ptResolver.DoResolve();
					return resolver;
				}
			}

			public override IfFastInjectorBinding<TConcreteType> Bind<T, TConcreteType>()
			{
				var iResolver = BindExplicit<T, TConcreteType> ();
				return new InjectorFluent<TConcreteType>(iResolver);
			}

			public override IfInjectorTypes.IfFastInjectorBinding<CT> Bind<T,CT> (Expression<Func<CT>> factoryExpression)
			{
				var iResolver = BindExplicit<T, CT> ();
				iResolver.SetResolver(factoryExpression);
				return new InjectorFluent<CT>(iResolver);
			}

			private InternalResolver<CType> BindExplicit<BType, CType>()
				where BType : class
				where CType : class, BType
			{
				lock (syncLock) {
					Type bindType = typeof(BType);
					InjectorTypeConstructs typeConstruct = new InjectorTypeConstructs ();
					
					implicitTypeLookup.Remove (bindType);
					allTypeConstructs.Remove (bindType);
					allTypeConstructs.Add (bindType, typeConstruct);
					AddImplicitTypes (bindType, GetImplicitTypes(bindType));
					
					CreateInternalResolverInstance (typeof(CType), typeConstruct);

					ResetTypeResolveClosures ();

					return (InternalResolver<CType>) typeConstruct.InternalResolver;
				}
			}

			private IInternalResolver BindImplicit(Type bindType) {
				lock (syncLock) {
					InjectorTypeConstructs typeConstruct;
					if (allTypeConstructs.TryGetValue (bindType, out typeConstruct)) {
						if (typeConstruct.IsInternalResolverPending) {
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, bindType.Name));
						}
						return typeConstruct.InternalResolver;
					}

					typeConstruct = new InjectorTypeConstructs ();
					allTypeConstructs.Add (bindType, typeConstruct);

					// Handle implementedBy
					var implType = GetIfImplementedBy (bindType);
					if (implType != null) {
						CreateInternalResolverInstance (implType, typeConstruct);
					} else {
						CreateInternalResolverInstance (bindType, typeConstruct);
					}

					ResetTypeResolveClosures ();

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

			private void CreateInternalResolverInstance(Type implType, InjectorTypeConstructs typeConstruct) {
				try {
					Type iResolverType = typeof(InternalResolver<>);
					Type genericType = iResolverType.MakeGenericType(new Type[] { implType });
					typeConstruct.InternalResolver = (IInternalResolver) Activator.CreateInstance(genericType, this, typeConstruct, syncLock);
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

			private void ResetTypeResolveClosures () {
				lock (syncLock) {
					foreach (var resolveClosure in typeResolveClosures.Values) {
						resolveClosure.Dispose ();
					}
				}
			}

			private class TypeResolverClosure<T> : IDisposable where T : class {
				private readonly InjectorInternal injector;
				private Func<T> resolver;

				public TypeResolverClosure(InjectorInternal injector) {
					this.injector = injector;
				}

				public T DoResolve() {
					if (resolver == null) {
						resolver = injector.ResolveResolver (typeof(T)).GetResolveFunc<T>();
					}

					return resolver();
				}

				public void Dispose() {
					resolver = null;
				}
			}
		}

		/// <summary>
		/// The Injector Internal.
		/// </summary>
		protected internal class InternalResolver<T> : IInternalResolver
			where T : class
		{
			private bool isVerifiedNotRecursive;

			private readonly object syncLock;
			private readonly Type typeofT = typeof(T);
			private readonly List<SetterExpression> setterExpressions = new List<SetterExpression>();
			private Func<T> resolverFactoryCompiled;
			private Func<T> resolve; // TODO - Ok as not locked?
			private Action<T> resolveProperties;

			private bool singleton = false;

			private readonly InjectorInternal injector;
			private readonly InjectorTypeConstructs typeConstructs;

			public InternalResolver(InjectorInternal injector, InjectorTypeConstructs typeConstructs, object syncLock) {
				this.injector = injector;
				this.typeConstructs = typeConstructs;
				this.syncLock = syncLock;
				InitInitialResolver();
			}

			public object DoResolve() {
				return resolve ();
			}

			public T DoInject(T instance) {
				if (instance != null) {
					resolveProperties (instance);
				}

				return instance;
			}

			public Func<ST> GetResolveFunc<ST> () where ST : class {
				lock (syncLock) {
					resolve ();
					return resolve as Func<ST>;
				}
			}

			private void InitInitialResolver()
			{
				if (typeofT.IsInterface || typeofT.IsAbstract)
				{
					// if we can not instantiate, set the resolver to throw an exception.
					// this resolver will be replaced when the type is configured
					SetResolverInner(() => ThrowInterfaceException());
				}
				else
				{
					// try to find the default constructor and create a default resolver from it
					SetDefaultConstructor();
				}
			}

			/// <summary>
			/// Return an InvocationExpression for Resolver of type T
			/// </summary>
			/// <returns></returns>
			private Expression GetResolverInvocationExpression()
			{
				Expression<Func<T>> expressionForResolverLambda = () => resolve();
				return (Expression)expressionForResolverLambda.Body;
			}

			/// <summary>
			/// Return an InvocationExpression for Resolver of type parameterType
			/// </summary>
			/// <param name="parameterType"></param>
			/// <returns></returns>
			private Expression GetResolverInvocationExpressionForType(Type parameterType)
			{
				return injector.ResolveClosure (parameterType);
			}

			private T ThrowInterfaceException()
			{
				throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorUnableToResultInterface, typeof(T).FullName));
			}

			private class SetterExpression
			{
				public MemberExpression PropertyMemberExpression { get; set; }
				public MethodCallExpression SetterMethodExpression { get; set; }
				public LambdaExpression Setter { get; set; }
			}

			/// <summary>
			/// Expression to construct new instance of class
			/// </summary>
			private Expression<Func<T>> ResolverFactoryExpression { get; set; }

			/// <summary>
			/// Expression to construct new instance of class and set members or other operations
			/// </summary>
			private Expression<Func<T>> ResolverExpression { get; set; }

			/// <summary>
			/// Sets the resolver.
			/// </summary>
			/// <param name="factoryExpression">Factory expression.</param>
			public void SetResolver(Expression<Func<T>> factoryExpression)
			{
				lock (syncLock) {
					var visitor = new ReplaceMethodCallWithInvocationExpressionVisitor<T>(this);
					var newFactoryExpression = (Expression<Func<T>>)visitor.Visit(factoryExpression);
					SetResolverInner(newFactoryExpression);
				}
			}

			/// <summary>
			/// Sets the resolver.
			/// </summary>
			/// <param name="constructor">Constructor.</param>
			public void SetResolver(ConstructorInfo constructor)
			{
				lock (syncLock) {
					SetConstructor(constructor);
				}
			}

			private void SetResolverInner(Expression<Func<T>> factoryExpression)
			{
				ResolverFactoryExpression = factoryExpression;
				CompileResolver();
			}

			public void AddPropertySetter<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				AddPropertySetter<TPropertyType>(propertyExpression, injector.ResolveFactoryClosure<TPropertyType>());
			}

			public void AddPropertySetter<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				lock (syncLock) {
					AddPropertySetterInner<TPropertyType>(propertyExpression, setter);
				}
			}

			private void AddPropertySetterInner<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				var propertyMemberExpression = propertyExpression.Body as MemberExpression;
				if (propertyMemberExpression == null) {
					throw new ArgumentException(IfFastInjectorErrors.ErrorMustContainMemberExpression, "propertyExpression");
				}

				setterExpressions.Add(new SetterExpression { PropertyMemberExpression = propertyMemberExpression, Setter = setter });

				CompileResolver();
			}

			public void AddMethodInjector<TPropertyType> (Expression<Action<T, TPropertyType>> methodExpression)
				where TPropertyType : class
			{
				AddMethodInjector (methodExpression, injector.ResolveFactoryClosure<TPropertyType>());
			}

			public void AddMethodInjector<TPropertyType> (Expression<Action<T, TPropertyType>> methodExpression, Expression<Func<TPropertyType>> setter)
			{
				lock (syncLock) {
					AddMethodSetterInner (methodExpression, setter);
				}
			}

			private void AddMethodSetterInner<TPropertyType>(Expression<Action<T, TPropertyType>> methodExpression, Expression<Func<TPropertyType>> setter)
			{
				var callExpr = methodExpression.Body as MethodCallExpression;
				setterExpressions.Add(new SetterExpression { SetterMethodExpression = callExpr, Setter = setter });
				CompileResolver();
			}


			public void AsSingleton(bool singleton) {
				lock (syncLock) {
					this.singleton = singleton;
					CompileResolver ();
				}
			}

			/// <summary>
			/// Compile the resolver expression
			/// If any setter expressions are used, build an expression that creates the object and then sets the properties before returning it,
			/// otherwise, use the simpler expression that just returns the object
			/// </summary>
			private void CompileResolver()
			{
				// if no property expressions, then just use the unmodified ResolverFactoryExpression
				if (setterExpressions.Any()) {
					var resolver = ResolverFactoryExpression;

					var variableExpression = Expression.Variable(typeofT);
					var assignExpression = Expression.Assign(variableExpression, resolver.Body);

					var blockExpression = new List<Expression>();
					blockExpression.Add(assignExpression);

					// setters
					AddPropertySetterExpressions (variableExpression, blockExpression);

					// return value
					blockExpression.Add(variableExpression);

					var expression = Expression.Block(new ParameterExpression[] { variableExpression }, blockExpression);

					ResolverExpression = (Expression<Func<T>>)Expression.Lambda(expression, resolver.Parameters);
				} else {
					ResolverExpression = ResolverFactoryExpression;
				}

				this.isVerifiedNotRecursive = false;

				resolverFactoryCompiled = ResolverExpression.Compile ();
				resolve = ResolveWithRecursionCheck;

				// Makes an Action that will set the properties
				CompilePropertySetters ();
			}

			private void CompilePropertySetters() {
				// if no property expressions, then just use the unmodified ResolverFactoryExpression
				if (setterExpressions.Any()) {
					var instance = Expression.Parameter (typeof(T), "instance");
					var instanceVar = Expression.Variable(typeofT);

					var assignExpression = Expression.Assign(instanceVar, instance);

					var blockExpression = new List<Expression> ();
					blockExpression.Add(assignExpression);
					AddPropertySetterExpressions (instanceVar, blockExpression);

					var expression = Expression.Block(new [] { instanceVar }, blockExpression);

					resolveProperties = Expression.Lambda<Action<T>>(expression, instance).Compile();
				} else {
					resolveProperties = (T x) => {};
				}
			}

			private void AddPropertySetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpression) {
				foreach (var v in setterExpressions)
				{
					if (v.PropertyMemberExpression != null) {
						var propertyOrFieldExpression = GetSetterExpressionBody (instanceVar, v);
						var propertyOrFieldAssignExpression = Expression.Assign (propertyOrFieldExpression, v.Setter.Body);
						blockExpression.Add (propertyOrFieldAssignExpression);
					} else {
						var methodCallExpression = Expression.Call(instanceVar, v.SetterMethodExpression.Method, v.Setter.Body);
						blockExpression.Add (methodCallExpression);
					}
				}
			}

			private MemberExpression GetSetterExpressionBody(ParameterExpression instanceVar, SetterExpression expr) {
				var member = expr.PropertyMemberExpression.Member;
				if (member is PropertyInfo) {
					// Trick to handle Property{get; private set} where the property is declared on a parent type
					var propMem = (PropertyInfo)member;
					if (propMem.DeclaringType != typeofT) {
						propMem = propMem.DeclaringType
							.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
							.Where (p => p.Name.Equals(propMem.Name))
							.First ();
					}

					return Expression.Property (instanceVar, propMem);
				} else {
					return Expression.Field(instanceVar, (FieldInfo) member);
				}
			}

			/// <summary>
			/// Convert expression of Func T to expression of Func object
			/// </summary>
			/// <param name="func"></param>
			/// <returns></returns>
			private Expression<Func<object>> ConvertFunc(Expression<Func<T>> func)
			{
				return (Expression<Func<object>>)Expression.Lambda(Expression.Convert(func.Body, typeof(object)), func.Parameters);
			}

			private bool IsRecursionTestPending {
				get {
					return typeConstructs.IsRecursionTestPending;
				}
				set {
					typeConstructs.IsRecursionTestPending = value;
				}
			}

			private T ResolveWithRecursionCheck()
			{
				// Lock until executed once; we will compile this away once verified
				lock (syncLock) {
					if (!isVerifiedNotRecursive) {
						if (IsRecursionTestPending) {
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, typeofT.Name));
						}
						IsRecursionTestPending = true;
					}

					var retval = resolverFactoryCompiled();

					isVerifiedNotRecursive = true;
					IsRecursionTestPending = false;

					if (this.singleton) {
						resolve = () => retval;
					} else {
						resolve = resolverFactoryCompiled;
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
					SetConstructor(constructor);
				}
			}

			/// <summary>
			/// Create an expression to create this type from the passed-in constructor
			/// </summary>
			/// <param name="constructor"></param>
			private void SetConstructor(ConstructorInfo constructor)
			{
				var methodParameters = constructor.GetParameters().Select(v => v.ParameterType).ToArray();

				var arguments = new List<Expression>();

				foreach (var parameterType in methodParameters) {
					var argument = GetResolverInvocationExpressionForType(parameterType);
					arguments.Add(argument);
				}

				Expression createInstanceExpression = Expression.New(constructor, arguments);

				SetResolverInner((Expression<Func<T>>)Expression.Lambda(createInstanceExpression));
			}

			private class ReplaceMethodCallWithInvocationExpressionVisitor<RMT> : ExpressionVisitor
				where RMT : class
			{
				private readonly InternalResolver<RMT> resolver;
				private readonly MethodInfo resolverMethod;

				public ReplaceMethodCallWithInvocationExpressionVisitor(InternalResolver<RMT> resolver)
				{
					this.resolver = resolver;
					this.resolverMethod = resolver.injector.GenericResolve;
				}

				protected override Expression VisitMethodCall(MethodCallExpression node)
				{
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

		/// <summary>
		/// The fluent class is really only important to give the extension methods the type for T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		protected internal class InjectorFluent<T> : IfFastInjectorBinding<T>
			where T : class 
		{ 
			private readonly InternalResolver<T> resolver;

			protected internal InjectorFluent(InternalResolver<T> resolver) {
				this.resolver = resolver;
			}

			public IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				resolver.AddPropertySetter(propertyExpression);
				return this;
			}

			public IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				resolver.AddPropertySetter(propertyExpression, setter);
				return this;
			}

			public IfFastInjectorBinding<T> AddMethodInjector<TPropertyType> (Expression<Action<T, TPropertyType>> methodExpression)
				where TPropertyType : class
			{
				resolver.AddMethodInjector (methodExpression);
				return this;
			}

			public IfFastInjectorBinding<T> AddMethodInjector<TPropertyType> (Expression<Action<T, TPropertyType>> methodExpression, Expression<Func<TPropertyType>> setter)
			{
				resolver.AddMethodInjector (methodExpression, setter);
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
		private static readonly MethodInfo GenericSetupImplicitPropResolversInternal;

		static IfFastInjectorInternal() {
			Expression<Action<InternalResolver<Exception>>> TmpBindingExpression = (r) => SetupImplicitPropResolvers<Exception>(r);
			GenericSetupImplicitPropResolvers = ((MethodCallExpression)TmpBindingExpression.Body).Method.GetGenericMethodDefinition();

			Expression<Action<InternalResolver<Exception>,ParameterExpression,MemberExpression>> TmpBindingExpressionInternal = (r, p, e) => SetupImplicitPropResolversInternal<Exception, Exception>(r, p, e);
			GenericSetupImplicitPropResolversInternal = ((MethodCallExpression)TmpBindingExpressionInternal.Body).Method.GetGenericMethodDefinition();		
		}			

		protected internal static void SetupImplicitPropResolvers(IInternalResolver resolver, Type implType) {
			GenericSetupImplicitPropResolvers.MakeGenericMethod (implType).Invoke (null, new object[]{resolver});
		}

		protected internal static void SetupImplicitPropResolvers<T>(InternalResolver<T> resolver) where T : class {
			var parameterT = Expression.Parameter(typeof(T), "x");

			Type typeT = typeof(T);
			do {
				var propsAndFields =
					(from p in typeT.GetProperties (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						select new { Expr = Expression.Property (parameterT, p), Info = p as MemberInfo, MemType = p.PropertyType })
					.Union(from f in typeT.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
						select new { Expr = Expression.Field (parameterT, f), Info = f as MemberInfo, MemType = f.FieldType })
					.Where(pf => pf.Info.GetCustomAttributes(typeof(IfInjectAttribute), true).Length != 0)
					.Where (pf => pf.Info.DeclaringType == typeT);

				foreach (var pf in propsAndFields) {
					InvokeSetupImplicitPropResolversInternal<T> (pf.MemType, pf.Info.Name, resolver, parameterT, pf.Expr);
				}
			} while ((typeT = typeT.BaseType) != null && typeT != typeof(object));

			// setup singleton
			if (typeof(T).GetCustomAttributes (typeof(IfSingletonAttribute), false).Length > 0) {
				resolver.AsSingleton (true);
			}
		}

		private static void InvokeSetupImplicitPropResolversInternal<T>(Type memberType, string memberName, InternalResolver<T> resolver, ParameterExpression objExpr, MemberExpression memExpr) 
			where T : class 
		{
			if (!memberType.IsClass && !memberType.IsInterface) {
				throw new IfFastInjectorException (string.Format(IfFastInjectorErrors.ErrorUnableToBindNonClassFieldsProperties, memberName, typeof(T).Name));
			}

			GenericSetupImplicitPropResolversInternal
				.MakeGenericMethod (typeof(T), memberType)
					.Invoke (null, new object[]{resolver, objExpr, memExpr});
		}

		private static void SetupImplicitPropResolversInternal<T, TProperty>(InternalResolver<T> resolver, ParameterExpression parameterT, MemberExpression memberExpression) 
			where T : class 
			where TProperty : class
		{
			var expr =
				Expression.Lambda<Func<T, TProperty>>(
					memberExpression,
					parameterT);

			resolver.AddPropertySetter (expr);
		}

		#endregion // ImplicitBindingsHelpers
		////////////////////////////////////////////////////////////////////////////////
	}
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IfInjector.IfCore;
using IfInjector.IfCore.IfBinding;
using IfInjector.IfCore.IfExpression;
using IfInjector.IfCore.IfLifestyle;
using IfInjector.IfCore.IfPlatform;

namespace IfInjector
{	
	/// <summary>
	/// Internal resolver interface.
	/// </summary>
	internal interface IResolver {
		/// <summary>
		/// Gets the binding config.
		/// </summary>
		/// <value>The binding config.</value>
		IBindingConfig BindingConfig { get; }

		/// <summary>
		/// Checks if the 'touched' type is in the dependency list for the current resolver. If it is, clear the resolver.
		/// </summary>
		/// <param name="touched">The type that has been modified.</param>
		void ClearResolver ();

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
		/// <param name="callerDeps">SetShim of the 'callers' dependencies. IResolver adds itself and its depdendencies to the set.</param>
		Expression GetResolveExpression (SetShim<Type> callerDeps);

		/// <summary>
		/// Gets the dependencies of this resolver.
		/// </summary>
		/// <value>The dependencies.</value>
		SetShim<Type> Dependencies { get; }
	}

	/// <summary>
	/// The actual injector implementation.
	/// </summary>
	public partial class Injector : IInjector
	{	
		private readonly object syncLock = new object();
		private readonly MethodInfo createResolverInstanceGeneric;

		private readonly SafeDictionary<Type, IResolver> allResolvers;
		private readonly SafeDictionary<Type, IResolver> allImplicitResolvers;
		private readonly SafeDictionary<Type, SetShim<Type>> implicitTypeLookup;

		private readonly Dictionary<Type, SetShim<IResolver>> resolverDeps = new Dictionary<Type, SetShim<IResolver>>();

		public Injector() 
		{
			// Init dictionaries
			allResolvers = new SafeDictionary<Type, IResolver>(syncLock);
			allImplicitResolvers = new SafeDictionary<Type, IResolver>(syncLock);
			implicitTypeLookup = new SafeDictionary<Type, SetShim<Type>> (syncLock);

			// Init resolver
			Expression<Action> tmpExpr = () => CreateResolverInstanceGeneric<Exception, Exception>(true);
			createResolverInstanceGeneric = ((MethodCallExpression)tmpExpr.Body).Method.GetGenericMethodDefinition();

			// Implicitly resolvable
			Expression<Func<Injector>> injectorFactoryExpr = () => this;
			var injectorResolver = BindExplicit<Injector, Injector>();
			injectorResolver.BindingConfig.FactoryExpression = injectorFactoryExpr;
			injectorResolver.BindingConfig.Lifestyle = Lifestyle.Singleton;
		}

		public object Resolve(Type type)
		{
			return ResolveResolver (type).DoResolve ();
		}

		public T Resolve<T>()
			where T : class
		{
			return (T)Resolve (typeof(T));
		}

		public T InjectProperties<T> (T instance, bool useExplicitBinding = false)
			where T : class
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

		internal Expression ResolveResolverExpression(Type type, SetShim<Type> callerDependencies)
		{
			return ResolveResolver (type).GetResolveExpression (callerDependencies);
		}

		private IResolver ResolveResolver(Type type)
		{
			SetShim<Type> lookup;
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

		public IInjectorBinding<CType> Bind<BType, CType>()
			where BType : class
			where CType : class, BType
		{
			CheckBindingType (typeof(BType));
			var iResolver = BindExplicit<BType, CType> ();
			return new InjectorBinding<CType>(syncLock, iResolver.BindingConfig);
		}

		public IInjectorBinding<CType> Bind<CType> ()
			where CType : class
		{
			return Bind<CType, CType> ();
		}

		public void Verify()
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
				IResolver oldResolver, implicitResolver;					

				implicitTypeLookup.Remove (bindType);
				if (allResolvers.TryGetValue (bindType, out oldResolver)) {
					allResolvers.Remove (bindType);
					if (!allImplicitResolvers.TryGetValue (bindType, out implicitResolver) || !object.ReferenceEquals (oldResolver, implicitResolver)) {
						ClearDependencies (oldResolver);
					}
				}

				// Add after create resolver
				var resolver = CreateResolverInstanceGeneric<BType, CType> (false);
				AddImplicitTypes (bindType, ImplicitTypeUtilities.GetImplicitTypes(bindType));

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

		private IResolver CreateResolverInstance(Type bindType, Type implType, bool isImplicitBinding) {
			try {
				return (IResolver) createResolverInstanceGeneric.MakeGenericMethod(bindType, implType).Invoke(this, new object[]{isImplicitBinding});
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
			
			ImplicitTypeUtilities.SetupImplicitPropResolvers<CType> (resolver.BindingConfig, syncLock);

			return resolver;
		}

		private void AddImplicitTypes(Type boundType, SetShim<Type> implicitTypes) {
			lock (syncLock) {
				foreach(Type implicitType in implicitTypes) {
					if (GetIfImplementedBy (implicitType) == null) {
						SetShim<Type> newSet, oldSet;

						if (implicitTypeLookup.TryGetValue (implicitType, out oldSet)) {
							implicitTypeLookup.Remove (implicitType);
							newSet = new SetShim<Type> (oldSet);
						} else {
							newSet = new SetShim<Type> ();
						}

						newSet.Add (boundType);
						implicitTypeLookup.Add (implicitType, newSet);
					} else {
						BindImplicit (implicitType);
					}
				}
			}
		}
		
		private void CheckBindingType(Type bindType) {
			if (typeof(Injector) == bindType) {
				throw InjectorErrors.ErrorMayNotBindInjector.FormatEx ();
			}
		}

		///////// Code to ensure 'fast' (minimal) clearing of complex resolver chains
		/// The reason for doing this, is (p
		#region ResolverDependencies

		internal void SetDependencies(IResolver resolver) {
			lock (syncLock) {
				foreach (Type t in resolver.Dependencies) {
					SetShim<IResolver> resolvers;
					if (!resolverDeps.TryGetValue (t, out resolvers)) {
						resolvers = new SetShim<IResolver> ();
						resolverDeps.Add (t, resolvers);
					}
					resolvers.Add (resolver);
				}
			}
		}

		internal void ClearDependencies(IResolver resolver) {
			lock (syncLock) {
				// ensure operating on copy to avoid modification inside of loop
				foreach (Type t in resolver.Dependencies.ToArray()) {
					SetShim<IResolver> resolvers;
					if (resolverDeps.TryGetValue (t, out resolvers)) {
						resolvers.Remove (resolver);
					}
				}
			}
		}

		/// <summary>
		/// Clears the dependent resolvers.
		/// </summary>
		/// <param name="bindType">Key type.</param>
		internal void ClearDependentResolvers(Type bindType) {
			lock (syncLock) {
				SetShim<IResolver> resolvers;
				if (resolverDeps.TryGetValue (bindType, out resolvers)) {
					foreach (var resolver in resolvers) {
						resolver.ClearResolver ();
					}
				}
			}
		}

		#endregion ResolverDependencies
		////////////

		/// <summary>
		/// Implicit type helper utilities
		/// </summary>
		private static class ImplicitTypeUtilities {
			private static readonly Type ObjectType = typeof(object);

			internal static void SetupImplicitPropResolvers<CType>(IBindingConfig bindingConfig, object syncLock) where CType : class {
				var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
				Type cType = typeof(CType); 

				do {
					foreach (var prop in FilterMemberInfo<PropertyInfo>(cType, cType.GetProperties (bindingFlags))) {
						bindingConfig.SetPropertyInfoSetter(prop, null);
					}

					foreach (var field in FilterMemberInfo<FieldInfo>(cType, cType.GetFields (bindingFlags))) {
						bindingConfig.SetFieldInfoSetter(field, null);
					}
				} while ((cType = cType.BaseType) != null && cType != ObjectType);

				if (typeof(CType).GetCustomAttributes (typeof(SingletonAttribute), false).Length > 0) {
					bindingConfig.Lifestyle =Lifestyle.Singleton;
				}
			}

			private static IEnumerable<MInfo> FilterMemberInfo<MInfo>(Type cType, IEnumerable<MInfo> propsOrFields) 
				where MInfo : MemberInfo 
			{
				return from p in propsOrFields 
					where p.GetCustomAttributes(typeof(InjectAttribute), false).Length != 0
						select p;
			}

			/// <summary>
			/// Gets the implicit types.
			/// </summary>
			/// <returns>The implicit types.</returns>
			/// <param name="boundType">Bound type.</param>
			internal static SetShim<Type> GetImplicitTypes(Type boundType) {
				var implicitTypes = new SetShim<Type>();

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

		/// <summary>
		/// Injector binding implementation.
		/// </summary>
		private class InjectorBinding<CType> : IInjectorBinding<CType>
			where CType : class 
		{ 
			private readonly object syncLock;
			private readonly IBindingConfig bindingConfig;

			internal InjectorBinding(object syncLock, IBindingConfig bindingConfig) {
				this.syncLock = syncLock;
				this.bindingConfig = bindingConfig;
			}

			public IInjectorBinding<CType> SetFactoryLambda (LambdaExpression factoryExpression) 
			{
				lock (syncLock) {
					bindingConfig.FactoryExpression = factoryExpression;
				}
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
				lock (syncLock) {
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

				return this;
			}

			public IInjectorBinding<CType> AsSingleton (bool singleton = true) {
				lock (syncLock) {
					if (singleton) {
						bindingConfig.Lifestyle = Lifestyle.Singleton;
					} else {
						bindingConfig.Lifestyle = Lifestyle.Transient;
					}
				}

				return this;
			}
		}
	}


	internal class Resolver<CType> : IResolver 
		where CType : class 
	{
		private readonly Type cType = typeof(CType);
		private readonly Type bindType;
		private readonly object syncLock;

		private readonly Injector injector;

		private readonly IBindingConfig bindingConfig;
		private readonly IExpressionCompiler<CType> expressionCompiler;

		private bool isRecursionTestPending;

		private LifestyleResolver<CType> resolver;
		private Func<CType,CType> resolveProperties;

		public SetShim<Type> Dependencies { 
			get { 
				lock (syncLock) {
					return expressionCompiler.Dependencies; 
				}
			} 
		}

		public Resolver(Injector injector, Type bindType, object syncLock)
		{
			this.injector = injector;
			this.bindType = bindType;
			this.syncLock = syncLock;

			this.bindingConfig = new BindingConfig<CType> ();
			this.expressionCompiler = new ExpressionCompiler<CType> (bindingConfig) { ResolveResolverExpression = injector.ResolveResolverExpression };

			this.bindingConfig.Changed += OnBindingChanged;
		}

		public IBindingConfig BindingConfig {
			get { return bindingConfig; }
		}

		public object DoResolve() {
			if (!IsResolved()) {
				CompileResolver ();
			}


			return resolver.Resolve ();
		}

		public void DoInject(object instance) {
			DoInjectTyped (instance as CType);
		}

		private void CompileResolver() {
			lock (syncLock) {
				if (!IsResolved()) {
					if (isRecursionTestPending) { // START: Handle compile loop
						throw InjectorErrors.ErrorResolutionRecursionDetected.FormatEx(cType.Name);
					}
					isRecursionTestPending = true; 

					var resolverExpression = expressionCompiler.CompileResolverExpression ();
					var resolverExpressionCompiled = resolverExpression.Compile ();
					var testInstance = resolverExpressionCompiled ();

					injector.SetDependencies (this);

					resolver = bindingConfig.Lifestyle.GetLifestyleResolver<CType> (syncLock, resolverExpression, resolverExpressionCompiled, testInstance);

					isRecursionTestPending = false; // END: Handle compile loop
				}
			}
		}

		private CType DoInjectTyped(CType instance) {
			if (instance != null) {
				if (resolveProperties == null) {
					lock (this) {
						resolveProperties = expressionCompiler.CompilePropertiesResolver ();
					}
				}

				resolveProperties (instance);
			}

			return instance;
		}

		private bool IsResolved() {
			return resolver != null;
		}

		private void OnBindingChanged(object sender, EventArgs e) {
			lock (syncLock) {
				injector.ClearDependentResolvers (bindType);
				ClearResolver ();
			}
		}

		public void ClearResolver() {
			lock (syncLock) {
				isRecursionTestPending = false;

				expressionCompiler.Dependencies.Clear ();
				injector.ClearDependencies (this);

				resolver = null;
			}
		}

		public Expression GetResolveExpression (SetShim<Type> callerDeps) {
			lock (syncLock) {
				if (!IsResolved()) {
					CompileResolver ();
				}

				callerDeps.UnionWith (expressionCompiler.Dependencies);
				callerDeps.Add (bindType);

				return resolver.ResolveExpression.Body;
			}
		}
	}
}
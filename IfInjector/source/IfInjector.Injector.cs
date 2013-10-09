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
	/// Resolver changed event handler.
	/// </summary>
	internal delegate void ResolverChangedEventHandler(object sender, BindingKey bindingKey);

	/// <summary>
	/// Internal resolver interface.
	/// </summary>
	internal interface IResolver {
		/// <summary>
		/// Occurs when changed.
		/// </summary>
		event ResolverChangedEventHandler Changed;

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
		Expression GetResolveExpression (SetShim<BindingKey> callerDeps);

		/// <summary>
		/// Gets the dependencies of this resolver.
		/// </summary>
		/// <value>The dependencies.</value>
		SetShim<BindingKey> Dependencies { get; }
	}

	/// <summary>
	/// The actual injector implementation.
	/// </summary>
	public partial class Injector : IInjector
	{	
		private readonly object syncLock = new object();
		private readonly MethodInfo createResolverInstanceGeneric;
		private readonly MethodInfo bindExplicitGeneric;

		private readonly SafeDictionary<BindingKey, IResolver> allResolvers;
		private readonly SafeDictionary<BindingKey, SetShim<BindingKey>> implicitTypeLookup;

		private readonly Dictionary<BindingKey, SetShim<IResolver>> resolverDeps = new Dictionary<BindingKey, SetShim<IResolver>>();

		public Injector() 
		{
			// Init dictionaries
			allResolvers = new SafeDictionary<BindingKey, IResolver>(syncLock);
			implicitTypeLookup = new SafeDictionary<BindingKey, SetShim<BindingKey>> (syncLock);

			// Init resolver
			Expression<Action> tmpExpr = () => CreateResolverInstanceGeneric<Exception, Exception>(null, null, true);
			createResolverInstanceGeneric = ((MethodCallExpression)tmpExpr.Body).Method.GetGenericMethodDefinition();

			// Init bindExplicit
			Expression<Action> tmpBindExpr = () => BindExplicit<Exception, Exception> (null, null);
			bindExplicitGeneric = ((MethodCallExpression)tmpBindExpr.Body).Method.GetGenericMethodDefinition();

			// Implicitly resolvable
			Expression<Func<Injector>> injectorFactoryExpr = () => this;
			var injectorResolver = BindExplicit<Injector, Injector>(BindingKey.Get<Injector>(), null);
			injectorResolver.BindingConfig.FactoryExpression = injectorFactoryExpr;
			injectorResolver.BindingConfig.Lifestyle = Lifestyle.Singleton;
		}

		public object Resolve(Type type)
		{
			return ResolveResolver (BindingKey.Get(type)).DoResolve ();
		}

		public T Resolve<T>()
			where T : class
		{
			return (T) ResolveResolver (BindingKey.Get<T>()).DoResolve ();
		}

		public T InjectProperties<T> (T instance)
			where T : class
		{
			var iResolver = ResolveResolver (BindingKey.GetMemberInjector<T>());
			iResolver.DoInject (instance);
			return instance;
		}

		internal Expression ResolveResolverExpression(BindingKey bindingKey, SetShim<BindingKey> callerDependencies)
		{
			return ResolveResolver (bindingKey).GetResolveExpression (callerDependencies);
		}

		private IResolver ResolveResolver(BindingKey bindingKey)
		{
			SetShim<BindingKey> lookup;
			IResolver resolver;

			if (allResolvers.UnsyncedTryGetValue (bindingKey, out resolver)) {
				return resolver;
			} else if (implicitTypeLookup.UnsyncedTryGetValue (bindingKey, out lookup) && lookup.Count > 0) {
				if (lookup.Count == 1) {
					return ResolveResolver (lookup.First());
				} else {
					throw InjectorErrors.ErrorAmbiguousBinding.FormatEx(bindingKey.BindingType.Name);
				}
			} 

			return BindImplicit (bindingKey);
		}

		public void Bind(IBinding binding)
		{
			IInternalBinding internalBinding = (IInternalBinding) binding;
			CheckBindingType(internalBinding.BindingKey.BindingType);

			lock (syncLock) {
				try {
					IResolver resolver = (IResolver) bindExplicitGeneric
						.MakeGenericMethod (internalBinding.BindingKey.BindingType, internalBinding.ConcreteType)
							.Invoke(this, new object[]{internalBinding.BindingKey, internalBinding.BindingConfig});
				} catch (TargetInvocationException ex) {
					throw ex.InnerException;
				}
			}
		}

		public IInstanceInjectorBinding<CType> BindInstanceInjector<CType> () 
			where CType : class
		{
			CheckBindingType (typeof(CType));
			var iResolver = BindExplicit<CType, CType> (BindingKey.GetMemberInjector<CType>(), null);
			return new InstanceInjectorBinding<CType>(syncLock, iResolver.BindingConfig);
		}

		public void Verify()
		{
			lock (syncLock) {
				foreach (var resolver in allResolvers.Values) {
					resolver.DoResolve ();
				}
			}
		}

		private IResolver BindExplicit<BType, CType>(BindingKey bindingKey, IBindingConfig bindingConfig)
			where BType : class
			where CType : class, BType
		{
			lock (syncLock) {
				IResolver oldResolver, implicitResolver;					

				implicitTypeLookup.Remove (bindingKey);
				if (allResolvers.TryGetValue (bindingKey, out oldResolver)) {
					allResolvers.Remove (bindingKey);
					// TODO - sketchy
					if (!allResolvers.TryGetValue (bindingKey, out implicitResolver) || !object.ReferenceEquals (oldResolver, implicitResolver)) {
						ClearDependencies (oldResolver);
						oldResolver.Changed -= ClearDependentResolvers;
					}
				}

				// Add after create resolver
				var resolver = CreateResolverInstanceGeneric<BType, CType> (bindingKey, bindingConfig, false);
				if (!bindingKey.Member) {
					AddImplicitTypes (bindingKey, ImplicitTypeUtilities.GetImplicitTypes (bindingKey));
				}
				
				ClearDependentResolvers (this, bindingKey);

				return resolver;
			}
		}

		private IResolver BindImplicit(BindingKey bindingKey) {
			lock (syncLock) {
				IResolver resolver;
				if (allResolvers.TryGetValue (bindingKey, out resolver)) {
					return resolver;
				}

				// Handle implementedBy
				var implType = GetIfImplementedBy (bindingKey);
				if (implType != null) {
					resolver = CreateResolverInstance (bindingKey, implType, null, true);
				} else {
					resolver = CreateResolverInstance (bindingKey, bindingKey.BindingType, null, true);
				}

				// NOTE: In theory, implicit bindings should never change the object graph;
				// ... There are some edge cases around dynamically created assemblies - 
				// but we do not care much about those in mobile environments that are targetted
				ClearDependentResolvers (this, bindingKey);

				return resolver;
			}
		}

		private Type GetIfImplementedBy(BindingKey bindingKey) {
			var implTypeAttrs = bindingKey.BindingType.GetCustomAttributes(typeof(ImplementedByAttribute), false);
			if (implTypeAttrs.Length > 0) {
				return (implTypeAttrs[0] as ImplementedByAttribute).Implementor;
			}
			return null;
		}

		private IResolver CreateResolverInstance(BindingKey bindingKey, Type implType, IBindingConfig bindingConfig, bool isImplicitBinding) {
			try {
				return (IResolver) createResolverInstanceGeneric
					.MakeGenericMethod(bindingKey.BindingType, implType)
						.Invoke(this, new object[]{bindingKey, bindingConfig, isImplicitBinding});
			} catch (TargetInvocationException ex) {
				throw ex.InnerException;
			}
		}

		private Resolver<CType> CreateResolverInstanceGeneric<BType, CType>(BindingKey bindingKey, IBindingConfig bindingConfig, bool isImplicitBinding) 
			where BType : class
			where CType : class, BType
		{
			if (bindingConfig == null) {
				bindingConfig = new BindingConfig<CType> ();
				ImplicitTypeUtilities.SetupImplicitPropResolvers<CType>(bindingConfig, syncLock);
			}
			
			var resolver = new Resolver<CType> (this, bindingKey, bindingConfig, syncLock);
			
			if (isImplicitBinding) {
				allResolvers.Add (bindingKey, resolver);
				if (!allResolvers.ContainsKey (bindingKey)) {
					allResolvers.Add (bindingKey, resolver);
				}
			} else {
				allResolvers.Add (bindingKey, resolver);
			}
			
			resolver.Changed += ClearDependentResolvers;

			return resolver;
		}

		private void AddImplicitTypes(BindingKey bindingKey, SetShim<BindingKey> implicitTypeKeys) {
			lock (syncLock) {
				foreach(BindingKey implicitTypeKey in implicitTypeKeys) {
					if (GetIfImplementedBy (implicitTypeKey) == null) {
						SetShim<BindingKey> newSet, oldSet;

						if (implicitTypeLookup.TryGetValue (implicitTypeKey, out oldSet)) {
							implicitTypeLookup.Remove (implicitTypeKey);
							newSet = new SetShim<BindingKey> (oldSet);
						} else {
							newSet = new SetShim<BindingKey> ();
						}

						newSet.Add (bindingKey);
						implicitTypeLookup.Add (implicitTypeKey, newSet);
					} else {
						BindImplicit (implicitTypeKey);
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
				foreach (var t in resolver.Dependencies) {
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
				foreach (var t in resolver.Dependencies.ToArray()) {
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
		private void ClearDependentResolvers(object source, BindingKey bindingKey) {
			lock (syncLock) {
				SetShim<IResolver> resolvers;
				if (resolverDeps.TryGetValue (bindingKey, out resolvers)) {
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
		internal static class ImplicitTypeUtilities {
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

				if (typeof(CType).GetCustomAttributes (typeof(SingletonAttribute), false).Any()) {
					bindingConfig.Lifestyle = Lifestyle.Singleton;
				}
			}

			private static IEnumerable<MInfo> FilterMemberInfo<MInfo>(Type cType, IEnumerable<MInfo> propsOrFields) 
				where MInfo : MemberInfo 
			{
				return from p in propsOrFields 
					where p.GetCustomAttributes(typeof(InjectAttribute), false).Any()
						select p;
			}

			/// <summary>
			/// Gets the implicit types.
			/// </summary>
			/// <returns>The implicit types.</returns>
			/// <param name="boundType">Bound type.</param>
			internal static SetShim<BindingKey> GetImplicitTypes(BindingKey bindType) {
				var implicitTypes = new SetShim<BindingKey>();

				foreach (Type iFace in bindType.BindingType.GetInterfaces()) {
					implicitTypes.Add(BindingKey.Get(iFace));
				}

				Type wTypeChain = bindType.BindingType;
				while ((wTypeChain = wTypeChain.BaseType) != null && wTypeChain != typeof(object)) {
					implicitTypes.Add(BindingKey.Get(wTypeChain));
				}

				return implicitTypes;
			}
		}

		/// <summary>
		/// Instance injector binding.
		/// </summary>
		internal class InstanceInjectorBinding<CType> : IInstanceInjectorBinding<CType>
			where CType : class 
		{ 
			private readonly object syncLock;
			private readonly IBindingConfig bindingConfig;

			internal InstanceInjectorBinding(object syncLock, IBindingConfig bindingConfig) {
				this.syncLock = syncLock;
				this.bindingConfig = bindingConfig;
			}

			public IInstanceInjectorBinding<CType> AddPropertyInjector<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				return AddPropertyInjectorInner (propertyExpression, null);
			}

			public IInstanceInjectorBinding<CType> AddPropertyInjector<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				return AddPropertyInjectorInner (propertyExpression, setter);
			}

			private IInstanceInjectorBinding<CType> AddPropertyInjectorInner<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter) {
				lock (syncLock) {
					BindingUtil.AddPropertyInjectorToBindingConfig<CType, TPropertyType> (bindingConfig, propertyExpression, setter);
				}
				return this;
			}
		}
	}


	internal class Resolver<CType> : IResolver 
		where CType : class 
	{
		private readonly Type cType = typeof(CType);

		protected readonly BindingKey bindingKey;
		protected readonly object syncLock;
		protected readonly Injector injector;

		private readonly IBindingConfig bindingConfig;
		private readonly IExpressionCompiler<CType> expressionCompiler;

		private bool isRecursionTestPending;

		private LifestyleResolver<CType> resolver;
		private Func<CType,CType> resolveProperties;

		public event ResolverChangedEventHandler Changed;

		public virtual SetShim<BindingKey> Dependencies { 
			get { 
				lock (syncLock) {
					return expressionCompiler.Dependencies; 
				}
			} 
		}

		public Resolver(Injector injector, BindingKey bindingKey, IBindingConfig bindingConfig, object syncLock)
		{
			this.injector = injector;
			this.bindingKey = bindingKey;
			this.syncLock = syncLock;

			this.bindingConfig = bindingConfig;
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

					InjectorSetDependencies ();

					resolver = bindingConfig.Lifestyle.GetLifestyleResolver<CType> (syncLock, resolverExpression, resolverExpressionCompiled, testInstance);

					isRecursionTestPending = false; // END: Handle compile loop
				}
			}
		}

		/// <summary>
		/// Ugly hack to ensure correct dependencies for collection types.
		/// </summary>
		protected virtual void InjectorSetDependencies() {
			injector.SetDependencies (this);
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
				if (Changed != null) {
					Changed (this, bindingKey);
				}
				ClearResolver ();
			}
		}

		public virtual void ClearResolver() {
			lock (syncLock) {
				isRecursionTestPending = false;

				expressionCompiler.Dependencies.Clear ();
				ClearInjectorDependencies();

				resolver = null;
			}
		}

		public Expression GetResolveExpression (SetShim<BindingKey> callerDeps) {
			lock (syncLock) {
				if (!IsResolved()) {
					CompileResolver ();
				}

				callerDeps.UnionWith (expressionCompiler.Dependencies);
				callerDeps.Add (bindingKey);

				return resolver.ResolveExpression.Body;
			}
		}

		private void ClearInjectorDependencies() {
			injector.ClearDependencies (this);
		}
	}
}
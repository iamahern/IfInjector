using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;
using IfInjector.Bindings.Fluent.Concrete;
using IfInjector.Bindings.Fluent.OpenGeneric;
using IfInjector.Bindings.Fluent.Members;
using IfInjector.Bindings.Lifestyles;
using IfInjector.Resolver;
using IfInjector.Errors;
using IfInjector.Util;

namespace IfInjector
{	
	/// <summary>
	/// The actual injector implementation.
	/// </summary>
	public class Injector
	{	
		private readonly object syncLock = new object();
		private bool resolveCalled = false;

		private readonly MethodInfo createResolverInstanceGeneric;
		private readonly MethodInfo bindExplicitGeneric;

		private readonly SafeDictionary<BindingKey, IResolver> allResolvers;
		private readonly SafeDictionary<BindingKey, SetShim<BindingKey>> implicitTypeLookup;

		// no implicits initially
		private readonly SafeDictionary<BindingKey, IBindingConfig> allGenericResolvers;

		private readonly SafeDictionary<Type, IResolver> instanceResolversCache;

		public Injector() 
		{
			// Init type dictionaries
			allResolvers = new SafeDictionary<BindingKey, IResolver>(syncLock);
			implicitTypeLookup = new SafeDictionary<BindingKey, SetShim<BindingKey>> (syncLock);

			// Init generic dictionaries
			allGenericResolvers = new SafeDictionary<BindingKey, IBindingConfig> (syncLock);

			// Init resolvers cache
			instanceResolversCache = new SafeDictionary<Type, IResolver>(syncLock);

			// Init resolver
			Expression<Action> tmpExpr = () => CreateResolverInstanceGeneric<Exception, Exception>(null, null, true);
			createResolverInstanceGeneric = ((MethodCallExpression)tmpExpr.Body).Method.GetGenericMethodDefinition();

			// Init bindExplicit
			Expression<Action> tmpBindExpr = () => BindExplicit<Exception, Exception> (null, null);
			bindExplicitGeneric = ((MethodCallExpression)tmpBindExpr.Body).Method.GetGenericMethodDefinition();

			// Implicitly resolvable
			Expression<Func<Injector>> injectorFactoryExpr = () => this;
			var bindingConfig = new BindingConfig(typeof(Injector));
			bindingConfig.FactoryExpression = injectorFactoryExpr;
			bindingConfig.Lifestyle = Lifestyle.Singleton;
			var injectorResolver = BindExplicit<Injector, Injector>(BindingKey.Get<Injector>(), bindingConfig);
		}

		/// <summary>
		/// Bind the specified binding.
		/// </summary>
		/// <param name="binding">Binding.</param>
		public void Register(IBinding binding)
		{
			lock (syncLock) {
				IBindingInternal internalBinding = (IBindingInternal) binding;
				ValidateInternalBinding (internalBinding);
				BindExplicit (internalBinding);
			}
		}

		/// <summary>
		/// Binds the member injector.
		/// </summary>
		/// <param name="membersBinding">Members binding.</param>
		public void Register (IMembersBinding membersBinding)
		{
			lock (syncLock) {
				IBindingInternal internalBinding = (IBindingInternal) membersBinding;
				ValidateInternalBinding (internalBinding);
				BindExplicit (internalBinding);
			}
		}

		/// <summary>
		/// Bind the specified binding.
		/// </summary>
		/// <param name="binding">Binding.</param>
		/// <param name="openGenericBinding">Open generic binding.</param>
		public void Register (IOpenGenericBinding openGenericBinding)
		{
			lock (syncLock) {
				IBindingInternal internalBinding = (IBindingInternal) openGenericBinding;
				ValidateInternalBinding (internalBinding);
				allGenericResolvers.Add (internalBinding.BindingKey, internalBinding.BindingConfig);
			}
		}

		/// <summary>
		/// Resolve this instance.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T Resolve<T>()
			where T : class
		{
			return (T)Resolve (typeof(T));
		}

		/// <summary>
		/// Resolve the specified type.
		/// </summary>
		/// <param name="type">Type.</param>
		public object Resolve(Type type)
		{
			IResolver resolver;

			if (!instanceResolversCache.UnsyncedTryGetValue (type, out resolver)) {
				resolver = ResolveResolver (BindingKey.Get (type));
				lock (syncLock) {
					if (!instanceResolversCache.ContainsKey (type)) {
						instanceResolversCache.Add (type, resolver);
					}
				}
			}

			return resolver.DoResolve ();
		}

		/// <summary>
		/// Injects the properties of an instance.
		/// </summary>
		/// <returns>The instance object.</returns>
		/// <param name="instance">Instance.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public T InjectProperties<T> (T instance)
			where T : class
		{
			var iResolver = ResolveResolver (BindingKey.GetMember<T>());
			iResolver.DoInject (instance);
			return instance;
		}

		/// <summary>
		/// Verify that all bindings all valid.
		/// </summary>
		public void Verify()
		{
			lock (syncLock) {
				foreach (var resolver in allResolvers.Values) {
					resolver.DoResolve ();
				}
			}
		}

		internal Expression ResolveResolverExpression(BindingKey bindingKey)
		{
			return ResolveResolver (bindingKey).GetResolveExpression ();
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

		internal void SetResolveCalled() {
			lock (syncLock) {
				if (!resolveCalled) {
					resolveCalled = true;
				}
			}
		}

		private IResolver BindExplicit(IBindingInternal internalBinding) {
			try {
				return (IResolver) bindExplicitGeneric
					.MakeGenericMethod (internalBinding.BindingKey.BindingType, internalBinding.ConcreteType)
						.Invoke(this, new object[]{internalBinding.BindingKey, internalBinding.BindingConfig});
			} catch (TargetInvocationException ex) {
				throw ex.InnerException;
			}
		}

		private IResolver BindExplicit<BType, CType>(BindingKey bindingKey, IBindingConfig bindingConfig)
			where BType : class
			where CType : class, BType
		{
			lock (syncLock) {
				IResolver oldResolver;					
				if (allResolvers.TryGetValue (bindingKey, out oldResolver)) {
					allResolvers.Remove (bindingKey);
					implicitTypeLookup.Remove (bindingKey);
				}

				// Add after create resolver
				var resolver = CreateResolverInstanceGeneric<BType, CType> (bindingKey, bindingConfig, false);
				if (!bindingKey.Member) {
					AddImplicitTypes (bindingKey, GetImplicitTypes (bindingKey));
				}

				return resolver;
			}
		}

		private IResolver BindImplicit(BindingKey bindingKey) {
			lock (syncLock) {
				IResolver resolver;
				if (allResolvers.TryGetValue (bindingKey, out resolver)) {
					return resolver;
				}

				// Handle explicit generic
				IBindingConfig bindingConfig = null;
				Type implType = null;
				if (bindingKey.BindingType.IsGenericType) {
					implType = GetIfImplementedByForGeneric (bindingKey, out bindingConfig);
				} else {
					implType = GetIfImplementedByForType (bindingKey.BindingType);
				}

				if (implType != null) {
					return CreateResolverInstance (bindingKey, implType, bindingConfig, true);
				} else {
					return CreateResolverInstance (bindingKey, bindingKey.BindingType, bindingConfig, true);
				}
			}
		}

		private Type GetIfImplementedBy(BindingKey bindingKey) {
			Type bindingType = bindingKey.BindingType;

			if (bindingType.IsGenericType) {
				return GetIfImplementedByForGeneric (bindingKey);
			}

			return GetIfImplementedByForType (bindingType);
		}

		private Type GetIfImplementedByForType(Type bindingType) {
			var implTypeAttr = bindingType.GetCustomAttributes(typeof(ImplementedByAttribute), false).FirstOrDefault();
			if (implTypeAttr != null) {
				return (implTypeAttr as ImplementedByAttribute).Implementor;
			}

			return null;
		}

		private Type GetIfImplementedByForGeneric(BindingKey bindingKey) {
			IBindingConfig bindingConfig;
			return GetIfImplementedByForGeneric (bindingKey, out bindingConfig);
		}

		private Type GetIfImplementedByForGeneric(BindingKey bindingKey, out IBindingConfig bindingConfig) {
			var bindingType = bindingKey.BindingType;
			Type genericConcreteType = null;

			if (bindingType.IsGenericTypeDefinition) {
				throw InjectorErrors.ErrorGenericsCannotResolveOpenType.FormatEx (bindingType);
			}

			var genericBindingType = bindingType.GetGenericTypeDefinition ();
			var genericTypeArguments = bindingType.GetGenericArguments ();
			var genericBindingKey = BindingKey.GetInternal (genericBindingType, bindingKey.Member);

			// Try registrations
			IBindingConfig genericBindingConfig;
			if (allGenericResolvers.TryGetValue (genericBindingKey, out genericBindingConfig)) {
				genericConcreteType = genericBindingConfig.ConcreteType;
			}

			// Try implicit
			if (genericConcreteType == null) {
				genericConcreteType = GetIfImplementedByForType (genericBindingType);
			}

			// Throw ex if unable to resolve concrete type
			bindingConfig = null;
			if (genericConcreteType != null) {
				OpenGenericBinding.For (genericBindingType).To (genericConcreteType); // validate binding
				Type concreteType = genericConcreteType.MakeGenericType (genericTypeArguments);

				if (genericBindingConfig != null) {
					bindingConfig = new BindingConfig (concreteType);
					bindingConfig.Lifestyle = genericBindingConfig.Lifestyle;
				}

				return concreteType;
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
				bindingConfig = BindingConfigUtils.CreateImplicitBindingSettings<CType> ();
			} else {
				bindingConfig = BindingConfigUtils.MergeImplicitWithExplicitSettings<CType> (bindingConfig);
			}
			
			var resolver = new Resolver<CType> (this, bindingConfig, syncLock);
			
			if (isImplicitBinding) {
				allResolvers.Add (bindingKey, resolver);
				if (!allResolvers.ContainsKey (bindingKey)) {
					allResolvers.Add (bindingKey, resolver);
				}
			} else {
				allResolvers.Add (bindingKey, resolver);
			}

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
		
		private void ValidateInternalBinding(IBindingInternal internalBinding) {
			if (typeof(Injector).Equals(internalBinding.BindingKey.BindingType)) {
				throw InjectorErrors.ErrorMayNotBindInjector.FormatEx ();
			}

			if (resolveCalled) {
				throw InjectorErrors.ErrorBindingRegistrationNotPermitted.FormatEx ();
			}
		}

		/// <summary>
		/// Gets the implicit types.
		/// </summary>
		/// <returns>The implicit types.</returns>
		/// <param name="boundType">Bound type.</param>
		private static SetShim<BindingKey> GetImplicitTypes(BindingKey bindingKey) {
			var implicitTypes = new SetShim<BindingKey>();
			var bindingType = bindingKey.BindingType;

			foreach (Type iFace in bindingType.GetInterfaces()) {
				implicitTypes.Add (BindingKey.Get (iFace));
			}

			Type wTypeChain = bindingType;
			while ((wTypeChain = wTypeChain.BaseType) != null && wTypeChain != typeof(object)) {
				implicitTypes.Add (BindingKey.Get (wTypeChain));
			}

			return implicitTypes;
		}
	}
}
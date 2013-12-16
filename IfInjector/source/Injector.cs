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
	public class Injector : IInjector
	{	
		private readonly object syncLock = new object();
		private bool resolveCalled = false;

		private readonly MethodInfo createResolverInstanceGeneric;
		private readonly MethodInfo bindExplicitGeneric;

		private readonly SafeDictionary<BindingKey, IResolver> allResolvers;

		private readonly ImplicitBindingResolver implicitBindingResolver;
		private readonly GenericBindingResolver genericBindingRresolver;

		private readonly SafeDictionary<Type, IResolver> instanceResolversCache;

		public Injector() 
		{
			// Registration of resolvers
			allResolvers = new SafeDictionary<BindingKey, IResolver>(syncLock);

			// Registration of binding key resolvers
			implicitBindingResolver = new ImplicitBindingResolver (syncLock);
			genericBindingRresolver = new GenericBindingResolver (this, syncLock);

			// Init resolvers cache
			instanceResolversCache = new SafeDictionary<Type, IResolver>(syncLock);

			// Init resolver
			Expression<Action> tmpExpr = () => CreateResolverInstance<Exception, Exception>(null, null);
			createResolverInstanceGeneric = ((MethodCallExpression)tmpExpr.Body).Method.GetGenericMethodDefinition();

			// Init bindExplicit
			Expression<Action> tmpBindExpr = () => BindExplicit<Exception, Exception> (null, null);
			bindExplicitGeneric = ((MethodCallExpression)tmpBindExpr.Body).Method.GetGenericMethodDefinition();

			// Implicitly resolvable
			Expression<Func<Injector>> injectorFactoryExpr = () => this;
			var bindingConfig = new BindingConfig(typeof(Injector));
			bindingConfig.FactoryExpression = injectorFactoryExpr;
			bindingConfig.Lifestyle = Lifestyle.Singleton;
			var injectorResolver = BindExplicit<Injector, Injector>(BindingKey.Get(typeof(Injector)), bindingConfig);
		}

		/// <inheritdoc/>
		public void Register(IBinding binding)
		{
			lock (syncLock) {
				IBindingInternal internalBinding = (IBindingInternal) binding;
				ValidateInternalBinding (internalBinding);
				BindExplicit (internalBinding);
			}
		}

		/// <inheritdoc/>
		public void Register (IMembersBinding membersBinding)
		{
			lock (syncLock) {
				IBindingInternal internalBinding = (IBindingInternal) membersBinding;
				ValidateInternalBinding (internalBinding);
				BindExplicit (internalBinding);
			}
		}

		/// <inheritdoc/>
		public void Register (IOpenGenericBinding openGenericBinding)
		{
			lock (syncLock) {
				IBindingInternal internalBinding = (IBindingInternal) openGenericBinding;
				ValidateInternalBinding (internalBinding);
				genericBindingRresolver.Register (internalBinding);
			}
		}

		/// <inheritdoc/>
		public T Resolve<T>()
			where T : class
		{
			return (T)Resolve (typeof(T));
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
		public T InjectMembers<T> (T instance)
			where T : class
		{
			var iResolver = ResolveResolver (BindingKey.GetMember(instance.GetType()));
			iResolver.DoInject (instance);
			return instance;
		}

		/// <inheritdoc/>
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
			BindingKey implicitKey;
			IResolver resolver;

			if (allResolvers.UnsyncedTryGetValue (bindingKey, out resolver)) {
				return resolver;
			} else if ((implicitKey = ResolveImplicitKey(bindingKey)) != null) {
				return ResolveResolver (implicitKey);
			}

			return BindImplicit (bindingKey);
		}

		/// <summary>
		/// Resolves the implicit key.
		/// </summary>
		/// <returns>The implicit key.</returns>
		/// <param name="bindingKey">Binding key.</param>
		private BindingKey ResolveImplicitKey(BindingKey bindingKey) {
			BindingKey implicitKey = genericBindingRresolver.ResolveBinding (bindingKey);
			
			if (implicitKey == null) {
				implicitKey = implicitBindingResolver.ResolveBinding (bindingKey);
			}

			return implicitKey;
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
					if (bindingKey.IsImplicit) {
						// Handle edge case where 2 thread auto-register same implicit 
						// binding via 'ResolveBinding'. More complex to add guard within 
						// ResolveBinding() than here.
						return oldResolver;
					}
					allResolvers.Remove (bindingKey);
				}
				
				// Add after create resolver
				var resolver = CreateResolverInstance<BType, CType> (bindingKey, bindingConfig);

				// Register Implicits
				implicitBindingResolver.Register(bindingKey);
				
				return resolver;
			}
		}

		private IResolver BindImplicit(BindingKey bindingKey) {
			lock (syncLock) {
				IResolver resolver;
				if (allResolvers.TryGetValue (bindingKey, out resolver)) {
					return resolver;
				}

				Type implType = BindingAttributeUtils.GetImplementedBy (bindingKey.BindingType);

				return CreateResolverInstanceGeneric (
					bindingKey.ToImplicit(),  
					(implType != null) ? implType : bindingKey.BindingType);
			}
		}
		
		private IResolver CreateResolverInstanceGeneric(BindingKey bindingKey, Type implType) {
			try {
				return (IResolver) createResolverInstanceGeneric
					.MakeGenericMethod(bindingKey.BindingType, implType)
						.Invoke(this, new object[]{bindingKey, null});
			} catch (TargetInvocationException ex) {
				throw ex.InnerException;
			}
		}

		private Resolver<CType> CreateResolverInstance<BType, CType>(BindingKey bindingKey, IBindingConfig bindingConfig) 
			where BType : class
			where CType : class, BType
		{
			if (bindingConfig == null) {
				bindingConfig = BindingConfigUtils.CreateImplicitBindingSettings<CType> ();
			} else {
				bindingConfig = BindingConfigUtils.MergeImplicitWithExplicitSettings<CType> (bindingConfig);
			}
			
			var resolver = new Resolver<CType> (this, bindingConfig, syncLock);
			allResolvers.Add (bindingKey, resolver);
			
			return resolver;
		}

		private void ValidateInternalBinding(IBindingInternal internalBinding) {
			var bindingKey = internalBinding.BindingKey;
			if (typeof(Injector).Equals(bindingKey.BindingType)) {
				throw InjectorErrors.ErrorMayNotBindInjector.FormatEx ();
			}

			if (resolveCalled && !bindingKey.IsImplicit) {
				throw InjectorErrors.ErrorBindingRegistrationNotPermitted.FormatEx ();
			}
		}
	}
}
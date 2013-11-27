using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Lifestyles;
using IfInjector.Errors;
using IfInjector.Resolver.Expressions;

namespace IfInjector.Resolver
{
	/// <summary>
	/// Internal resolver type.
	/// </summary>
	internal class Resolver<CType> : IResolver 
		where CType : class 
	{
		private readonly Type cType = typeof(CType);

		protected readonly object syncLock;
		protected readonly Injector injector;

		private readonly IBindingConfig bindingConfig;
		private readonly IExpressionCompiler<CType> expressionCompiler;

		private bool isRecursionTestPending;

		private LifestyleResolver<CType> resolver;
		private Func<CType,CType> resolveProperties;

		public Resolver(Injector injector, IBindingConfig bindingConfig, object syncLock)
		{
			this.injector = injector;
			this.bindingConfig = bindingConfig;
			this.syncLock = syncLock;

			this.expressionCompiler = new ExpressionCompiler<CType> (
				bindingConfig, 
				injector.ResolveResolverExpression);
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
					injector.SetResolveCalled (); // Indicate resolve called to change the injector to read-only

					if (isRecursionTestPending) { // START: Handle compile loop
						throw InjectorErrors.ErrorResolutionRecursionDetected.FormatEx(cType.Name);
					}
					isRecursionTestPending = true; 

					resolver = bindingConfig.Lifestyle.GetLifestyleResolver<CType> (
						syncLock, 
						expressionCompiler, 
						(CType) expressionCompiler.InstanceResolver());

					isRecursionTestPending = false; // END: Handle compile loop
				}
			}
		}

		private CType DoInjectTyped(CType instance) {
			if (instance != null) {
				if (resolveProperties == null) {
					lock (syncLock) {
						// Expression compiler caches delegate; no need for double checking
						resolveProperties = expressionCompiler.PropertiesResolver;
					}
				}

				resolveProperties (instance);
			}

			return instance;
		}

		public Expression GetResolveExpression () {
			lock (syncLock) {
				if (!IsResolved()) {
					CompileResolver ();
				}

				return resolver.ResolveExpression;
			}
		}

		private bool IsResolved() {
			return resolver != null;
		}		
	}
}


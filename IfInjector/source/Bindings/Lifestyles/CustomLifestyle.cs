using System;
using System.Linq.Expressions;

using IfInjector.Resolver.Expressions;

namespace IfInjector.Bindings.Lifestyles
{
	internal class CustomLifestyle : Lifestyle {
		private readonly CustomLifestyleDelegate lifestyleDelegate;

		internal CustomLifestyle(CustomLifestyleDelegate lifestyleDelegate) {
			this.lifestyleDelegate = lifestyleDelegate;
		}

		internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
			object syncLock, 
			IExpressionCompiler<CType> expressionCompiler,
			CType testInstance)
		{
			Func<object> instanceResolver = () => expressionCompiler.InstanceResolver ();
			return new CustomLifecyleResolver<CType>(lifestyleDelegate(instanceResolver));
		}

		private class CustomLifecyleResolver<CType> : LifestyleResolver<CType> where CType : class {
			private readonly Func<object> instanceResolver;

			internal CustomLifecyleResolver(Func<object> instanceResolver) : base() 
			{
				this.instanceResolver = instanceResolver;
			}

			internal override CType Resolve() {
				return (CType) instanceResolver();
			}
		}
	}
}
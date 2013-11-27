using System;
using System.Linq.Expressions;

using IfInjector.Resolver.Expressions;

namespace IfInjector.Bindings.Lifestyles
{
	class TransientLifestyle : Lifestyle {
		internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
			object syncLock, 
			IExpressionCompiler<CType> expressionCompiler,
			CType testInstance)
		{
			return new TransientLifestyleResolver<CType>(expressionCompiler);
		}

		private class TransientLifestyleResolver<CType> : LifestyleResolver<CType> where CType : class {
			private readonly Func<CType> instanceResolver;

			internal TransientLifestyleResolver(IExpressionCompiler<CType> expressionCompiler) : base(expressionCompiler.InstanceResolverExpression) {
				this.instanceResolver = expressionCompiler.InstanceResolver;
			}

			internal override CType Resolve() {
				return instanceResolver();
			}
		}
	}
}
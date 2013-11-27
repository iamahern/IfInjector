using System;
using System.Linq.Expressions;

using IfInjector.Resolver.Expressions;

namespace IfInjector.Bindings.Lifestyles
{
	/// <summary>
	/// Singleton lifestyle.
	/// </summary>
	internal class SingletonLifestyle : Lifestyle {
		internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
			object syncLock, 
			IExpressionCompiler<CType> expressionCompiler,
			CType testInstance)
		{
			return new SingletonLifestyleResolver<CType>(testInstance);
		}

		private class SingletonLifestyleResolver<CType> : LifestyleResolver<CType> where CType : class {
			private readonly CType instance;

			internal SingletonLifestyleResolver(CType instance) :
				base(Expression.Constant(instance))
			{
				this.instance = instance;
			}

			internal override CType Resolve() {
				return instance;
			}
		}
	}
}


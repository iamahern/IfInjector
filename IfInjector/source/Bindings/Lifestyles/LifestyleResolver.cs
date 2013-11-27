using System;
using System.Linq.Expressions;

namespace IfInjector.Bindings.Lifestyles
{
	/// <summary>
	/// Lifestyle resolver. This is used to build resolvers and resolver expressions for a given lifestyle.
	/// </summary>
	internal abstract class LifestyleResolver<CType> where CType : class {
		private readonly Expression resolveExpression;

		/// <summary>
		/// Initializes a new instance of the <see cref="IfInjector.Bindings.Lifestyles.LifestyleResolver`1"/> class.
		/// </summary>
		/// <param name="resolveExpression">Resolve expression.</param>
		internal LifestyleResolver(Expression resolveExpression) {
			this.resolveExpression = resolveExpression;
		}

		internal LifestyleResolver() {
			Expression<Func<CType>> expr = () => Resolve ();
			resolveExpression = expr.Body;
		}

		/// <summary>
		/// Gets the resolve expression. This is used in expression compilation.
		/// </summary>
		/// <value>The resolve expression.</value>
		internal Expression ResolveExpression {
			get {
				return resolveExpression;
			}
		}

		/// <summary>
		/// Resolve this instance. The lifestyle is responsible for creating additional instances as necessary.
		/// </summary>
		internal abstract CType Resolve();
	}
}
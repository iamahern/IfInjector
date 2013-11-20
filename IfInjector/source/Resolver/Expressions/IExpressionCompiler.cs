using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Config;

namespace IfInjector.Resolver.Expressions
{
	/// <summary>
	/// Resolve instance expression.
	/// </summary>
	internal delegate Expression ResolveResolverExpression(BindingKey bindingKey);

	/// <summary>
	/// Expression compiler definition. Synchronization must be managed externally by the API caller.
	/// </summary>
	internal interface IExpressionCompiler<CType> where CType : class {
		/// <summary>
		/// Compiles the resolver expression.
		/// </summary>
		/// <returns>The resolver expression.</returns>
		Expression<Func<CType>> CompileResolverExpression();

		/// <summary>
		/// Compiles the properties resolver.
		/// </summary>
		/// <returns>The properties resolver.</returns>
		Func<CType, CType> CompilePropertiesResolver ();
	}
}


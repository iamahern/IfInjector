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
		/// Gets the instance resolver expression.
		/// </summary>
		/// <value>The instance resolver expression.</value>
		Expression InstanceResolverExpression { get; }

		/// <summary>
		/// Gets the instance resolver.
		/// </summary>
		/// <value>The instance resolver.</value>
		Func<CType> InstanceResolver { get; } 

		/// <summary>
		/// Gets the properties resolver.
		/// </summary>
		/// <value>The properties resolver.</value>
		Func<CType, CType> PropertiesResolver { get; }
	}
}


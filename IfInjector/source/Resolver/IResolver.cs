using System;
using System.Linq.Expressions;

namespace IfInjector.Resolver
{
	/// <summary>
	/// Internal resolver interface.
	/// </summary>
	internal interface IResolver {
		/// <summary>
		/// Resolves the object.
		/// </summary>
		/// <returns>The resolve.</returns>
		object DoResolve ();

		/// <summary>
		/// Performs property injection on an instance.
		/// </summary>
		/// <param name="instance">Instance.</param>
		void DoInject (object instance);

		/// <summary>
		/// Gets the resolve expression.
		/// </summary>
		/// <returns>The resolve expression.</returns>
		Expression GetResolveExpression ();
	}
}


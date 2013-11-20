using System;
using System.Linq.Expressions;

namespace IfInjector.Bindings.Fluent.Concrete
{
	/// <summary>
	/// Internal interface for ongoing bindings.
	/// </summary>
	internal interface IOngoingBindingInternal<BType> 
		where BType : class
	{
		/// <summary>
		/// Sets the factory lambda.
		/// </summary>
		/// <returns>The factory lambda.</returns>
		/// <param name="factoryExpression">Factory expression.</param>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		IBinding<BType, CType> SetFactoryLambda<CType>(LambdaExpression factoryExpression)
			where CType : class, BType;
	}
}


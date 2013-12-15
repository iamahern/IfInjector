using System;
using System.Linq.Expressions;

namespace IfInjector.Bindings.Fluent
{
	public interface IMemberInjectableBinding<FType, CType>
		where FType : IMemberInjectableBinding<FType, CType>
		where CType : class
	{
		/// <summary>
		/// Indicate that the referenced property should be injected.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="memberExpression">Member expression.</param>
		/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
		FType InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> memberExpression) 
			where TPropertyType : class;

		/// <summary>
		/// Indicate that the referenced property should be injected using the specified setter expression.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="memberExpression">Member expression.</param>
		/// <param name="setter">Setter.</param>
		/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
		FType InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> memberExpression, Expression<Func<TPropertyType>> setter);
	}
}


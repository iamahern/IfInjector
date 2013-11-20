using System;
using System.Linq.Expressions;

namespace IfInjector.Bindings
{
	public interface IPropertyInjectableBinding<FType, CType>
		where FType : IPropertyInjectableBinding<FType, CType>
		where CType : class
	{
		/// <summary>
		/// Indicate that the referenced property should be injected.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="propertyExpression">Property expression.</param>
		/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
		FType InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
			where TPropertyType : class;

		/// <summary>
		/// Indicate that the referenced property should be injected using the specified setter expression.
		/// </summary>
		/// <returns>The property.</returns>
		/// <param name="propertyExpression">Property expression.</param>
		/// <param name="setter">Setter.</param>
		/// <typeparam name="TPropertyType">The 1st type parameter.</typeparam>
		FType InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter);
	}
}


using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Properties
{
	/// <summary>
	/// Base binding type. This represents a closed member binding object.
	/// </summary>
	public interface IPropertiesBinding {}

	/// <summary>
	/// Members binding.
	/// </summary>
	public interface IPropertiesBinding<CType> : IPropertiesBinding,
		IPropertyInjectableBinding<IPropertiesBinding<CType>, CType>
		where CType : class
	{
	}
}


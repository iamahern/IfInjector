using System;
using System.Linq.Expressions;
using IfInjector.Bindings;
using IfInjector.Bindings.Lifestyles;

namespace IfInjector.Bindings.Fluent.Concrete
{
	/// <summary>
	/// Base binding type. This represents a closed binding object.
	/// </summary>
	public interface IBinding {}

	/// <summary>
	/// Base binding type. This represents a 'typed' closed binding object.
	/// </summary>
	public interface IBinding<BType, CType> : IBinding
		where BType : class
		where CType : class, BType
	{}
}

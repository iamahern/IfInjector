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
	/// Bbound binding.
	/// </summary>
	public interface IBinding<BType, CType> : IBinding,
			ILifestyleSetableBinding<IBinding<BType, CType>>, 
			IMemberInjectableBinding<IBinding<BType, CType>, CType>
		where BType : class
		where CType : class, BType
	{
	}
}

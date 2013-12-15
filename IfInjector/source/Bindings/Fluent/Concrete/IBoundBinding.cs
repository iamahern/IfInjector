using System;

namespace IfInjector.Bindings.Fluent.Concrete
{
	/// <summary>
	/// Bound binding type.
	/// </summary>
	public interface IBoundBinding<BType, CType> : 
		IBinding<BType, CType>,
		ILifestyleSetableBinding<IBoundBinding<BType, CType>>, 
		IMemberInjectableBinding<IBoundBinding<BType, CType>, CType>
			where BType : class
			where CType : class, BType
	{
	}
}


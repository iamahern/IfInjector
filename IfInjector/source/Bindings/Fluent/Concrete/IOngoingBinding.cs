using System;

namespace IfInjector.Bindings.Fluent.Concrete
{
	/// <summary>
	/// Ongoing binding.
	/// </summary>
	public interface IOngoingBinding<BType> : IBoundBinding<BType, BType>
		where BType : class
	{
		/// <summary>
		/// Associate this binding with the specified implementation type.
		/// </summary>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		IBoundBinding<BType, CType> To<CType> () 
			where CType : class, BType;
	}
}


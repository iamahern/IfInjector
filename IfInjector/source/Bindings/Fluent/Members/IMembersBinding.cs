using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Members
{
	/// <summary>
	/// Base binding type. This represents a closed member binding object.
	/// </summary>
	public interface IMembersBinding {}

	/// <summary>
	/// Members binding.
	/// </summary>
	public interface IMembersBinding<CType> : IMembersBinding,
		IMemberInjectableBinding<IMembersBinding<CType>, CType>
		where CType : class
	{
	}
}


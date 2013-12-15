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
	/// Base binding type. This represents a closed, typed member binding object.
	/// </summary>
	public interface IMembersBinding<CType> : IMembersBinding
		where CType : class
	{
	}
}


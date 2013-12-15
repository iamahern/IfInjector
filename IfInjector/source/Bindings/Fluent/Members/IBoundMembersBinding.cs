using System;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Members
{
	public interface IBoundMembersBinding<CType> :
			IMembersBinding<CType>,
			IMemberInjectableBinding<IBoundMembersBinding<CType>, CType>
		where CType : class
	{
	}
}


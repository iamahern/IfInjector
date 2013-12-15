using System;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.OpenGeneric
{
	public interface IBoundOpenGenericBinding : 
		IOpenGenericBinding,
		ILifestyleSetableBinding<IBoundOpenGenericBinding>
	{
	}
}


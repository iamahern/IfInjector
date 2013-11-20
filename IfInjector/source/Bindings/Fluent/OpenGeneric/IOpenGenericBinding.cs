using System;
using IfInjector.Bindings;
using IfInjector.Bindings.Lifestyles;

namespace IfInjector.Bindings.Fluent.OpenGeneric
{
	/// <summary>
	/// Binding type for open generic bindings.
	/// </summary>
	public interface IOpenGenericBinding : 
		ILifestyleSetableBinding<IOpenGenericBinding> 
	{
	}
}
using System;

namespace IfInjector
{
	/// <summary>
	/// Inject attribute. Used to flag constructors for preferred injection. 
	/// </summary>
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Property | AttributeTargets.Field)]
	public class InjectAttribute : Attribute {}
}


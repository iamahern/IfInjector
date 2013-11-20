using System;

namespace IfInjector
{
	/// <summary>
	/// Singleton attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class, Inherited=true)]
	public class SingletonAttribute : Attribute {}
}


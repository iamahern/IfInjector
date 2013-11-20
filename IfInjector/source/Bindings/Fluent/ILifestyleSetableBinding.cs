using System;
using IfInjector.Bindings.Lifestyles;

namespace IfInjector.Bindings
{
	public interface ILifestyleSetableBinding<FType>
		where FType : ILifestyleSetableBinding<FType>
	{
		/// <summary>
		/// Sets the lifestyle.
		/// </summary>
		/// <returns>The lifestyle.</returns>
		/// <param name="lifestyle">Lifestyle.</param>
		FType SetLifestyle (Lifestyle lifestyle);
	}
}


using System;
using IfInjector.Bindings.Fluent.Concrete;

namespace IfInjector
{
	/// <summary>
	/// Binding factory class.
	/// </summary>
	public static class Binding {

		/// <summary>
		/// Create a binding.
		/// </summary>
		/// <typeparam name="BType">The 1st type parameter.</typeparam>
		public static IOngoingBinding<BType> For<BType>() where BType : class {
			return new OngoingBindingInternal<BType>(); 
		}
	}
}


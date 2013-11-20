using System;
using IfInjector.Bindings.Fluent.Properties;

namespace IfInjector
{
	/// <summary>
	/// Member binding factory class.
	/// </summary>
	public static class PropertiesBinding {

		/// <summary>
		/// Create a binding.
		/// </summary>
		/// <typeparam name="BType">The 1st type parameter.</typeparam>
		public static IPropertiesBinding<BType> For<BType>() where BType : class {
			return new PropertiesBinding<BType>(); 
		}
	}
}


using System;
using IfInjector.Bindings.Fluent.Members;

namespace IfInjector
{
	/// <summary>
	/// Member binding factory class.
	/// </summary>
	public static class MembersBinding {

		/// <summary>
		/// Create a binding.
		/// </summary>
		/// <typeparam name="BType">The 1st type parameter.</typeparam>
		public static IBoundMembersBinding<BType> For<BType>() where BType : class {
			return new BoundMembersBinding<BType>(); 
		}
	}
}


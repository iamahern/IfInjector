using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq.Expressions;

using System.Linq;
using IfInjector.Bindings.Lifestyles;
using IfInjector.Util;

namespace IfInjector.Bindings.Config
{
	/// <summary>
	/// The binding key object is an immutable type used to index bindings.
	/// </summary>
	internal sealed class BindingKey : IEquatable<BindingKey> {
		/// <summary>
		/// Internal constant for hash code.
		/// </summary>
		private const int HASHCODE_MULTIPLIER = 33;

		private readonly Type bindingType;
		private readonly bool member;

		private BindingKey(Type bindingType, bool member) {
			this.bindingType = bindingType;
			this.member = member;
		}

		/// <summary>
		/// Gets the type of the binding.
		/// </summary>
		/// <value>The type of the binding.</value>
		public Type BindingType {
			get {
				return bindingType;	
			}
		}

		/// <summary>
		/// Gets a value indicating whether this <see cref="IfInjector.IfBinding.BindingKey"/> is a property-only binding.
		/// </summary>
		/// <value><c>true</c> if member; otherwise, <c>false</c>.</value>
		public bool Member {
			get {
				return member;
			}
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode() {
			return HASHCODE_MULTIPLIER * BindingType.GetHashCode () + Member.GetHashCode ();
		}

		/// <summary>
		/// Determines whether the specified <see cref="System.Object"/> is equal to the current <see cref="IfInjector.IfBinding.BindingKey"/>.
		/// </summary>
		/// <param name="obj">The <see cref="System.Object"/> to compare with the current <see cref="IfInjector.IfBinding.BindingKey"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="System.Object"/> is equal to the current
		/// <see cref="IfInjector.IfBinding.BindingKey"/>; otherwise, <c>false</c>.</returns>
		public override bool Equals(object obj) {
			return Equals(obj as BindingKey);
		}

		/// <summary>
		/// Determines whether the specified <see cref="IfInjector.IfBinding.BindingKey"/> is equal to the current <see cref="IfInjector.IfBinding.BindingKey"/>.
		/// </summary>
		/// <param name="obj">The <see cref="IfInjector.IfBinding.BindingKey"/> to compare with the current <see cref="IfInjector.IfBinding.BindingKey"/>.</param>
		/// <returns><c>true</c> if the specified <see cref="IfInjector.IfBinding.BindingKey"/> is equal to the current
		/// <see cref="IfInjector.IfBinding.BindingKey"/>; otherwise, <c>false</c>.</returns>
		public bool Equals(BindingKey obj) {
			if (obj == null) {
				return false;
			} else if (object.ReferenceEquals (this, obj)) {
				return true;
			} else {
				return BindingType.Equals (obj.BindingType) &&
					Member == obj.Member;
			}
		}

		/// <summary>
		/// Get this instance of the binding key.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static BindingKey Get<T>() where T : class {
			return GetInternal (typeof(T), false);
		}

		/// <summary>
		/// Get the instance injector.
		/// </summary>
		/// <param name="keyType">Key type.</param>
		public static BindingKey Get(Type keyType) {
			return GetInternal (keyType, false);
		}

		/// <summary>
		/// Gets the member injector key.
		/// </summary>
		/// <returns>The member injector key.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		internal static BindingKey GetMember<T>() where T : class {
			return GetInternal (typeof(T), true);
		}

		/// <summary>
		/// Gets a key.
		/// </summary>
		/// <returns>The internal.</returns>
		/// <param name="keyType">Key type.</param>
		/// <param name="isMember">If set to <c>true</c> is member.</param>
		internal static BindingKey GetInternal(Type keyType, bool isMember) {
			return new BindingKey (keyType, isMember);
		}
	}
}
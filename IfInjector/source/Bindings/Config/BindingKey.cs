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
	internal class BindingKey : IEquatable<BindingKey> {
		/// <summary>
		/// Marker value to indicate no qualifier value.
		/// </summary>
		private static readonly string NoQualifierValue = "49cb7b90-6bd3-4041-b58d-bc29cf55b3a2";

		/// <summary>
		/// Internal constant for hash code.
		/// </summary>
		private const int HASHCODE_MULTIPLIER = 33;

		private readonly Type bindingType;
		private readonly bool member;
		private readonly string qualifier;

		protected BindingKey(Type bindingType, bool member, string qualifier) {
			this.bindingType = bindingType;
			this.member = member;
			this.qualifier = (qualifier == null) ? NoQualifierValue : qualifier;
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
		/// Gets the qualifier.
		/// </summary>
		/// <value>The qualifier.</value>
		public string Qualifier {
			get {
				if (NoQualifierValue == qualifier) {
					return null;
				}
				return qualifier;
			}
		}

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode() {
			return HASHCODE_MULTIPLIER * BindingType.GetHashCode () + Member.GetHashCode () + qualifier.GetHashCode();
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
				return bindingType.Equals (obj.bindingType) &&
					member == obj.member &&
					qualifier.Equals(obj.qualifier);
			}
		}

		/// <summary>
		/// Get the instance injector.
		/// </summary>
		/// <param name="keyType">Key type.</param>
		internal static BindingKey Get(Type keyType) {
			return Get (keyType, null);
		}

		/// <summary>
		/// Gets a key.
		/// </summary>
		/// <returns>The internal.</returns>
		/// <param name="keyType">Key type.</param>
		/// <param name="isMember">If set to <c>true</c> is member.</param>
		internal static BindingKey Get(Type keyType, bool isMember) {
			return new BindingKey (keyType, isMember, null);
		}

		/// <summary>
		/// Gets the internal.
		/// </summary>
		/// <returns>The internal.</returns>
		/// <param name="keyType">Key type.</param>
		/// <param name="isMember">If set to <c>true</c> is member.</param>
		/// <param name="qualifier">Qualifier.</param>
		internal static BindingKey Get(Type keyType, string qualifier) {
			return new BindingKey (keyType, false, qualifier);
		}
	}

	/// <summary>
	/// Typed binding key.
	/// </summary>
	internal class BindingKey<BType> : BindingKey where BType : class {
		internal static readonly BindingKey<BType> InstanceKey = new BindingKey<BType> (false, null);
		internal static readonly BindingKey<BType> MembersKey = new BindingKey<BType> (true, null);

		private BindingKey(bool member, string qualifier) : base(typeof(BType), member, qualifier) { }

		/// <summary>
		/// Get the instance injector.
		/// </summary>
		/// <param name="keyType">Key type.</param>
		/// <param name="qualifier">Qualifier.</param>
		internal static BindingKey Get(string qualifier) {
			return new BindingKey<BType> (false, qualifier);
		}
	}
}
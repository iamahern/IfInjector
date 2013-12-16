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
		private readonly bool isMember;
		private readonly bool isImplicit;
		private readonly string qualifier;

		protected BindingKey(Type bindingType, bool isMember, bool isImplicit, string qualifier) {
			this.bindingType = bindingType;
			this.isMember = isMember;
			this.isImplicit = isImplicit;
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
		public bool IsMember {
			get {
				return isMember;
			}
		}

		/// <summary>
		/// Gets a value indicating whether this instance is implicit. This facet does not affect the key for equals or hashing purposes.
		/// </summary>
		/// <value><c>true</c> if this instance is implicit; otherwise, <c>false</c>.</value>
		public bool IsImplicit {
			get {
				return isImplicit;
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
			return HASHCODE_MULTIPLIER * BindingType.GetHashCode () + IsMember.GetHashCode () + qualifier.GetHashCode();
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
					isMember == obj.isMember &&
					qualifier.Equals(obj.qualifier);
			}
		}

		/// <summary>
		/// Get the implicit version of the current binding key.
		/// </summary>
		/// <returns>The implicit.</returns>
		internal BindingKey ToImplicit() {
			if (IsImplicit) {
				return this;
			}

			return new BindingKey (
				bindingType: BindingType,
				isMember: IsMember,
				isImplicit: true,
				qualifier: Qualifier);
		}

		/// <summary>
		/// Gets a key.
		/// </summary>
		/// <returns>The internal.</returns>
		/// <param name="bindingType">bindingType.</param>
		/// <param name="isMember">If set to <c>true</c> is member.</param>
		/// <param name="qualifier">Qualifier.</param>
		internal static BindingKey Get(Type bindingType, string qualifier = null) {
			return new BindingKey (
				bindingType: bindingType, 
				isMember: false, 
				isImplicit: false, 
				qualifier: qualifier);
		}

		/// <summary>
		/// Get the specified qualifier.
		/// </summary>
		/// <param name="qualifier">Qualifier.</param>
		/// <typeparam name="BType">The 1st type parameter.</typeparam>
		internal static BindingKey Get<BType>(string qualifier = null) where BType : class {
			return Get (typeof(BType), qualifier);
		}

		/// <summary>
		/// Gets a key.
		/// </summary>
		/// <returns>The internal.</returns>
		/// <param name="concreteType">Concrete type.</param>
		/// <param name="isMember">If set to <c>true</c> is member.</param>
		internal static BindingKey GetMember(Type concreteType) {
			return new BindingKey (
				bindingType: concreteType, 
				isMember: true, 
				isImplicit: false, 
				qualifier: null);
		}

		/// <summary>
		/// Gets the member key.
		/// </summary>
		/// <returns>The member.</returns>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		internal static BindingKey GetMember<CType>() {
			return GetMember (typeof(CType));
		}
	}
}
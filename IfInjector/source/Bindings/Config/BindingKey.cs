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
		private static readonly string DELIM = "|";
		private static readonly string TYPE = "Type=";
		private static readonly string PROPERTY = "Property=";

		private static object syncLock = new object();
		private static SafeDictionary<string, BindingKey> bindingKeys = new SafeDictionary<string, BindingKey>(syncLock);

		private string KeyString { get; set; }

		/// <summary>
		/// Gets the type of the binding.
		/// </summary>
		/// <value>The type of the binding.</value>
		public Type BindingType { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this <see cref="IfInjector.IfBinding.BindingKey"/> is a property-only binding.
		/// </summary>
		/// <value><c>true</c> if member; otherwise, <c>false</c>.</value>
		public bool Member { get; private set; }

		/// <summary>
		/// Serves as a hash function for a particular type.
		/// </summary>
		/// <returns>A hash code for this instance that is suitable for use in hashing algorithms and data structures such as a hash table.</returns>
		public override int GetHashCode() {
			return KeyString.GetHashCode();
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
			return obj != null && obj.KeyString == KeyString;
		}

		/// <summary>
		/// Get this instance of the binding key.
		/// </summary>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		public static BindingKey Get<T>() where T : class {
			return BindingKeyInternal<T>.INSTANCE;
		}

		/// <summary>
		/// Get the instance injector.
		/// </summary>
		/// <param name="keyType">Key type.</param>
		public static BindingKey Get(Type keyType) {
			return GetInternal (keyType, false);
		}

		/// <summary>
		/// Gets the properties injector.
		/// </summary>
		/// <returns>The properties injector.</returns>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		internal static BindingKey GetPropertiesInjector<T>() where T : class {
			return BindingKeyInternal<T>.PROPERTIES;
		}

		internal static BindingKey GetInternal(Type keyType, bool isMember) {
			string keyString = 
				TYPE + keyType.FullName + DELIM + PROPERTY + isMember;

			BindingKey key;
			if (!bindingKeys.UnsyncedTryGetValue (keyString, out key)) {
				lock (syncLock) {
					if (!bindingKeys.TryGetValue (keyString, out key)) {
						key = new BindingKey () { 
							KeyString = keyString,
							BindingType = keyType,
							Member = isMember
						};
						bindingKeys.Add (keyString, key);
					}
				}
			}
			return key;
		}

		private static class BindingKeyInternal<T> where T : class {
			public static readonly BindingKey INSTANCE = BindingKey.GetInternal (typeof(T), false);
			public static readonly BindingKey PROPERTIES = BindingKey.GetInternal (typeof(T), true);
		}
	}
}
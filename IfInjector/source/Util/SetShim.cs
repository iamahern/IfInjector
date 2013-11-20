using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

/// <summary>
/// Internal core namespace.
/// </summary>
namespace IfInjector.Util
{
	/// <summary>
	/// Substitute for HashSet on Windows Phone. This object must be locked by the API user.
	/// </summary>
	internal class SetShim<T> : IEnumerable<T>, IEnumerable
	{
		private readonly Dictionary<T, bool> data = new Dictionary<T, bool>();

		public SetShim(IEnumerable<T> collection = null) {
			UnionWith (collection);
		}

		public int Count { get { return data.Count; } }

		public void Add(T item) {
			data [item] = true;
		}

		public bool Remove(T item) {
			return data.Remove (item);
		}

		public void Clear() {
			data.Clear();
		}

		public bool Contains(T item) {
			return data.ContainsKey(item);
		}

		public void CopyTo(T[] array) {
			UnionWith(array);
		}

		public IEnumerator<T> GetEnumerator() {
			return data.Keys.GetEnumerator();
		}

		public void UnionWith(IEnumerable<T> items) {
			if (items != null) {
				foreach (var item in items) {
					Add (item);
				}
			}
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return data.Keys.GetEnumerator();
		}
	}
}
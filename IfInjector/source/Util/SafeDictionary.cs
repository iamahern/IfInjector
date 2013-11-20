using System;
using System.Collections.Generic;

namespace IfInjector.Util
{
	/// <summary>
	/// Thread safe dictionary wrapper. Users supply a synchronization lock to allow for course-grained locking. Course grained locking helps to prevent obscure deadlock issues. 
	/// 
	/// For performance, the dictionary supports [optional] unsynced read operations.
	/// </summary>
	internal class SafeDictionary<TKey,TValue> {
		private readonly object syncLock;
		private readonly Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();
		private Dictionary<TKey, TValue> unsyncDict = new Dictionary<TKey, TValue> ();

		public SafeDictionary(object syncLock) {
			this.syncLock = syncLock;
		}

		public void Add(TKey key, TValue value) {
			lock (syncLock) {
				dict.Add(key, value);
				unsyncDict = new Dictionary<TKey, TValue> (dict);
			}
		}

		public bool TryGetValue(TKey key, out TValue value) {
			lock (syncLock) {
				return dict.TryGetValue (key, out value);
			}
		}

		public bool UnsyncedTryGetValue(TKey key, out TValue value) {
			return unsyncDict.TryGetValue (key, out value);
		}

		public IEnumerable<KeyValuePair<TKey, TValue>> UnsyncedEnumerate() {
			return unsyncDict;
		}

		public bool ContainsKey(TKey key) {
			lock (syncLock) {
				return dict.ContainsKey(key);
			}
		}

		public IEnumerable<TValue> Values {
			get {
				lock (syncLock) {
					// use unsync since that is a copy on write object.
					return unsyncDict.Values;
				}
			}
		}

		public bool Remove(TKey key) {
			lock (syncLock) {
				bool res = dict.Remove (key);
				if (res) {
					unsyncDict = new Dictionary<TKey, TValue> (dict);
				}
				return res;
			}
		}
	}

}
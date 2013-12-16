using System;
using System.Linq;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;
using IfInjector.Errors;
using IfInjector.Util;

namespace IfInjector.Resolver
{
	internal class ImplicitBindingResolver : IBindingResolver
	{
		private readonly SafeDictionary<BindingKey, SetShim<BindingKey>> implicitTypeLookup;

		internal ImplicitBindingResolver (object syncLock) {
			this.implicitTypeLookup = new SafeDictionary<BindingKey, SetShim<BindingKey>> (syncLock);
		}

		/// <inheritdoc/>
		public BindingKey ResolveBinding (BindingKey explicitKey) {
			SetShim<BindingKey> lookup;
			implicitTypeLookup.UnsyncedTryGetValue (explicitKey, out lookup);

			if (lookup == null) {
				return null;
			} else if (lookup.Count == 1) {
				return lookup.First();
			} else  {
				throw InjectorErrors.ErrorAmbiguousBinding.FormatEx(explicitKey.BindingType.Name);
			}
		}

		/// <summary>
		/// Register the specified binding.
		/// </summary>
		/// <param name="binding">Binding.</param>
		internal void Register (BindingKey bindingKey) {
			if (!bindingKey.IsMember && !bindingKey.IsImplicit) {
				implicitTypeLookup.Remove (bindingKey);
				AddImplicitTypes (bindingKey, GetImplicitTypes (bindingKey));
			}
		}

		/// <summary>
		/// Adds the implicit types.
		/// </summary>
		/// <param name="bindingKey">Binding key.</param>
		/// <param name="implicitTypeKeys">Implicit type keys.</param>
		private void AddImplicitTypes(BindingKey bindingKey, SetShim<BindingKey> implicitTypeKeys) {
			foreach(BindingKey implicitTypeKey in implicitTypeKeys) {
				if (BindingAttributeUtils.GetImplementedBy (implicitTypeKey.BindingType) == null) {
					SetShim<BindingKey> newSet, oldSet;

					if (implicitTypeLookup.TryGetValue (implicitTypeKey, out oldSet)) {
						implicitTypeLookup.Remove (implicitTypeKey);
						newSet = new SetShim<BindingKey> (oldSet);
					} else {
						newSet = new SetShim<BindingKey> ();
					}

					newSet.Add (bindingKey);
					implicitTypeLookup.Add (implicitTypeKey, newSet);
				} else {
					return; // TODO - should skip rest?
				}
			}
		}

		/// <summary>
		/// Gets the implicit types.
		/// </summary>
		/// <returns>The implicit types.</returns>
		/// <param name="boundType">Bound type.</param>
		private static SetShim<BindingKey> GetImplicitTypes(BindingKey bindingKey) {
			var implicitTypes = new SetShim<BindingKey>();
			var bindingType = bindingKey.BindingType;

			foreach (Type iFace in bindingType.GetInterfaces()) {
				implicitTypes.Add (BindingKey.Get (iFace));
			}

			Type wTypeChain = bindingType;
			while ((wTypeChain = wTypeChain.BaseType) != null && wTypeChain != typeof(object)) {
				implicitTypes.Add (BindingKey.Get (wTypeChain));
			}

			return implicitTypes;
		}
	}
}


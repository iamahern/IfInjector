using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace FastInjectorMx
{
	/// <summary>
	/// Holder class for errors.
	/// </summary>
	static class ErrorsCodes { 
		internal const string ErrTypeAlreadyDefined = "Attempting to create duplicate binding for type: %1s";

		internal static string Params (this string formatStr, params object[] args) {
			return string.Format (formatStr, args);
		}
	}

	/// <summary>
	/// Abstract operational binding.
	/// </summary>
	internal abstract class AbstractOperationalBinding : IBinderModifiers {
		public bool IsSingleton { get; protected set; }

		public bool IsResolved { get; internal set; }

		public void AsSingleton () {
			IsSingleton = true;
		}

		/// <summary>
		/// Gets the type of the key.
		/// </summary>
		/// <returns>The key type.</returns>
		public abstract Type GetKeyType ();
	}

	/// <summary>
	/// Abstract binding.
	/// </summary>
	internal class AbstractBinding<KeyType> : AbstractOperationalBinding {
		public override Type GetKeyType() { 
			return GetType ().GetGenericArguments () [0];
		}
	}

	/// <summary>
	/// Base type for type based bindings.
	/// </summary>
	internal class TypeBinding<KeyType, ConcreteType> : AbstractBinding<KeyType>
		where ConcreteType : class, KeyType
	{
		// TODO
	}

	internal class Injector : IBinder {
		// TODO thread-safety
//		private readonly Dictionary<Type, Func<Type>> resolvedProviders; 
//		private readonly Dictionary<Type, Func<Type>> toResolveProviders;

		private readonly Dictionary<Type, AbstractOperationalBinding> bindings = new Dictionary<Type, AbstractOperationalBinding>();

		public Injector(Action<IBinder> bindings) {

		}
		
		public IBinderModifiers Bind<KeyType, ConcreteType> () 
			where ConcreteType : class, KeyType {
			return new TypeBinding<KeyType, ConcreteType>();
		}

		public IBinderModifiers Bind<ConcreteType> ()
			where ConcreteType : class {
			return new TypeBinding<ConcreteType, ConcreteType> ();
		}

		public IBinderModifiers BindProvider<KeyType> (IProvider<KeyType> provider) {
			return null; // TODO
		}

		public void BindInstance<KeyType, ConcreteType>(ConcreteType instance)
			where ConcreteType : class, KeyType {
			// TODO
		}

		public void BindInstance<ConcreteType>(ConcreteType instance)
			where ConcreteType : class {
			// TODO
		}

		private BT AddBinding<BT>(BT binding) where BT : AbstractOperationalBinding {
			if (bindings.ContainsKey (binding.GetKeyType())) {
				throw new BindingException (ErrorsCodes.ErrTypeAlreadyDefined.Params(binding.GetKeyType()));
			}
			bindings.Add (binding.GetKeyType(), binding);
			return binding;
		}
	}

}
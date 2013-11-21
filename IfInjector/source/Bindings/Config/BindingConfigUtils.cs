using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using IfInjector.Bindings.Config;
using IfInjector.Bindings.Lifestyles;
using IfInjector.Errors;

namespace IfInjector.Bindings.Config
{
	/// <summary>
	/// Internal utilities for binding classes.
	/// </summary>
	internal static class BindingConfigUtils {
		private static readonly Type ObjectType = typeof(object);

		/// <summary>
		/// Creates the implicit binding settings for the given type.
		/// </summary>
		/// <returns>The implicit binding settings.</returns>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		internal static IBindingConfig CreateImplicitBindingSettings<CType>() where CType : class {
			return MergeImplicitWithExplicitSettings<CType>(new BindingConfig(typeof(CType)));
		}

		/// <summary>
		/// Clones the supplied explicit bindings and merges the settings together with implicit bindings.
		/// </summary>
		/// <returns>The with implicit settings.</returns>
		/// <param name="explicitBindingConfig">Explicit binding config.</param>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		internal static IBindingConfig MergeImplicitWithExplicitSettings<CType>(IBindingConfig explicitBindingConfig) where CType : class {
			// setup implicits
			var bindingConfig = new BindingConfig (typeof(CType));
			var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
			Type cType = typeof(CType); 

			do {
				foreach (var prop in FilterMemberInfo<PropertyInfo>(cType, cType.GetProperties (bindingFlags))) {
					bindingConfig.SetPropertyInfoSetter(prop, null);
				}

				foreach (var field in FilterMemberInfo<FieldInfo>(cType, cType.GetFields (bindingFlags))) {
					bindingConfig.SetFieldInfoSetter(field, null);
				}
			} while ((cType = cType.BaseType) != null && cType != ObjectType);

			if (typeof(CType).GetCustomAttributes (typeof(SingletonAttribute), false).Any()) {
				bindingConfig.Lifestyle = Lifestyle.Singleton;
			}

			MergeBinding(bindingConfig, explicitBindingConfig);
			EnsureConstructoryOrFactory<CType> (bindingConfig);

			// ensure lifestyle
			if (bindingConfig.Lifestyle == null) {
				bindingConfig.Lifestyle = Lifestyle.Transient;
			}

			return bindingConfig;
		}

		private static IEnumerable<MInfo> FilterMemberInfo<MInfo>(Type cType, IEnumerable<MInfo> propsOrFields) 
			where MInfo : MemberInfo 
		{
			return from p in propsOrFields 
				where p.GetCustomAttributes(typeof(InjectAttribute), false).Any()
					select p;
		}

		private static void MergeBinding (IBindingConfig bindingConfig, IBindingConfig explicitBindingConfig) {
			// merge constructor
			if (explicitBindingConfig.Constructor != null) {
				bindingConfig.Constructor = explicitBindingConfig.Constructor;
			}

			// merge factory
			if (explicitBindingConfig.FactoryExpression != null) {
				bindingConfig.FactoryExpression = explicitBindingConfig.FactoryExpression;
			}

			// merge lifestyle
			if (explicitBindingConfig.Lifestyle != null) {
				bindingConfig.Lifestyle = explicitBindingConfig.Lifestyle;
			}

			foreach (var fis in explicitBindingConfig.GetFieldInfoSetters()) {
				bindingConfig.SetFieldInfoSetter (fis.MemberInfo, fis.MemberSetter);
			}

			foreach (var pis in explicitBindingConfig.GetPropertyInfoSetters()) {
				bindingConfig.SetPropertyInfoSetter (pis.MemberInfo, pis.MemberSetter);
			}
		}

		private static void EnsureConstructoryOrFactory<CType>(IBindingConfig bindingConfig) where CType : class {
			var cType = typeof(CType);

			// Do not trigger property change
			if (bindingConfig.FactoryExpression == null && bindingConfig.Constructor == null) {
				if (cType.IsInterface || cType.IsAbstract) {
					// if we can not instantiate, set the resolver to throw an exception.
					Expression<Func<CType>> throwEx = () => ThrowInterfaceException<CType> ();
					bindingConfig.FactoryExpression = throwEx;
				} else {
					// try to find the default constructor and create a default resolver from it
					var ctor = cType.GetConstructors ()
						.OrderBy (v => Attribute.IsDefined (v, typeof(InjectAttribute)) ? 0 : 1)
							.ThenBy (v => v.GetParameters ().Count ())
							.FirstOrDefault ();

					if (ctor != null) {
						bindingConfig.Constructor = ctor;
					} else {
						Expression<Func<CType>> throwEx = () => ThrowConstructorException<CType> ();
						bindingConfig.FactoryExpression = throwEx;
					}
				}
			}
		}

		private static CType ThrowConstructorException<CType>()  where CType : class  {
			throw InjectorErrors.ErrorNoAppropriateConstructor.FormatEx (typeof(CType).FullName);
		}

		private static CType ThrowInterfaceException<CType>() where CType : class {
			throw InjectorErrors.ErrorUnableToResultInterface.FormatEx(typeof(CType).FullName);
		}

		/// <summary>
		/// Adds the property injector to binding config.
		/// </summary>
		/// <param name="bindingConfig">Binding config.</param>
		/// <param name="propertyExpression">Property expression.</param>
		/// <param name="setter">Setter.</param>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		/// <typeparam name="TPropertyType">The 2nd type parameter.</typeparam>
		internal static void AddMemberInjectorToBindingConfig<CType, TPropertyType>(
			IBindingConfig bindingConfig,
			Expression<Func<CType, TPropertyType>> propertyExpression, 
			Expression<Func<TPropertyType>> setter) 
			where CType : class
		{
			var memberExpression = propertyExpression.Body as MemberExpression;
			if (memberExpression == null) {
				throw InjectorErrors.ErrorMustContainMemberExpression.FormatEx ("memberExpression");
			}

			var member = memberExpression.Member;
			if (member is PropertyInfo) {
				bindingConfig.SetPropertyInfoSetter (member as PropertyInfo, setter);
			} else if (member is FieldInfo) {
				bindingConfig.SetFieldInfoSetter (member as FieldInfo, setter);
			} else {
				// Should not be reachable.
				throw InjectorErrors.ErrorMustContainMemberExpression.FormatEx ("memberExpression");
			}
		}
	}
}
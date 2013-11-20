using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Properties
{
	/// <summary>
	/// Properties binding implementation.
	/// </summary>
	internal class PropertiesBinding<CType> : IPropertiesBinding<CType>, IBindingInternal
		where CType : class
	{
		public IBindingConfig BindingConfig { get; private set; }
		public BindingKey BindingKey { get; private set; }
		public Type ConcreteType { get { return typeof(CType); } }

		internal PropertiesBinding() {
			BindingConfig = new BindingConfig(typeof(CType));
			BindingKey = BindingKey.GetPropertiesInjector<CType> ();
		}

		public IPropertiesBinding<CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
			where TPropertyType : class
		{
			return InjectProperty<TPropertyType> (propertyExpression, null);
		}

		public IPropertiesBinding<CType> InjectProperty<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
		{
			BindingConfigUtils.AddPropertyInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, propertyExpression, setter);
			return this;
		}
	}
}
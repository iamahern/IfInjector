using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;
using IfInjector.Bindings.Fluent.Concrete;
using IfInjector.Bindings.Lifestyles;

namespace IfInjector.Bindings.Fluent
{
	/// <summary>
	/// Internal binding implementation.
	/// </summary>
	internal class BoundBinding<BType, CType> : IBoundBinding<BType, CType>, IBindingInternal
		where BType : class
		where CType : class, BType
	{
		public IBindingConfig BindingConfig { get; private set; }
		public BindingKey BindingKey { get; private set; }
		public Type ConcreteType { get { return typeof(CType); } }

		internal BoundBinding() {
			BindingConfig = new BindingConfig(typeof(CType));
			BindingKey = BindingKey<BType>.InstanceKey;
		}

		public IBoundBinding<BType, CType> SetLifestyle (Lifestyle lifestyle) {
			BindingConfig.Lifestyle = lifestyle;
			return this;
		}

		public IBoundBinding<BType, CType> InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> propertyExpression) 
			where TPropertyType : class
		{
			return AddPropertyInjectorInner<TPropertyType> (propertyExpression, null);
		}

		public IBoundBinding<BType, CType> InjectMember<TPropertyType> (
			Expression<Func<CType, TPropertyType>> propertyExpression, 
			Expression<Func<TPropertyType>> setter)
		{
			return AddPropertyInjectorInner<TPropertyType> (propertyExpression, setter);
		}

		private IBoundBinding<BType, CType> AddPropertyInjectorInner<TPropertyType>(Expression<Func<CType, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter) {
			BindingConfigUtils.AddMemberInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, propertyExpression, setter);
			return this;
		}
	}
}
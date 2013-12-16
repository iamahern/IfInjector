using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Members
{
	/// <summary>
	/// Properties binding implementation.
	/// </summary>
	internal class BoundMembersBinding<CType> : IBoundMembersBinding<CType>, IBindingInternal
		where CType : class
	{
		public IBindingConfig BindingConfig { get; private set; }
		public BindingKey BindingKey { get; private set; }
		public Type ConcreteType { get { return typeof(CType); } }

		internal BoundMembersBinding() {
			BindingConfig = new BindingConfig(typeof(CType));
			BindingKey = BindingKey.GetMember<CType>();
		}

		public IBoundMembersBinding<CType> InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> memberExpression) 
			where TPropertyType : class
		{
			return InjectMember<TPropertyType> (memberExpression, null);
		}

		public IBoundMembersBinding<CType> InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> memberExpression, Expression<Func<TPropertyType>> setter)
		{
			BindingConfigUtils.AddMemberInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, memberExpression, setter);
			return this;
		}
	}
}
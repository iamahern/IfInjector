using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Config;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Members
{
	/// <summary>
	/// Properties binding implementation.
	/// </summary>
	internal class MembersBinding<CType> : IMembersBinding<CType>, IBindingInternal
		where CType : class
	{
		public IBindingConfig BindingConfig { get; private set; }
		public BindingKey BindingKey { get; private set; }
		public Type ConcreteType { get { return typeof(CType); } }

		internal MembersBinding() {
			BindingConfig = new BindingConfig(typeof(CType));
			BindingKey = BindingKey.GetMember<CType> ();
		}

		public IMembersBinding<CType> InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> memberExpression) 
			where TPropertyType : class
		{
			return InjectMember<TPropertyType> (memberExpression, null);
		}

		public IMembersBinding<CType> InjectMember<TPropertyType> (Expression<Func<CType, TPropertyType>> memberExpression, Expression<Func<TPropertyType>> setter)
		{
			BindingConfigUtils.AddMemberInjectorToBindingConfig<CType, TPropertyType> (BindingConfig, memberExpression, setter);
			return this;
		}
	}
}
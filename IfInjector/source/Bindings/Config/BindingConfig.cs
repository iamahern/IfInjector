using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using IfInjector.Bindings.Lifestyles;
using IfInjector.Errors;

namespace IfInjector.Bindings.Config
{
	/// <summary>
	/// Binding config. At present time this is not part of the public API. In the future it may be open to allow.
	/// 
	/// Caller's must synchronize access via the syncLock.
	/// </summary>
	internal class BindingConfig : IBindingConfig {
		///////////////
		// Setter Config
		///////////////
		private class MemberSetterConfig<MTInfo> : IMemberSetterConfig<MTInfo> where MTInfo : MemberInfo
		{
			public Type ConcreteType { get; set; }
			public LambdaExpression MemberSetter { get; set; }
			public MTInfo MemberInfo { get; set; }
			public Type MemberType { get; set; }

			public MemberSetterConfig<MTInfo> Validate() {
				if (MemberSetter == null && !MemberType.IsClass && !MemberType.IsInterface) {
					throw InjectorErrors.ErrorUnableToBindNonClassFieldsProperties.FormatEx(MemberInfo.Name, ConcreteType.FullName);
				}
				return this;
			}
		}

		private readonly Dictionary<PropertyInfo, IMemberSetterConfig<PropertyInfo>> propertyInjectors 
			= new Dictionary<PropertyInfo, IMemberSetterConfig<PropertyInfo>>();
		private readonly Dictionary<FieldInfo, IMemberSetterConfig<FieldInfo>> fieldInjectors 
			= new Dictionary<FieldInfo, IMemberSetterConfig<FieldInfo>>();

		internal BindingConfig(Type concreteType) {
			ConcreteType = concreteType;
		}

		public Type ConcreteType { get; private set; }
		public Lifestyle Lifestyle { get; set; }
		public ConstructorInfo Constructor { get; set; }
		public LambdaExpression FactoryExpression { get; set; }

		public void SetPropertyInfoSetter (PropertyInfo propertyInfo, LambdaExpression setter) {
			propertyInjectors [propertyInfo] = new MemberSetterConfig<PropertyInfo> {
				ConcreteType = ConcreteType,
				MemberInfo = propertyInfo,
				MemberType = propertyInfo.PropertyType,
				MemberSetter = setter
			}.Validate();
		}

		public IMemberSetterConfig<PropertyInfo>[] GetPropertyInfoSetters() {
			return propertyInjectors.Values.ToArray ();
		}

		public void SetFieldInfoSetter (FieldInfo fieldInfo, LambdaExpression setter) {
			fieldInjectors [fieldInfo] = new MemberSetterConfig<FieldInfo> {
				ConcreteType = ConcreteType,
				MemberInfo = fieldInfo,
				MemberType = fieldInfo.FieldType,
				MemberSetter = setter
			}.Validate();
		}

		public IMemberSetterConfig<FieldInfo>[] GetFieldInfoSetters() {
			return fieldInjectors.Values.ToArray ();
		}
	}
}
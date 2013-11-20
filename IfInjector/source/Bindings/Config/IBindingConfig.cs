using System;
using System.Reflection;
using System.Linq.Expressions;
using IfInjector.Bindings.Lifestyles;

namespace IfInjector.Bindings.Config
{
	/// <summary>
	/// Binding config. At present time this is not part of the public API. In the future it may be open to allow.
	/// 
	/// Caller's must synchronize access via the syncLock.
	/// </summary>
	internal interface IBindingConfig {

		/// <summary>
		/// Gets or sets the concrete type.
		/// </summary>
		/// <value>The type of the C.</value>
		Type ConcreteType { get; }

		/// <summary>
		/// Gets or sets the lifestyle.
		/// </summary>
		/// <value>The lifestyle.</value>
		Lifestyle Lifestyle { get; set; }

		/// <summary>
		/// Gets or sets the constructor.
		/// </summary>
		/// <value>The constructor.</value>
		ConstructorInfo Constructor { get; set; }

		/// <summary>
		/// Gets or sets the factory expression.
		/// </summary>
		/// <value>The factory expression.</value>
		LambdaExpression FactoryExpression { get; set; }

		/// <summary>
		/// Adds the property info setter.
		/// </summary>
		/// <param name="propertyInfo">Property info.</param>
		/// <param name="setter">This may be null to allow for an implicit binding or a 0-arg LambdaExpression.</param>
		void SetPropertyInfoSetter (PropertyInfo propertyInfo, LambdaExpression setter);

		/// <summary>
		/// Gets the property info setters.
		/// </summary>
		/// <returns>The property info setters.</returns>
		IMemberSetterConfig<PropertyInfo>[] GetPropertyInfoSetters();

		/// <summary>
		/// Adds the field info setter.
		/// </summary>
		/// <param name="fieldInfo">Field info.</param>
		/// <param name="setter">This may be null to allow for an implicit binding or a 0-arg LambdaExpression.</param>
		void SetFieldInfoSetter (FieldInfo fieldInfo, LambdaExpression setter);

		/// <summary>
		/// Gets the field info setters.
		/// </summary>
		/// <returns>The field info setters.</returns>
		IMemberSetterConfig<FieldInfo>[] GetFieldInfoSetters();
	}
}

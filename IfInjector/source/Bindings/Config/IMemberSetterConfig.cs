using System;
using System.Reflection;
using System.Linq.Expressions;

namespace IfInjector.Bindings.Config
{
	/// <summary>
	/// Binding config setter expression.
	/// </summary>
	internal interface IMemberSetterConfig<MTInfo> where MTInfo : MemberInfo
	{
		/// <summary>
		/// Gets the setter.
		/// </summary>
		/// <value>The setter.</value>
		LambdaExpression MemberSetter { get; }

		/// <summary>
		/// Gets the info.
		/// </summary>
		/// <value>The info.</value>
		MTInfo MemberInfo { get; }

		/// <summary>
		/// Gets the type of the member.
		/// </summary>
		/// <value>The type of the member.</value>
		Type MemberType { get; }
	}
}
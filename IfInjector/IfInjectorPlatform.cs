using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace IfInjector
{
	internal partial class InjectorInternal 
	{
		/// <summary>
		/// Substitute for HashSet on Windows Phone.
		/// </summary>
		protected internal class SetShim<T> : IEnumerable<T>, IEnumerable
		{
			private readonly Dictionary<T, bool> data = new Dictionary<T, bool>();

			public SetShim(IEnumerable<T> collection = null) {
				UnionWith (collection);
			}

			public int Count { get { return data.Count; } }

			public void Add(T item) {
				data [item] = true;
			}

			public void Clear() {
				data.Clear();
			}

			public bool Contains(T item) {
				return data.ContainsKey(item);
			}

			public void CopyTo(T[] array) {
				UnionWith(array);
			}

			public IEnumerator<T> GetEnumerator() {
				return data.Keys.GetEnumerator();
			}

			public void UnionWith(IEnumerable<T> items) {
				if (items != null) {
					foreach (var item in items) {
						Add (item);
					}
				}
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return data.Keys.GetEnumerator();
			}
		}

		internal class SetterExpression
		{
			public bool IsResolve() {
				return Setter == null;
			}

			public LambdaExpression Setter { get; set; }
			public MemberInfo Info { get; set; }
			public Type MemberType { get; set; }
		}

		protected internal partial class Resolver<CType> : IResolver 
			where CType : class 
		{
			internal Expression<Func<CType>> CompileFactoryExprSetters(Expression<Func<CType>> factoryExpr)
			{
				return Expression.Lambda<Func<CType>>(Expression.Invoke(CompilePropertiesResolverExpr(), factoryExpr));
			}

			internal Func<CType,CType> CompilePropertiesResolver()
			{
				if (fieldInjectors.Any() || propertyInjectors.Any()) {
					return CompilePropertiesResolverExpr ().Compile ();
				} else {
					return (CType x) => { return x; };
				}
			}

			internal Expression<Func<CType, CType>> CompilePropertiesResolverExpr()
			{
				var instance = Expression.Parameter (typeof(CType), "instanceR");
				var instanceVar = Expression.Variable(typeof(CType));
				var assignExpression = Expression.Assign(instanceVar, instance);

				var blockExpression = new List<Expression> ();
				blockExpression.Add(assignExpression);
				AddFieldSetterExpressions(instanceVar, blockExpression);
				AddPropertySetterExpressions(instanceVar, blockExpression);

				// return val
				blockExpression.Add (instanceVar);

				var expression = Expression.Block(new [] { instanceVar }, blockExpression);

				return Expression.Lambda<Func<CType, CType>>(expression, instance);
			}


			private void AddFieldSetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) 
			{
				foreach (var field in fieldInjectors)
				{
					var valueExpr = GetSetterExpression (field.Value);
					var fieldExpr = CompileFieldSetterExpression (instanceVar, field.Key, valueExpr);
					blockExpressions.Add (fieldExpr);
				}
			}

			private Expression CompileFieldSetterExpression(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
			{
				var valueFunc = Expression.Lambda (valueExpr).Compile();
				Expression<Action<CType>> setValueExpr = inst => CallSetField(inst, fieldInfo, valueFunc);
				return Expression.Invoke(setValueExpr, instanceVar);
			}

			private void CallSetField(CType instance, FieldInfo fieldInfo, Delegate propValue)
			{
				fieldInfo.SetValue (instance, propValue.DynamicInvoke ());
			}

			private void AddPropertySetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) {
				foreach (var prop in propertyInjectors)
				{
					var valueExpr =  GetSetterExpression (prop.Value);
					var setMethod = prop.Key.GetSetMethod(true);
					var propAssignExpr = Expression.Call(instanceVar, setMethod, valueExpr);
					blockExpressions.Add (propAssignExpr);
				}
			}

			private Expression GetSetterExpression(SetterExpression setter) {
				if (setter.IsResolve ()) {
					return GetResolverInvocationExpressionForType (setter.MemberType);
				} else {
					return setter.Setter.Body;
				}
			}
		}
	}
}
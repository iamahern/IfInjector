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

		/// <summary>
		/// The following block of code is specific to providing WP7.5 support. Dropping WP7.5 
		/// provides support for Expression.Var and Expression.Assign, which removes the need 
		/// for the allowances here.
		/// 
		/// If you are not in a WP7.5 environment, you may substitue this for a more 
		/// performant implementation.
		/// </summary>
		protected internal partial class Resolver<CType> : IResolver 
			where CType : class 
		{
			private MethodInfo compileFieldSetterExpressionGeneric;

			private void InitPlatformSupport() {
				Expression<Action> val = () => CompileFieldSetterExpressionGeneric<Exception> (null, null, null);
				compileFieldSetterExpressionGeneric = ((MethodCallExpression)val.Body).Method.GetGenericMethodDefinition();
			}

			private Expression<Func<CType, CType>> CompilePropertiesResolverExpr()
			{
				var instance = Expression.Parameter (typeof(CType), "instanceR");

				var blockExpression = new List<Expression> ();
				AddFieldSetterExpressions(instance, blockExpression);
				AddPropertySetterExpressions(instance, blockExpression);

				var blockFuncs = new List<Action<CType>>(
					from be in blockExpression 
					select Expression.Lambda<Action<CType>>(be, instance).Compile());

				Expression<Func<CType,CType>> setSettersExpr = (CType inst) => CallSetterList (inst, blockFuncs);

				return setSettersExpr;
			}

			private CType CallSetterList(CType instance, List<Action<CType>> setters) {
				foreach (var setter in setters) {
					setter (instance);
				}
				return instance;
			}

			private void AddFieldSetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) 
			{
				foreach (var field in fieldInjectors) {
					var valueExpr = GetSetterValueExpression (field.Value);
					var fieldExpr = CompileFieldSetterExpression (instanceVar, field.Key, valueExpr);
					blockExpressions.Add (fieldExpr);
				}
			}

			private Expression CompileFieldSetterExpression(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
			{
				return (Expression) compileFieldSetterExpressionGeneric.MakeGenericMethod (fieldInfo.FieldType).Invoke (this, new object[]{instanceVar, fieldInfo, valueExpr});
			}

			private Expression CompileFieldSetterExpressionGeneric<TPropertyType>(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
			{
				var valueFunc = Expression.Lambda<Func<TPropertyType>> (valueExpr).Compile();
				Expression<Action<CType>> setValueExpr = inst => CallSetField<TPropertyType>(inst, fieldInfo, valueFunc);
				return Expression.Invoke(setValueExpr, instanceVar);
			}

			private void CallSetField<TPropertyType>(CType instance, FieldInfo fieldInfo, Func<TPropertyType> propValue)
			{
				fieldInfo.SetValue (instance, propValue());
			}

			private void AddPropertySetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) {
				foreach (var prop in propertyInjectors) {
					var valueExpr =  GetSetterValueExpression (prop.Value);
					var setMethod = prop.Key.GetSetMethod(true);
					var propAssignExpr = Expression.Call(instanceVar, setMethod, valueExpr);
					blockExpressions.Add (propAssignExpr);
				}
			}

			private Expression GetSetterValueExpression(SetterExpression setter) {
				if (setter.IsResolve ()) {
					return GetResolverInvocationExpressionForType (setter.MemberType);
				} else {
					return setter.Setter.Body;
				}
			}
		}
	}
}
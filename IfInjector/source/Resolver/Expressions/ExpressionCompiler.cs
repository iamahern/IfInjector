using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

using IfInjector.Bindings.Config;

/// <summary>
/// Internal core namespace.
/// </summary>
namespace IfInjector.Resolver.Expressions
{
	internal class ExpressionCompiler<CType> : IExpressionCompiler<CType> where CType : class {

		private static readonly MethodInfo compileFieldSetterExpressionGeneric;

		static ExpressionCompiler() {
			Expression<Action> val = () => CompileFieldSetterExpressionGeneric<Exception> (null, null, null);
			compileFieldSetterExpressionGeneric = ((MethodCallExpression)val.Body).Method.GetGenericMethodDefinition();
		}

		private readonly IBindingConfig bindingConfig;
		private readonly ResolveResolverExpression resolveResolverExpression;

		private Expression instanceResolverExpression;
		private Func<CType> instanceResolver;
		private Func<CType, CType> propertiesResolver;

		
		internal ExpressionCompiler(IBindingConfig bindingConfig, ResolveResolverExpression resolveResolverExpression) {
			this.bindingConfig = bindingConfig;
			this.resolveResolverExpression = resolveResolverExpression;
		}

		public Func<CType> InstanceResolver {
			get {
				if (instanceResolver == null) {
					var instanceResolverExpressionLambda = Expression.Lambda<Func<CType>>(InstanceResolverExpression);
					instanceResolver = instanceResolverExpressionLambda.Compile ();
				}
				return instanceResolver;
			}
		}

		public Expression InstanceResolverExpression { 
			get {
				if (instanceResolverExpression == null) {
					instanceResolverExpression = CompileResolverExpression ();
				}

				return instanceResolverExpression;
			}
		}

		private Expression CompileResolverExpression() {
			if (bindingConfig.FactoryExpression != null) {
				var factoryExpr = CompileFactoryExpr ();
				var fieldInjectors = bindingConfig.GetFieldInfoSetters ();
				var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

				if (fieldInjectors.Any () || propertyInjectors.Any ()) {
					return CompileFactoryExprSetters (factoryExpr, PropertiesResolver);
				} else {
					return factoryExpr;
				}
			} else {
				return CompileConstructorExpr ();
			}
		}

		private Expression CompileFactoryExpr()
		{
			var arguments = CompileArgumentListExprs(bindingConfig.FactoryExpression.Parameters.Select (x => x.Type));
			return Expression.Invoke (bindingConfig.FactoryExpression, arguments.ToArray());
		}

		private Expression CompileConstructorExpr()
		{
			var arguments = CompileArgumentListExprs(bindingConfig.Constructor.GetParameters().Select(v => v.ParameterType));
			var createInstanceExpression = Expression.New(bindingConfig.Constructor, arguments);

			var fieldInjectors = bindingConfig.GetFieldInfoSetters ();
			var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

			if (fieldInjectors.Any () || propertyInjectors.Any ()) {
				var fields = from iconf in fieldInjectors select Expression.Bind (iconf.MemberInfo, GetSetterValueExpression(iconf));
				var props = from iconf in propertyInjectors select Expression.Bind (iconf.MemberInfo, GetSetterValueExpression(iconf));
				return Expression.MemberInit (createInstanceExpression, fields.Union(props).ToArray());
			} else {
				return createInstanceExpression;
			}
		}

		private List<Expression> CompileArgumentListExprs(IEnumerable<Type> args) {
			var argumentsOut = new List<Expression>();

			foreach (var parameterType in args) {
				var argument = GetResolverInvocationExpressionForType(parameterType);
				argumentsOut.Add(argument);
			}

			return argumentsOut;
		}

		private Expression CompileFactoryExprSetters(Expression factoryExpr, Func<CType, CType> cPropertiesResolver)
		{
			Func<CType> factory = Expression.Lambda<Func<CType>>(factoryExpr).Compile ();
			Expression<Func<CType>> func = () => cPropertiesResolver(factory());
			return func.Body;
		}

		private Expression GetSetterValueExpression<MIType>(IMemberSetterConfig<MIType> setter) where MIType : MemberInfo {
			if (IsResolve (setter)) {
				return GetResolverInvocationExpressionForType (setter.MemberType);
			} else {
				return setter.MemberSetter.Body;
			}
		}

		private bool IsResolve<MIType>(IMemberSetterConfig<MIType> setter) where MIType : MemberInfo {
			return setter.MemberSetter == null;
		}

		private Expression GetResolverInvocationExpressionForType(Type parameterType) {
			return resolveResolverExpression(BindingKey.Get(parameterType));
		}

		private void AddPropertySetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) {
			var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

			foreach (var prop in propertyInjectors) {
				var valueExpr =  GetSetterValueExpression (prop);
				var setMethod = prop.MemberInfo.GetSetMethod(true);
				var propAssignExpr = Expression.Call(instanceVar, setMethod, valueExpr);
				blockExpressions.Add (propAssignExpr);
			}
		}

		private static CType CallSetterList(CType instance, List<Action<CType>> setters) {
			foreach (var setter in setters) {
				setter (instance);
			}
			return instance;
		}

		private void AddFieldSetterExpressions(ParameterExpression instanceVar, List<Expression> blockExpressions) 
		{
			var fieldInjectors = bindingConfig.GetFieldInfoSetters ();

			foreach (var field in fieldInjectors) {
				var valueExpr = GetSetterValueExpression (field);
				var fieldExpr = CompileFieldSetterExpression (instanceVar, field.MemberInfo, valueExpr);
				blockExpressions.Add (fieldExpr);
			}
		}

		private static Expression CompileFieldSetterExpression(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
		{
			return (Expression) compileFieldSetterExpressionGeneric.MakeGenericMethod (fieldInfo.FieldType).Invoke (null, new object[]{instanceVar, fieldInfo, valueExpr});
		}

		private static Expression CompileFieldSetterExpressionGeneric<TPropertyType>(ParameterExpression instanceVar, FieldInfo fieldInfo, Expression valueExpr)
		{
			var valueFunc = Expression.Lambda<Func<TPropertyType>> (valueExpr).Compile();
			Expression<Action<CType>> setValueExpr = inst => CallSetField<TPropertyType>(inst, fieldInfo, valueFunc);
			return Expression.Invoke(setValueExpr, instanceVar);
		}

		private static void CallSetField<TPropertyType>(CType instance, FieldInfo fieldInfo, Func<TPropertyType> propValue)
		{
			fieldInfo.SetValue (instance, propValue());
		}

		public Func<CType,CType> PropertiesResolver {
			get {
				if (propertiesResolver == null) {
					propertiesResolver = CompilePropertiesResolver();
				}

				return propertiesResolver;
			}
		}

		private Func<CType,CType> CompilePropertiesResolver()
		{
			var fieldInjectors = bindingConfig.GetFieldInfoSetters ();
			var propertyInjectors = bindingConfig.GetPropertyInfoSetters ();

			if (fieldInjectors.Length > 0 || propertyInjectors.Length > 0) {
				return CompilePropertiesResolverExpr ().Compile ();
			} else {
				return (CType x) => { return x; };
			}
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
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

using IfFastInjector.IfInjectorTypes;

/// <summary>
/// NOTE: no actual implementation
/// I was was coding these interfaces before rebasing on top of 'fFastInjector' to use as a starting point.
/// 
/// I intend to merge the good parts of this with IFFastInjector
/// </summary>
namespace IfFastInjector
{
	/// <summary>
	/// Static encapsulation class to restict external access of components.
	/// </summary>
	internal abstract class IfFastInjectorInternal 
	{
		/// <summary>Internal resolver type.</summary>
		protected internal interface IInternalResolver {
			object DoResolve();
		}

		/// <summary>Utility to allow for locking granularity at the type construct level. Also simplifies map management.</summary>
		protected internal class InjectorTypeConstructs {
			public IInternalResolver InternalResolver { get; set; }
			public bool IsRecursionTestPending { get; set; }
			public bool IsInternalResolverPending { get; set; }
		}

		/// <summary>
		/// The actual injector implementation.
		/// </summary>
		internal class InjectorInternal : IfInjector
		{		
			// Thread safety via lock (internalResolvers) 
			private readonly SafeDictionary<Type, InjectorTypeConstructs> typeConstructs = new SafeDictionary<Type, InjectorTypeConstructs>();
			private readonly SafeDictionary<Type, ISet<Type>> implicitTypeLookup = new SafeDictionary<Type, ISet<Type>> ();

			protected internal readonly MethodInfo GenericResolve;
			private readonly MethodInfo genericResolveClosure;

			public InjectorInternal() 
			{
				Expression<Func<Exception>> TmpResolveExpression = () => this.Resolve<Exception>();
				this.GenericResolve = ((MethodCallExpression)TmpResolveExpression.Body).Method.GetGenericMethodDefinition();

				Expression<Func<Expression>> TmpResolveClosureExpression = () => this.ResolveClosure<Exception>();
				this.genericResolveClosure = ((MethodCallExpression)TmpResolveClosureExpression.Body).Method.GetGenericMethodDefinition();
			}

			public override T Resolve<T>()
			{
				var typeT = typeof(T);
				ISet<Type> lookup;

				if (typeConstructs.ContainsKey (typeT) || !((implicitTypeLookup.TryGetValue (typeT, out lookup) && lookup.Count > 0))) {
					return (T) GetInternalResolver (typeT, typeT, false).DoResolve();
				} else {
					if (lookup.Count == 1) {
						return (T) Resolve (lookup.First());
					} else {
						throw new IfFastInjectorException (string.Format(IfFastInjectorErrors.ErrorAmbiguousBinding, typeT.Name));
					}
				} 

				// TODO - change implicit resolution strategy
			}

			public override object Resolve(Type type)
			{
				InjectorTypeConstructs constructs;

				if (typeConstructs.TryGetValue(type, out constructs) && constructs.InternalResolver != null) {
					return constructs.InternalResolver.DoResolve();
				}

				// Not in dictionary, call Resolve<T> which will, in turn, set up and call the default Resolver
				return GenericResolve.MakeGenericMethod(type).Invoke(this, new object[0]);
			}

			protected internal Expression ResolveClosure(Type type) {
				return (Expression) genericResolveClosure.MakeGenericMethod(type).Invoke(this, new object[0]);
			}

			private Expression ResolveClosure<T>() where T : class {
				Expression<Func<T>> exp = () => Resolve<T> ();
				return exp.Body;
			}

			public override IfFastInjectorBinding<TConcreteType> Bind<T, TConcreteType>()
			{
				var iResolver = GetInternalResolverAndBind<T, TConcreteType> ();
				return new InjectorFluent<TConcreteType>(iResolver);
			}

			public override IfFastInjectorBinding<TConcreteType> Bind<TConcreteType> () {
				var iResolver = GetInternalResolverAndBind<TConcreteType, TConcreteType> ();
				return new InjectorFluent<TConcreteType>(iResolver);
			}

			public override IfFastInjectorBinding<T> Bind<T> (Expression<Func<T>> factoryExpression)
			{
				var iResolver = GetInternalResolverAndBind<T, T> ();
				iResolver.SetResolver(factoryExpression);
				return new InjectorFluent<T>(iResolver);
			}


			private InternalResolver<CType> GetInternalResolverAndBind<BType, CType>()
				where BType : class
				where CType : class, BType
			{
				return (InternalResolver<CType>) GetInternalResolver (typeof(BType), typeof(CType), true);
			}

			// TODO - review

			/// <summary>
			/// Gets the internal resolver.  The implType is only used in the case of new or implicit bindings.  TODO: seperate method for implicit binding...
			/// </summary>
			/// <returns>The internal resolver.</returns>
			/// <param name="bindType">Bind type.</param>
			/// <param name="implType">Impl type.</param>
			/// <param name="isNewBinding">If set to <c>true</c> is new binding.</param>
			private IInternalResolver GetInternalResolver(Type bindType, Type implType, bool isNewBinding) {
				InjectorTypeConstructs typeConstruct = GetTypeConstruct (bindType);

				lock (typeConstruct) {
					if (typeConstruct.InternalResolver == null || (isNewBinding && typeConstruct.InternalResolver.GetType().GetGenericArguments()[0] != implType)) {
						if (typeConstruct.IsInternalResolverPending) {
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, bindType.Name));
						}

						Type iResolverType = typeof(InternalResolver<>);
						Type genericType = iResolverType.MakeGenericType(new Type[] { implType });

						typeConstruct.IsInternalResolverPending = true;
						typeConstruct.InternalResolver = CreateInstance (genericType, this, typeConstruct);
					} 

					return typeConstruct.InternalResolver;
				}
			}

			private IInternalResolver CreateInstance(Type type, params object[] args) {
				try {
					return (IInternalResolver) Activator.CreateInstance(type, args);
				} catch (TargetInvocationException ex) {
					throw ex.InnerException;
				}
			}

			private InjectorTypeConstructs GetTypeConstruct(Type type) {
				return typeConstructs.GetWithInitial(type, () => {
					foreach (Type iFace in type.GetInterfaces()) {
						AddImplicitType(iFace, type);
					}

					Type wTypeChain = type;
					while ((wTypeChain = wTypeChain.BaseType) != null && wTypeChain != typeof(object)) {
						AddImplicitType(wTypeChain, type);
					}

					return new InjectorTypeConstructs();
				});
			}

			private void AddImplicitType(Type bType, Type cType) {
				ISet<Type> mSet = implicitTypeLookup.GetWithInitial(bType, () => new HashSet<Type>());
				lock (mSet) {
					mSet.Add (cType);
				}
			}
		}

		/// <summary>
		/// The Injector Internal.
		/// </summary>
		protected internal class InternalResolver<T> : IInternalResolver
			where T : class
		{
			private bool isVerifiedNotRecursive;

			private readonly Type typeofT = typeof(T);
			private readonly List<SetterExpression> setterExpressions = new List<SetterExpression>();
			private Func<T> resolverFactoryCompiled;
			private Func<T> resolve; // TODO - Ok as not locked?

			private bool singleton = false;

			private readonly InjectorInternal MyInjector;
			private readonly InjectorTypeConstructs myTypeConstructs;

			public InternalResolver(InjectorInternal injector, InjectorTypeConstructs typeConstructs) {
				this.MyInjector = injector;
				this.myTypeConstructs = typeConstructs;
				this.resolve = InitInitialResolver();
			}

			public T DoResolveTyped() {
				return resolve ();
			}

			public object DoResolve() {
				return DoResolveTyped ();
			}

			private Func<T> InitInitialResolver()
			{
				if (typeofT.IsInterface || typeofT.IsAbstract)
				{
					// if we can not instantiate, set the resolver to throw an exception.
					// this resolver will be replaced when the type is configured
					return SetResolverInner(() => ThrowInterfaceException());
				}
				else
				{
					// try to find the default constructor and create a default resolver from it
					return SetDefaultConstructor();
				}
			}

			/// <summary>
			/// Return an InvocationExpression for Resolver of type T
			/// </summary>
			/// <returns></returns>
			private Expression GetResolverInvocationExpression()
			{
				Expression<Func<T>> expressionForResolverLambda = () => resolve();
				return (Expression)expressionForResolverLambda.Body;
			}

			/// <summary>
			/// Return an InvocationExpression for Resolver of type parameterType
			/// </summary>
			/// <param name="parameterType"></param>
			/// <returns></returns>
			private Expression GetResolverInvocationExpressionForType(Type parameterType)
			{
				return MyInjector.ResolveClosure (parameterType);
			}

			private T ThrowInterfaceException()
			{
				throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorUnableToResultInterface, typeof(T).FullName));
			}

			private class SetterExpression
			{
				public MemberExpression PropertyMemberExpression { get; set; }
				public LambdaExpression Setter { get; set; }
			}

			/// <summary>
			/// Expression to construct new instance of class
			/// </summary>
			private Expression<Func<T>> ResolverFactoryExpression { get; set; }

			/// <summary>
			/// Expression to construct new instance of class and set members or other operations
			/// </summary>
			private Expression<Func<T>> ResolverExpression { get; set; }

			/// <summary>
			/// Sets the resolver.
			/// </summary>
			/// <param name="factoryExpression">Factory expression.</param>
			public void SetResolver(Expression<Func<T>> factoryExpression)
			{
				lock (this) {
					var visitor = new ReplaceMethodCallWithInvocationExpressionVisitor<T>(this);
					var newFactoryExpression = (Expression<Func<T>>)visitor.Visit(factoryExpression);
					SetResolverInner(newFactoryExpression);
				}
			}

			/// <summary>
			/// Sets the resolver.
			/// </summary>
			/// <param name="constructor">Constructor.</param>
			public void SetResolver(ConstructorInfo constructor)
			{
				lock (this) {
					SetConstructor(constructor);
				}
			}

			private Func<T> SetResolverInner(Expression<Func<T>> factoryExpression)
			{
				ResolverFactoryExpression = factoryExpression;
				return CompileResolver();
			}

			/// <summary>
			/// Add property setter for property, use the Resolver to determine the value of the property
			/// </summary>
			/// <typeparam name="TPropertyType"></typeparam>
			/// <param name="propertyExpression"></param>
			public void AddPropertySetter<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				lock (this) {
					Expression<Func<TPropertyType>> setter = () => MyInjector.Resolve<TPropertyType>();
					AddPropertySetterInner<TPropertyType>(propertyExpression, setter);
				}
			}

			/// <summary>
			/// Add property setter for the property, compile and use the expression for the value of the property
			/// </summary>
			/// <typeparam name="TPropertyType"></typeparam>
			/// <param name="propertyExpression"></param>
			/// <param name="setter"></param>
			public void AddPropertySetter<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
				where TPropertyType : class
			{
				lock (this) {
					AddPropertySetterInner<TPropertyType>(propertyExpression, setter);
				}
			}

			private void AddPropertySetterInner<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
			{
				var propertyMemberExpression = propertyExpression.Body as MemberExpression;
				if (propertyMemberExpression == null) {
					throw new ArgumentException(IfFastInjectorErrors.ErrorMustContainMemberExpression, "propertyExpression");
				}

				setterExpressions.Add(new SetterExpression { PropertyMemberExpression = propertyMemberExpression, Setter = setter });

				CompileResolver();
			}

			public void AsSingleton() {
				lock (this) {
					singleton = true;
					CompileResolver ();
				}
			}

			/// <summary>
			/// Compile the resolver expression
			/// If any setter expressions are used, build an expression that creates the object and then sets the properties before returning it,
			/// otherwise, use the simpler expression that just returns the object
			/// </summary>
			private Func<T> CompileResolver()
			{
				// if no property expressions, then just use the unmodified ResolverFactoryExpression
				if (setterExpressions.Any())
				{
					var resolver = ResolverFactoryExpression;

					var variableExpression = Expression.Variable(typeofT);
					var assignExpression = Expression.Assign(variableExpression, resolver.Body);

					// begin list of expressions for block expression
					var blockExpression = new List<Expression>();
					blockExpression.Add(assignExpression);

					// setters
					foreach (var v in setterExpressions)
					{
						var propertyExpression = Expression.Property(variableExpression, (PropertyInfo)v.PropertyMemberExpression.Member);
						var propertyAssignExpression = Expression.Assign(propertyExpression, v.Setter.Body);
						blockExpression.Add(propertyAssignExpression);
					}

					// return value
					blockExpression.Add(variableExpression);

					var expression = Expression.Block(new ParameterExpression[] { variableExpression }, blockExpression);

					ResolverExpression = (Expression<Func<T>>)Expression.Lambda(expression, resolver.Parameters);
				}
				else
				{
					ResolverExpression = ResolverFactoryExpression;
				}

				this.isVerifiedNotRecursive = false;

				// Handle singleton
				if (singleton) {
					var lazySingle = new Lazy<T> (ResolverExpression.Compile ());
					resolverFactoryCompiled = () => lazySingle.Value;
				} else {
					resolverFactoryCompiled = ResolverExpression.Compile ();
				}

				resolve = ResolveWithRecursionCheck;

				return ResolveWithRecursionCheck;
			}

			/// <summary>
			/// Convert expression of Func T to expression of Func object
			/// </summary>
			/// <param name="func"></param>
			/// <returns></returns>
			private Expression<Func<object>> ConvertFunc(Expression<Func<T>> func)
			{
				return (Expression<Func<object>>)Expression.Lambda(Expression.Convert(func.Body, typeof(object)), func.Parameters);
			}

			private bool IsRecursionTestPending {
				get {
					return myTypeConstructs.IsRecursionTestPending;
				}
				set {
					myTypeConstructs.IsRecursionTestPending = value;
				}
			}

			private T ResolveWithRecursionCheck()
			{
				// Lock until executed once; we will compile this away once verified
				lock (this) {
					if (!isVerifiedNotRecursive)
					{
						if (IsRecursionTestPending)
						{
							throw new IfFastInjectorException(string.Format(IfFastInjectorErrors.ErrorResolutionRecursionDetected, typeofT.Name));
						}
						IsRecursionTestPending = true;
					}

					var retval = resolverFactoryCompiled();

					isVerifiedNotRecursive = true;
					IsRecursionTestPending = false;
					resolve = resolverFactoryCompiled;
					return retval;
				}
			}

			/// <summary>
			/// Get the constructor with the fewest number of parameters and create a factory for it
			/// </summary>
			private Func<T> SetDefaultConstructor()
			{
				// get first available constructor ordered by parameter count ascending
				var constructor = typeofT.GetConstructors().Where(v => Attribute.IsDefined(v, typeof(IfIgnoreConstructorAttribute)) == false).OrderBy(v => Attribute.IsDefined(v, typeof(IfInjectAttribute)) ? 0 : 1).ThenBy(v => v.GetParameters().Count()).FirstOrDefault();

				if (constructor != null)
				{
					return SetConstructor(constructor);
				}

				return null;
			}

			/// <summary>
			/// Create an expression to create this type from the passed-in constructor
			/// </summary>
			/// <param name="constructor"></param>
			private Func<T> SetConstructor(ConstructorInfo constructor)
			{
				var methodParameters = constructor.GetParameters().Select(v => v.ParameterType).ToArray();

				var arguments = new List<Expression>();

				foreach (var parameterType in methodParameters)
				{
					var argument = GetResolverInvocationExpressionForType(parameterType);
					arguments.Add(argument);
				}

				Expression createInstanceExpression = Expression.New(constructor, arguments);

				return SetResolverInner((Expression<Func<T>>)Expression.Lambda(createInstanceExpression));
			}

			private class ReplaceMethodCallWithInvocationExpressionVisitor<RMT> : ExpressionVisitor
				where RMT : class
			{
				private readonly InternalResolver<RMT> resolver;
				private readonly MethodInfo resolverMethod;

				public ReplaceMethodCallWithInvocationExpressionVisitor(InternalResolver<RMT> resolver)
				{
					this.resolver = resolver;
					this.resolverMethod = resolver.MyInjector.GenericResolve;
				}

				protected override Expression VisitMethodCall(MethodCallExpression node)
				{
					var method = node.Method;
					if (method.IsGenericMethod && method.GetGenericMethodDefinition() == resolverMethod)
					{
						var parameterType = method.GetGenericArguments()[0];
						return resolver.GetResolverInvocationExpressionForType(parameterType);
					}
					else
					{
						return base.VisitMethodCall(node);
					}
				}
			}
		}

		/// <summary>
		/// The fluent class is really only important to give the extension methods the type for T
		/// </summary>
		/// <typeparam name="T"></typeparam>
		protected internal class InjectorFluent<T> : IfFastInjectorBinding<T>
			where T : class 
		{ 
			private readonly InternalResolver<T> resolver;

			protected internal InjectorFluent(InternalResolver<T> resolver) {
				this.resolver = resolver;
			}

			public IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
				where TPropertyType : class
			{
				resolver.AddPropertySetter(propertyExpression);
				return this;
			}

			public IfFastInjectorBinding<T> AddPropertyInjector<TPropertyType> (Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
				where TPropertyType : class 
			{
				resolver.AddPropertySetter(propertyExpression, setter);
				return this;
			}

			public void AsSingleton () {
				resolver.AsSingleton ();
			}
		}

		/// <summary>
		/// Thread safe dictionary wrapper. 
		/// </summary>
		protected internal class SafeDictionary<TKey,TValue> {
			private readonly object syncLock = new object();
			private readonly Dictionary<TKey, TValue> dict = new Dictionary<TKey, TValue>();

			public TValue GetWithInitial(TKey key, Func<TValue> initializer) 
			{
				lock (syncLock) 
				{
					TValue value;
					if (!dict.TryGetValue (key, out value)) 
					{
						dict [key] = value = initializer.Invoke ();
					}

					return value;
				}
			}

			public bool TryGetValue(TKey key, out TValue value) 
			{
				lock (syncLock) 
				{
					return dict.TryGetValue (key, out value);
				}
			}

			public Boolean ContainsKey(TKey key) 
			{
				lock (syncLock) 
				{
					return dict.ContainsKey(key);
				}
			}
		}
	}
}
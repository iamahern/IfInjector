using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace fFastInjector
{
    public static class Injector
    {
        internal static Dictionary<Type, Func<object>> _resolvers = new Dictionary<Type, Func<object>>();
        static Expression<Func<Exception>> TmpResolveExpression = () => Injector.Resolve<Exception>();
        static MethodInfo GenericResolve = ((MethodCallExpression)TmpResolveExpression.Body).Method.GetGenericMethodDefinition();

        static Injector()
        {
            ExceptionType = typeof(fFastInjectorException);
        }

        public static Type ExceptionType { get; set; }

        public static T Resolve<T>()
            where T : class
        {
            return InternalResolver<T>.Resolve();
        }

        public static object Resolve(Type type)
        {
            Func<object> resolver;
            if (_resolvers.TryGetValue(type, out resolver))
            {
                return resolver();
            }

            // Not in dictionary, call Resolve<T> which will, in turn, set up and call the default Resolver
            return GenericResolve.MakeGenericMethod(type).Invoke(null, new object[0]);
        }

        public static InjectorFluent<T> SetResolver<T, TConcreteType>()
            where T : class
            where TConcreteType : class,T
        {
            InternalResolver<T>.SetResolver(() => InternalResolver<TConcreteType>.Resolve());
            return new InjectorFluent<T>();
        }

        public static InjectorFluent<T> SetResolver<T>(ConstructorInfo constructor)
            where T : class
        {
            InternalResolver<T>.SetResolver(constructor);
            return new InjectorFluent<T>();
        }

        public static InjectorFluent<T> SetResolver<T>(Expression<Func<T>> factoryExpression)
            where T : class
        {
            InternalResolver<T>.SetResolver(factoryExpression);
            return new InjectorFluent<T>();
        }

        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
            where T : class
            where TPropertyType : class
        {
            InternalResolver<T>.AddPropertySetter(propertyExpression);
            return new InjectorFluent<T>();
        }

        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
            where T : class
        {
            InternalResolver<T>.AddPropertySetter(propertyExpression, setter);
            return new InjectorFluent<T>();
        }

        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(this InjectorFluent<T> fluent, Expression<Func<T, TPropertyType>> propertyExpression)
            where T : class
            where TPropertyType : class
        {
            InternalResolver<T>.AddPropertySetter(propertyExpression);
            return fluent;
        }

        public static InjectorFluent<T> AddPropertyInjector<T, TPropertyType>(this InjectorFluent<T> fluent, Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
            where T : class
        {
            InternalResolver<T>.AddPropertySetter(propertyExpression, setter);
            return fluent;
        }

        /// <summary>
        /// The fluent class is really only important to give the extension methods the type for T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public class InjectorFluent<T> where T : class { }

        public class SelectDuringRegistrationAttribute : Attribute { }
        public class IgnoreDuringRegistrationAttribute : Attribute { }

        public class fFastInjectorException : Exception
        {
            public fFastInjectorException() : base() { }
            public fFastInjectorException(string message) : base(message) { }
            public fFastInjectorException(string message, Exception innerException) : base(message, innerException) { }
        }

        internal static class InternalResolver<T>
          where T : class
        {
            internal const string ErrorResolutionRecursionDetected = "Resolution recursion detected.  Resolve<{0}> is called by a dependency of Resolve<{0}> leading to an infinite loop.";
            internal const string ErrorUnableToResultInterface = "Error on {0}. Unable to resolve Interface and Abstract classes without a configuration.";
            internal const string ErrorMustContainMemberExpression = "Must contain a MemberExpression";

            static readonly Type typeofT = typeof(T);
            static readonly List<SetterExpression> setterExpressions = new List<SetterExpression>();
            static Func<T> resolverFactoryCompiled;
            internal static Func<T> Resolve = InitInitialResolver();
            static bool isVerifiedNotRecursive;

            [ThreadStatic]
            static bool isRecursionTestPending;

            static Func<T> InitInitialResolver()
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
            static InvocationExpression GetResolverInvocationExpression()
            {
                Expression<Func<T>> expressionForResolverLambda = () => Resolve();
                return (InvocationExpression)expressionForResolverLambda.Body;
            }

            /// <summary>
            /// Return an InvocationExpression for Resolver of type parameterType
            /// </summary>
            /// <param name="parameterType"></param>
            /// <returns></returns>
            static InvocationExpression GetResolverInvocationExpressionForType(Type parameterType)
            {
                var type = typeof(InternalResolver<>).MakeGenericType(parameterType);

                Expression<Func<InvocationExpression>> method = () => GetResolverInvocationExpression();
                var methodName = ((MethodCallExpression)method.Body).Method.Name;

                return (InvocationExpression)type.GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[0]);
            }

            static T ThrowInterfaceException()
            {
                throw CreateException(string.Format(ErrorUnableToResultInterface, typeof(T).FullName));
            }

            static Exception CreateException(string message, Exception innerException = null)
            {
                return (Exception)Activator.CreateInstance(ExceptionType, message, innerException);
            }

            class SetterExpression
            {
                public MemberExpression PropertyMemberExpression { get; set; }
                public LambdaExpression Setter { get; set; }
            }

            /// <summary>
            /// Expression to construct new instance of class
            /// </summary>
            public static Expression<Func<T>> ResolverFactoryExpression { get; private set; }

            /// <summary>
            /// Expression to construct new instance of class and set members or other operations
            /// </summary>
            public static Expression<Func<T>> ResolverExpression { get; private set; }

            public static void SetResolver(Expression<Func<T>> factoryExpression)
            {
                var visitor = new ReplaceMethodCallWithInvocationExpressionVisitor();
                var newFactoryExpression = (Expression<Func<T>>)visitor.Visit(factoryExpression);
                SetResolverInner(newFactoryExpression);
            }

            public static void SetResolver(ConstructorInfo constructor)
            {
                SetConstructor(constructor);
            }

            private static Func<T> SetResolverInner(Expression<Func<T>> factoryExpression)
            {
                ResolverFactoryExpression = factoryExpression;
                return CompileResolver();
            }

            /// <summary>
            /// Add property setter for property, use the Resolver to determine the value of the property
            /// </summary>
            /// <typeparam name="TPropertyType"></typeparam>
            /// <param name="propertyExpression"></param>
            public static void AddPropertySetter<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression)
                where TPropertyType : class
            {
                Expression<Func<TPropertyType>> setter = () => InternalResolver<TPropertyType>.Resolve();
                AddPropertySetterInner<TPropertyType>(propertyExpression, setter);
            }

            /// <summary>
            /// Add property setter for the property, compile and use the expression for the value of the property
            /// </summary>
            /// <typeparam name="TPropertyType"></typeparam>
            /// <param name="propertyExpression"></param>
            /// <param name="setter"></param>
            public static void AddPropertySetter<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
            {
                AddPropertySetterInner<TPropertyType>(propertyExpression, setter);
            }

            private static void AddPropertySetterInner<TPropertyType>(Expression<Func<T, TPropertyType>> propertyExpression, Expression<Func<TPropertyType>> setter)
            {
                var propertyMemberExpression = propertyExpression.Body as MemberExpression;
                if (propertyMemberExpression == null)
                {
                    throw new ArgumentException(ErrorMustContainMemberExpression, "propertyExpression");
                }

                setterExpressions.Add(new SetterExpression { PropertyMemberExpression = propertyMemberExpression, Setter = setter });

                CompileResolver();
            }

            /// <summary>
            /// Compile the resolver expression
            /// If any setter expressions are used, build an expression that creates the object and then sets the properties before returning it,
            /// otherwise, use the simpler expression that just returns the object
            /// </summary>
            private static Func<T> CompileResolver()
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

                isVerifiedNotRecursive = false;

                resolverFactoryCompiled = ResolverExpression.Compile();
                Resolve = ResolveWithRecursionCheck;

                Injector._resolvers[typeofT] = ResolveWithRecursionCheck;

                return ResolveWithRecursionCheck;
            }

            /// <summary>
            /// Convert expression of Func T to expression of Func object
            /// </summary>
            /// <param name="func"></param>
            /// <returns></returns>
            static Expression<Func<object>> ConvertFunc(Expression<Func<T>> func)
            {
                return (Expression<Func<object>>)Expression.Lambda(Expression.Convert(func.Body, typeof(object)), func.Parameters);
            }

            static T ResolveWithRecursionCheck()
            {
                if (!isVerifiedNotRecursive)
                {
                    if (isRecursionTestPending)
                    {
                        throw CreateException(string.Format(ErrorResolutionRecursionDetected, typeofT.Name));
                    }
                    isRecursionTestPending = true;
                }

                var retval = resolverFactoryCompiled();

                isVerifiedNotRecursive = true;
                isRecursionTestPending = false;
                Resolve = resolverFactoryCompiled;
                Injector._resolvers[typeofT] = ConvertFunc(ResolverExpression).Compile();
                return retval;
            }

            /// <summary>
            /// Get the constructor with the fewest number of parameters and create a factory for it
            /// </summary>
            private static Func<T> SetDefaultConstructor()
            {
                // get first available constructor ordered by parameter count ascending
                var constructor = typeofT.GetConstructors().Where(v => Attribute.IsDefined(v, typeof(IgnoreDuringRegistrationAttribute)) == false).OrderBy(v => Attribute.IsDefined(v, typeof(SelectDuringRegistrationAttribute)) ? 0 : 1).ThenBy(v => v.GetParameters().Count()).FirstOrDefault();

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
            private static Func<T> SetConstructor(ConstructorInfo constructor)
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

            class ReplaceMethodCallWithInvocationExpressionVisitor : ExpressionVisitor
            {
                MethodInfo _resolverMethod;

                public ReplaceMethodCallWithInvocationExpressionVisitor()
                {
                    _resolverMethod = Injector.GenericResolve;
                }

                protected override Expression VisitMethodCall(MethodCallExpression node)
                {
                    var method = node.Method;
                    if (method.IsGenericMethod && method.GetGenericMethodDefinition() == _resolverMethod)
                    {
                        var parameterType = method.GetGenericArguments()[0];
                        return GetResolverInvocationExpressionForType(parameterType);
                    }
                    else
                    {
                        return base.VisitMethodCall(node);
                    }
                }
            }
        }
    }
}

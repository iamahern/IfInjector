using System;
using System.Linq.Expressions;

using IfInjector.Resolver.Expressions;

namespace IfInjector.Bindings.Lifestyles
{
	/// <summary>
	/// Base lifestyle class
	/// </summary>
	public abstract class Lifestyle {

		/// <summary>
		/// Delegate to create custom lifestyle.
		/// </summary>
		/// <example>
		/// var customLifestyle = Lifestyle.CreateCustom(instanceResolver => {
		/// 	ThreadLocal<object> instance = new ThreadLocal<object>(instanceResolver);
		///
		///		return () => {
		///			return instance.Value;
		///		}
		///	});
		/// </example>
		public delegate Func<object> CustomLifestyleDelegate(Func<object> instanceResolver);

		/// <summary>
		/// The singleton lifestyle constant.
		/// </summary>
		public static readonly Lifestyle Singleton = new SingletonLifestyle();

		/// <summary>
		/// The transient liestyle constant
		/// </summary>
		public static readonly Lifestyle Transient = new TransientLifestyle();

		/// <summary>
		/// Gets the lifestyle resolver.
		/// </summary>
		/// <returns>The lifestyle resolver.</returns>
		/// <param name="syncLock">Sync lock.</param>
		/// <param name="resolverExpression">Resolver expression.</param>
		/// <param name="resolverExpressionCompiled">Resolver expression compiled.</param>
		/// <param name="testInstance">Test instance.</param>
		/// <typeparam name="CType">The 1st type parameter.</typeparam>
		internal abstract LifestyleResolver<CType> GetLifestyleResolver<CType>(
				object syncLock, 
				IExpressionCompiler<CType> expressionCompiler,
				CType testInstance) 
			where CType : class;

		/// <summary>
		/// Creates a custom lifestyle.
		/// </summary>
		/// <returns>The custom lifestyle delegate.</returns>
		/// <param name="customLifestyle">The custom lifestyle.</param>
		public static Lifestyle CreateCustom(CustomLifestyleDelegate customLifestyle) {
			return new CustomLifestyle (customLifestyle);
		}
	}
}
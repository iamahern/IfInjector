using System;
using System.Linq.Expressions;

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
		/// var customLifestyle = Lifestyle.CreateCustom(instanceCreator => {
		/// 	ThreadLocal<object> instance = new ThreadLocal<object>(instanceCreator);
		///
		///		return () => {
		///			return instance.Value;
		///		}
		///	});
		/// </example>
		public delegate Func<object> CustomLifestyleDelegate(Func<object> instanceCreator);

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
			Expression<Func<CType>> resolverExpression,
			Func<CType> resolverExpressionCompiled,
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

		/////////
		// Internal impl for singleton
		private class SingletonLifestyle : Lifestyle {
			internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
				object syncLock, 
				Expression<Func<CType>> resolverExpression,
				Func<CType> resolverExpressionCompiled,
				CType testInstance)
			{
				return new SingletonLifestyleResolver<CType>(testInstance);
			}

			private class SingletonLifestyleResolver<CType> : LifestyleResolver<CType> where CType : class {
				private readonly CType instance;

				internal SingletonLifestyleResolver(CType instance) :
					base(Expression.Constant(instance))
				{
					this.instance = instance;
				}

				internal override CType Resolve() {
					return instance;
				}
			}
		}

		/////////
		// Internal impl for transient
		private class TransientLifestyle : Lifestyle {
			internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
				object syncLock, 
				Expression<Func<CType>> resolverExpression,
				Func<CType> resolverExpressionCompiled,
				CType testInstance)
			{
				return new TransientLifestyleResolver<CType>(resolverExpression, resolverExpressionCompiled);
			}

			private class TransientLifestyleResolver<CType> : LifestyleResolver<CType> where CType : class {
				private readonly Func<CType> resolverExpressionCompiled;

				internal TransientLifestyleResolver(Expression<Func<CType>> resolveExpression, Func<CType> resolverExpressionCompiled) : base(resolveExpression.Body) {
					this.resolverExpressionCompiled = resolverExpressionCompiled;
				}

				internal override CType Resolve() {
					return resolverExpressionCompiled();
				}
			}
		}

		/// <summary>
		/// Internal implementation for custom lifestyle factory class.
		/// </summary>
		private class CustomLifestyle : Lifestyle {
			private readonly CustomLifestyleDelegate lifestyleDelegate;

			internal CustomLifestyle(CustomLifestyleDelegate lifestyleDelegate) {
				this.lifestyleDelegate = lifestyleDelegate;
			}

			internal override LifestyleResolver<CType> GetLifestyleResolver<CType>(
				object syncLock, 
				Expression<Func<CType>> resolverExpression,
				Func<CType> resolverExpressionCompiled,
				CType testInstance)
			{
				Func<object> instanceCreator = () => resolverExpressionCompiled ();
				return new BaseCustomLifecyle<CType>(resolverExpression, lifestyleDelegate(instanceCreator));
			}

			private class BaseCustomLifecyle<CType> : LifestyleResolver<CType> where CType : class {
				private readonly Func<object> instanceCreator;

				internal BaseCustomLifecyle(
					Expression<Func<CType>> resolveExpression, 
					Func<object> instanceCreator) : base(resolveExpression) 
				{
					this.instanceCreator = instanceCreator;
				}

				internal override CType Resolve() {
					return (CType) instanceCreator();
				}
			}
		}
	}
}
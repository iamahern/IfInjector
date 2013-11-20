using System;
using System.Linq.Expressions;

using IfInjector.Bindings.Fluent;
using IfInjector.Bindings.Fluent.Concrete;
using IfInjector.Bindings.Lifestyles;

namespace IfInjector
{
	/// <summary>
	/// Provide extension methods for Func<P1..P4,CT>
	/// </summary>
	public static class InjectorBindingExtensions {
		/// <summary>
		/// Set the binding singleton.
		/// </summary>
		/// <returns>The singleton.</returns>
		/// <param name="binding">Binding.</param>
		/// <param name="singleton">If set to <c>true</c> singleton.</param>
		/// <typeparam name="BT">The 1st type parameter.</typeparam>
		/// <typeparam name="CT">The 2nd type parameter.</typeparam>
		public static IBinding<BT,CT> AsSingleton<BT,CT>(this IBinding<BT,CT> binding, bool singleton = true) 
			where BT : class
			where CT : class, BT
		{
			if (singleton) {
				return binding.SetLifestyle (Lifestyle.Singleton);
			} else {
				return binding.SetLifestyle (Lifestyle.Transient);
			}
		}

		/// <summary>
		/// Helper method to allow for setting of factories expressions.
		/// </summary>
		/// <returns>The factory helper.</returns>
		/// <param name="binding">Binding.</param>
		/// <param name="factoryExpression">Factory expression.</param>
		public static IBinding<BT,CT> SetFactoryHelper<BT, CT>(IOngoingBinding<BT> binding, LambdaExpression factoryExpression) 
			where BT : class
			where CT : class, BT
		{
			var internalBinding = (IOngoingBindingInternal<BT>)binding;
			return internalBinding.SetFactoryLambda<CT> (factoryExpression);
		}

		public static IBinding<BT,CT> SetFactory<BT, CT>(this IOngoingBinding<BT> binding, Expression<Func<CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IBinding<BT,CT> SetFactory<P1,BT,CT>(this IOngoingBinding<BT> binding, Expression<Func<P1,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IBinding<BT,CT> SetFactory<P1,P2,BT,CT>(this IOngoingBinding<BT> binding, Expression<Func<P1,P2,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IBinding<BT,CT> SetFactory<P1,P2,P3,BT,CT>(this IOngoingBinding<BT> binding, Expression<Func<P1,P2,P3,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
			where P3 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}

		public static IBinding<BT,CT> SetFactory<P1,P2,P3,P4,BT,CT>(this IOngoingBinding<BT> binding, Expression<Func<P1,P2,P3,P4,CT>> factoryExpression) 
			where BT : class
			where CT : class, BT
			where P1 : class
			where P2 : class
			where P3 : class
			where P4 : class
		{
			return SetFactoryHelper<BT,CT> (binding, factoryExpression);
		}
	}
}


using System;
using System.Linq.Expressions;
using IfInjector.Bindings.Fluent;

namespace IfInjector.Bindings.Fluent.Concrete
{
	/// <summary>
	/// Ongoing binding implementation.
	/// </summary>
	internal class OngoingBindingInternal<BType> : BoundBinding<BType, BType>, IOngoingBinding<BType>, IOngoingBindingInternal<BType>
		where BType : class
	{
		public IBoundBinding<BType, CType> To<CType> () 
			where CType : class, BType
		{
			return new BoundBinding<BType, CType> ();
		}

		public IBoundBinding<BType, CType> SetFactoryLambda<CType>(LambdaExpression factoryExpression)
			where CType : class, BType
		{
			var boundBinding = new BoundBinding<BType, CType> ();
			boundBinding.BindingConfig.FactoryExpression = factoryExpression;
			return boundBinding;
		}
	}
}
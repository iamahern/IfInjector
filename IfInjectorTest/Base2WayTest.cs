using System;
using IfInjector;
using IfInjector.Bindings.Fluent.Concrete;
using NUnit;
using NUnit.Framework;

namespace IfInjectorTest
{
	/// <summary>
	/// Base test case class, provides helpers for 
	/// </summary>
	public class Base2WayTest
	{
		public bool IsFactory { get; set; }

		protected Injector Injector { get; private set; }

		[SetUp]
		public void SetUpInjector() {
			Injector = new Injector ();
		}

		[TearDown]
		public void ResetInjector() {
			Injector = null;
		}

		protected IBinding<CType, CType> MakeBind<CType>() 
			where CType : class, new() 
		{
			return MakeBind<CType, CType> ();
		}

		protected IBinding<BType, CType> MakeBind<BType, CType>() 
			where BType : class 
			where CType : class, BType, new() 
		{
			if (IsFactory) {
				return Binding.For<BType> ().SetFactory (() => new CType ());
			} else {
				return Binding.For<BType>().To<CType> ();
			}
		}

		protected void Bind (IBinding binding) {
			Injector.Register (binding);
		}

		protected void Bind<CType>() 
			where CType : class, new()
		{
			Bind (MakeBind<CType> ());
		}

		protected void Bind<BType,CType>() 
			where BType : class
			where CType : class, BType, new()
		{
			Bind (MakeBind<BType, CType> ());
		}

	}
}


using System;
using IfInjector;
using IfInjector.IfCore;
using IfInjector.IfCore.IfBinding;
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

		protected IInjectorBinding<CType> Bind<CType>() where CType : class, new() {
			var binding = Injector.Bind<CType> ();
			if (IsFactory) {
				binding.SetFactory (() => new CType ());
			}
			return binding;
		}

		protected IInjectorBinding<CType> Bind<BType, CType>() 
			where BType : class 
			where CType : class, BType, new() 
		{
			var binding = Injector.Bind<BType, CType> ();
			if (IsFactory) {
				binding.SetFactory (() => new CType ());
			}
			return binding;
		}
	}
}


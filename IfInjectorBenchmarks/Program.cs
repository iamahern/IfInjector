using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Diagnostics;

using IfInjector;

namespace IfInjectorMain
{
	class MainClass
	{
		interface MyTestInterface { }
		class MyTestClass : MyTestInterface { }

		static Injector injector = new Injector();

		public static void Main (string[] args)
		{
			TestOriginal1 ();
			TestNew1 ();

			Console.WriteLine ("++++ (sleep) Rinse and repeat ++++");

			System.Threading.Thread.Sleep (2000);

			TestOriginal1 ();
			TestNew1 ();

			//AddManyImplsForInterface ();
			//TestOriginal2 ();

			Console.WriteLine ("++++ END TESTS ++++");
			Console.ReadLine ();
		}

		public static void TestOriginal1 () {
			fFastInjector.Injector.SetResolver<MyTestInterface, MyTestClass>();

			var result1 = fFastInjector.Injector.Resolve<MyTestInterface>();

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			for (int i = 0; i < 1000000; i++)
			{
				var result = fFastInjector.Injector.Resolve<MyTestInterface>();
			}
			Console.WriteLine("fFastInjector Resolve<T>(1000000) INSTANCE - NO properties: " + stopwatch.ElapsedTicks.ToString());
		
			// retest alt
			var typeT = typeof(MyTestInterface);
			stopwatch.Restart();
			for (int i = 0; i < 1000000; i++)
			{
				var result = fFastInjector.Injector.Resolve(typeT);
			}
			Console.WriteLine("fFastInjector Resolve(<T>)(1000000) INSTANCE - NO properties: " + stopwatch.ElapsedTicks.ToString());
		}

		public static void TestNew1 () {
			injector.Bind<MyTestInterface, MyTestClass>();

			var result1 = injector.Resolve<MyTestInterface>();

			var stopwatch = new Stopwatch();
			stopwatch.Start ();
			for (int i = 0; i < 1000000; i++)
			{
				var result = injector.Resolve<MyTestInterface>();
			}
			Console.WriteLine("IfFastInjector Resolve<T>(1000000) INSTANCE - NO properties: " + stopwatch.ElapsedTicks.ToString());
			
			var typeT = typeof(MyTestInterface);
			stopwatch.Restart();
			for (int i = 0; i < 1000000; i++)
			{
				var result = injector.Resolve(typeT);
			}
			Console.WriteLine("IFastInjector Resolve(<T>)(1000000) INSTANCE - NO properties: " + stopwatch.ElapsedTicks.ToString());
		}

		static void AddManyImplsForInterface() {
			AssemblyName assemblyName = new AssemblyName();
			assemblyName.Name = "tmpAssembly";
			AssemblyBuilder assemblyBuilder = Thread.GetDomain().DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
			ModuleBuilder module = assemblyBuilder.DefineDynamicModule("tmpModule");

			// Create many impls for the type and add resolvers for each 
			var setResolver = typeof(fFastInjector.Injector).GetMethod("SetResolver", Type.EmptyTypes);

			for (int i = 0; i < 1000; i++)
			{
				var interfaceBuilder = module.DefineType("TestInterface" + i.ToString(), TypeAttributes.Public | TypeAttributes.Interface | TypeAttributes.Abstract);
				var myInterface = interfaceBuilder.CreateType();
				var typebuilder = module.DefineType("TestType" + i.ToString(), TypeAttributes.Public | TypeAttributes.Class);

				typebuilder.AddInterfaceImplementation(myInterface);

				var myType = typebuilder.CreateType();
				var method = setResolver.MakeGenericMethod(myInterface, myType);
				
				method.Invoke(null, new object[0]);
			}
		}

		static void TestOriginal2 () {
			fFastInjector.Injector.SetResolver<MyTestInterface, MyTestClass>();

			var result1 = fFastInjector.Injector.Resolve<MyTestInterface>();
			var stopwatch = new Stopwatch();

			stopwatch.Start();
			for (int i = 0; i < 1000000; i++)
			{
				var result = fFastInjector.Injector.Resolve<MyTestInterface>();
			}
			Console.WriteLine("fFastInjector Resolve(1000000) INSTANCE - NO properties: " + stopwatch.ElapsedTicks.ToString());
		}
	}
}

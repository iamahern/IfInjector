using NUnit.Framework;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace FastInjectorMxTest
{
	[TestFixture()]
	public class TestLinq
	{
		[Test]
		public void CreateSetterFromGetter()
		{
			Action<Person, int> ageSetter = InitializeSet((Person p) => p.Age);
			Action<Person, string> nameSetter = InitializeSet((Person p) => p.Name);

			Person p1 = new Person();
			ageSetter(p1, 29);
			nameSetter(p1, "John");

			Assert.IsTrue(p1.Name == "John");
			Assert.IsTrue(p1.Age == 29);

			Action<Person, int, string> foo = InitializeFoo ();
			foo (p1, 20, "Mike");
			Assert.IsTrue(p1.Name == "Mike");
			Assert.IsTrue(p1.Age == 20);
		}

		public class Person { public int Age { get; set; } public string Name { get; set; } }

		public static Action<Person, int, string> InitializeFoo()
		{
			ParameterExpression instance = Expression.Parameter (typeof(Person), "instance");
			var instance2 = Expression.Variable (typeof(Person));

			ParameterExpression age = Expression.Parameter (typeof(int), "age");
			ParameterExpression name = Expression.Parameter (typeof(string), "name");

			Expression<Func<Person,int>> ageProp = (Person p) => p.Age;
			PropertyInfo agePropInfo = (ageProp.Body as MemberExpression).Member as PropertyInfo;

			Expression<Func<Person,string>> nameProp = (Person p) => p.Name;
			PropertyInfo namePropInfo = (nameProp.Body as MemberExpression).Member as PropertyInfo;

			var block = Expression.Block (
				new [] { instance2 },
				Expression.Assign(instance2, instance),
				Expression.Call (instance2, agePropInfo.GetSetMethod (), age),
				Expression.Call(instance2, namePropInfo.GetSetMethod(), name)
			);


			return Expression.Lambda<Action<Person, int, string>>(
				block,
				new ParameterExpression[] { instance, age, name }).Compile();
		}

		public static Action<TContainer, TProperty> InitializeSet<TContainer, TProperty>(Expression<Func<TContainer, TProperty>> getter)
		{
			PropertyInfo propertyInfo = (getter.Body as MemberExpression).Member as PropertyInfo;

			ParameterExpression instance = Expression.Parameter(typeof(TContainer), "instance");
			ParameterExpression parameter = Expression.Parameter(typeof(TProperty), "param");

			return Expression.Lambda<Action<TContainer, TProperty>>(
				Expression.Call(instance, propertyInfo.GetSetMethod(), parameter),
				new ParameterExpression[] { instance, parameter }).Compile();
		}
	}
}


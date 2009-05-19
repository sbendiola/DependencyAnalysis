
using System;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace TestDependency
{
		
	[TestFixture]
	public class Test
	{
		
		[Test]
		public void TestCase()
		{
			var illegalNamespace ="Foo.Bar";
			var errors = from type in ReferencedAssemblies.Types 
				where type.ReferencesNameSpace(illegalNamespace)
				select type;
			Assert.IsTrue(errors.Any() == false, string.Format("{0} should not reference namespace {1}", errors, illegalNamespace)); 
			
		}
	}
				
	
	public static class ReferencedAssemblies {
		
	 	public static ICollection<Type> Types {
		  	get {
				Console.WriteLine("entry " + Assembly.GetExecutingAssembly());
				var assemblies = Assembly.GetExecutingAssembly()
										 .GetReferencedAssemblies();
				return assemblies.Aggregate(new List<Type>(), (types, assembly) => {
					var a = Assembly.Load(assembly);
					types.AddRange(a.GetTypes().ToList());
					return types;
				});
			}
	  	}

		
		public static bool ReferencesNameSpace(this Type type, params string[] namespacename) {
			//check all method signatures
			var interfaces = type.GetInterfaces;
			var baseType = type.BaseType;
			
			if (type.IsInterface == false) {
				
			} 
			
			return true;
		}
	}
}
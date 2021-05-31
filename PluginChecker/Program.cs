using System;
using System.Reflection;

namespace PluginChecker {
	class Program {
		static void LoadErrors(Exception ex) {
			Console.WriteLine(ex);
			
			ReflectionTypeLoadException refEx = ex as ReflectionTypeLoadException;
			if (refEx == null) return;
			
			foreach (Exception ex2 in refEx.LoaderExceptions) {
				Console.WriteLine(ex2);
			}
			Console.ReadLine();
		}
		
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			using (DllProcessor processor = new DllProcessor()) {
				processor.Init(args[0]);
				processor.CheckDirectory("plugins");
			}
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}
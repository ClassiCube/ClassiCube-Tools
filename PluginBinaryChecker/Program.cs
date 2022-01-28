using System;

namespace PluginChecker 
{
	class Program 
	{
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			using (DllProcessor processor = new DllProcessor()) {
				processor.Init(args[0]);
				processor.CheckDirectory("plugins");
				processor.CheckDirectory("extra/commands/dll");
			}
			
			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
	}
}
using System;
using System.Diagnostics;
using System.IO;
using MCGalaxy;
using MCGalaxy.Scripting;

namespace PluginChecker 
{
	class Program 
	{
		static ICompiler cs_compiler;
		static Player log_player;
		static int compiled, failed;

		public static void Main(string[] args)
		{
			string root = Environment.CurrentDirectory;
			if (args.Length > 0)
				root = args[0];
			Console.WriteLine("Root MCGalaxy folder: " + root);
			WriteColored(ConsoleColor.Green, "NOTE: Referenced .dlls will be loaded from: " + Environment.CurrentDirectory);

			// TODO unfortunately currently necessary 
			try { Directory.CreateDirectory("logs"); } catch { }
			try { Directory.CreateDirectory("logs/errors"); } catch { }

			cs_compiler = ICompiler.Compilers.Find(c => c.ShortName == "CS");
			log_player  = new LogPlayer();

			if (root.StartsWith("@")) {
				// @[path] is specially treated as compiling all files in given directory
				CheckDirectory(root.Substring(1), "", "plugin");
			} else {
				CheckDirectory(root, "plugins", "plugin");
				CheckDirectory(root, "extra/commands/source", "command");
			}

			Console.WriteLine("Compiled {0} source files ({1} failures)", compiled, failed);
			// MCGalaxy's Scheduler threads prevent this application from closing
			Process.GetCurrentProcess().Kill();
		}	
		
		
		static void CheckDirectory(string root, string directory, string type) {
			string path    = Path.Combine(root, directory);
			string[] files = Directory.GetFiles(path, "*.cs");

			foreach (string file in files)
			{
				WriteColored(ConsoleColor.Yellow, "Compiling " + file);
				var results = ScriptingOperations.Compile(log_player, cs_compiler, type, new[] { file }, null);
				compiled++;
				if (results == null || results.Errors.Count > 0) failed++;
			}
		}
		class LogPlayer : Player
        {
			public LogPlayer() : base("ErrorChecker") { }

            public override void Message(byte type, string message)
            {
				if (message.Contains("&W"))
				{
					WriteColored(ConsoleColor.Red, "   " + message.Replace("&W", ""));
				}
				else
				{
					WriteColored(ConsoleColor.DarkGray, message);
				}
            }
        }

		static void WriteColored(ConsoleColor color, string message)
        {
			System.Console.ForegroundColor = color;
			System.Console.WriteLine(message);
			System.Console.ResetColor();
		}
	}
}
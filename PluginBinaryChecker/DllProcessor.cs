﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PluginChecker {
	
	public class DllProcessor : IDisposable {
		string root;
		
		public void Init(string root) {
			this.root = root;
			//AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve += ResolveAssembly;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
			InstructionProcessor.InitCache();
		}
		
		public void Dispose() {
			//AppDomain.CurrentDomain.ReflectionOnlyAssemblyResolve -= ResolveAssembly;
			AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;
		}
		
		
		Assembly LoadFrom(string path) {
			byte[] data = File.ReadAllBytes(path);
			return Assembly.Load(data);
			//return Assembly.ReflectionOnlyLoadFrom(path);
		}
		
		List<string> seenAssemblies = new List<string>();
		Assembly ResolveAssembly(object sender, ResolveEventArgs args) {
			AssemblyName name = new AssemblyName(args.Name);
			string path       = Path.Combine(root, name.Name + ".dll");
			
			// first try to load from MCGalaxy folder
			if (File.Exists(path)) return LoadFrom(path);
			
			// have we already tried loading this assembly but failed to do so?
			if (seenAssemblies.Contains(args.Name)) return null;
			seenAssemblies.Add(args.Name);
			
			// use normal assembly loading method - note that this can mean
			//  ResolveAssembly is called again
			return Assembly.Load(args.Name);
		}
		
		const BindingFlags all = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

		
		void LogFailure(MethodInfo method, string action, Instruction ins) {
			string file = method.DeclaringType.Assembly.GetName().Name + ".dll";

			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine("CAN'T RESOLVE '{0}' in {1}.{2} [IL_{3}] ({4})",
			                  action, method.DeclaringType.Name, method.Name, 
							  ins.Offset.ToString("x4"), file);
			Console.ResetColor();
		}
		
		void ResolveMethod(Assembly lib, MethodInfo method, Instruction ins) {
			MethodBase value = ins.ResolveMethod(lib);
			if (value == null) LogFailure(method, "method", ins);
		}
		
		void ResolveField(Assembly lib, MethodInfo method, Instruction ins) {
			FieldInfo value = ins.ResolveField(lib);
			if (value == null) LogFailure(method, "field", ins);
		}
		
		void ResolveType(Assembly lib, MethodInfo method, Instruction ins) {
			Type value = ins.ResolveType(lib);
			if (value == null) LogFailure(method, "type", ins);
		}
		
		void CheckMethod(Assembly lib, MethodInfo method) {
			MethodBody body = method.GetMethodBody();
			if (body == null) return;
			
			byte[] data = body.GetILAsByteArray();
			List<Instruction> all = InstructionProcessor.GetAll(data);

			foreach (Instruction ins in all) 
			{
				if (ins.UsesMethod) ResolveMethod(lib, method, ins);
				if (ins.UsesField)  ResolveField(lib,  method, ins);
				if (ins.UsesType)   ResolveType(lib,   method, ins);
			}
		}
		
		void CheckType(Assembly lib, Type type) {
			MethodInfo[] methods = type.GetMethods(all);
			foreach (MethodInfo method in methods) 
			{
				// only inspect methods in this assembly
				if (method.DeclaringType.Assembly != lib) continue;
				
				CheckMethod(lib, method);
			}
		}
		
		void CheckFile(string path) {
			Assembly lib = LoadFrom(path);
			Instruction.Ignored.Add(lib); // list of assemblies to ignore for resolving
			Type[] types = lib.GetTypes();
			
			foreach (Type type in types) 
			{
				CheckType(lib, type);
			}
		}
		
		public void CheckDirectory(string directory) {
			directory = Path.Combine(root, directory);
			string[] files = Directory.GetFiles(directory, "*.dll");

			foreach (string file in files)
			{
				try {
					CheckFile(file);
				} catch (Exception ex) {
					LogErrors(ex, file);
				}
			}
		}
		static void LogErrors(Exception ex, string file) {
			Console.ForegroundColor = ConsoleColor.Red;
			Console.WriteLine("### ERROR INSPECTING " + file);
			Console.ResetColor();

			Console.WriteLine(ex);
			ReflectionTypeLoadException refEx = ex as ReflectionTypeLoadException;
			if (refEx == null) return;

			foreach (Exception ex2 in refEx.LoaderExceptions)
			{
				Console.WriteLine(ex2);
			};
		}
	}
}

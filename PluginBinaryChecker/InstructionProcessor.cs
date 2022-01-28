using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace PluginChecker {
	
	public sealed class Instruction {
		public OpCode Opcode;
		public object Operand;
		public int Offset;
		
		public bool UsesMethod { get { return Opcode.OperandType == OperandType.InlineMethod; } }
		public bool UsesField  { get { return Opcode.OperandType == OperandType.InlineField; } }
		public bool UsesType   { get { return Opcode.OperandType == OperandType.InlineType; } }
		
		public override string ToString() {
			return Opcode.Name + " - " + Operand;
		}
		
		
		// don't try to resolve references from other plugin assemblies
		public static List<Assembly> Ignored = new List<Assembly>();
		
		static T Resolve<T>(Assembly lib, Func<Module, T> resolve) {
			// try with this plugin assembly first
			{
				foreach (Module m in lib.GetModules()) {
					try { return resolve(m); } catch { }
				}
			}
			
			// try again with ALL assemblies
			foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
				if (Ignored.Contains(a)) continue;
				
				foreach (Module m in a.GetModules()) {
					try { return resolve(m); } catch { }
				}
			}
			return default(T);
		}
		
		public MethodBase ResolveMethod(Assembly lib) {
			int token = (int)Operand;
			return Resolve(lib, m => m.ResolveMethod(token));
		}
		
		public FieldInfo ResolveField(Assembly lib, Type[] T) {
			int token = (int)Operand;
			return Resolve(lib, m => m.ResolveField(token, T, null));
		}
		
		public Type ResolveType(Assembly lib) {
			int token = (int)Operand;
			return Resolve(lib, m => m.ResolveType(token));
		}
	}
	
	public static class InstructionProcessor {

		static OpCode[] mainCodes = new OpCode[256];
		static OpCode[] extCodes = new OpCode[256];
		public static void InitCache() {
			// find all MSIL opcodes and cache them
			FieldInfo[] fields = typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static);
			foreach (FieldInfo field in fields) {
				if (field.FieldType != typeof(OpCode)) continue;
				
				OpCode opcode = (OpCode)field.GetValue(null);
				//if (opcode.OperandType == OperandType.InlineTok)
				//Console.WriteLine(opcode.Name + " :: " + opcode.OperandType);
				if (opcode.Size == 1) {
					mainCodes[opcode.Value] = opcode;
				} else if (opcode.Size == 2) {
					// second byte is 0xFE
					extCodes[opcode.Value & 0xFF] = opcode;
				}
			}
		}
		
		public static List<Instruction> GetAll(byte[] data) {
			List<Instruction> ins = new List<Instruction>();
			
			for (int offset = 0; offset < data.Length;) {
				ins.Add(Next(data, ref offset));
			}
			return ins;
		}
		
		public static Instruction Next(byte[] data, ref int offset) {
			int beg = offset;
			byte id = data[offset++];
			OpCode opcode;
			
			if (id == 0xFE) {
				// extended opcodes
				id     = data[offset++];
				opcode = extCodes[id];
			} else {
				opcode = mainCodes[id];
			}
			
			Instruction ins = new Instruction();
			ins.Opcode  = opcode;
            ins.Operand = ReadOperand(opcode.OperandType, data, ref offset);
			ins.Offset  = beg;
            return ins;
		}

		static object ReadOperand(OperandType type, byte[] data, ref int offset) {
			int count;
			switch (type) {
				case OperandType.InlineBrTarget:
				case OperandType.InlineField:
				case OperandType.InlineI:
				case OperandType.InlineMethod:
				case OperandType.InlineSig:
				case OperandType.InlineString:
				case OperandType.InlineTok:
				case OperandType.InlineType:
					return ReadInt32(data, ref offset);
					
				case OperandType.InlineI8:
					return ReadInt64(data, ref offset);
					
				case OperandType.InlineR: // really double
					return ReadInt64(data, ref offset);
					
				case OperandType.InlineVar:
					return ReadInt16(data, ref offset);
					
				case OperandType.ShortInlineI:
					return ReadInt8(data, ref offset);
					
				case OperandType.ShortInlineVar:
				case OperandType.ShortInlineBrTarget:
					return ReadUInt8(data, ref offset);
					
				case OperandType.ShortInlineR:
					return ReadInt32(data, ref offset);
					
				case OperandType.InlineNone:
					return null;
					
				case OperandType.InlineSwitch:
					count = ReadInt32(data, ref offset);
					// skip over switch addresses
					for (int i = 0; i < count; i++) ReadInt32(data, ref offset);
					return null;
					
				default:
					throw new NotSupportedException("Unsupported operand type: " + type);
			}
		}
		
		
		static byte ReadUInt8(byte[] data, ref int offset) {
			return data[offset++];
		}
		
		static sbyte ReadInt8(byte[] data, ref int offset) {
			return (sbyte)data[offset++];
		}
		
		static short ReadInt16(byte[] data, ref int offset) {
			return (short)(data[offset++] | (data[offset++] << 8));
		}
		
		static int ReadInt32(byte[] data, ref int offset) {
			return data[offset++]         | (data[offset++] <<  8) |
				(data[offset++] << 16) | (data[offset++] << 24);
		}
		
		static long ReadInt64(byte[] data, ref int offset) {
			long lo = ReadInt32(data, ref offset) & uint.MaxValue;
			long hi = ReadInt32(data, ref offset) & uint.MaxValue;
			return (lo << 32) | hi;
		}
	}
}

using System;
using System.Globalization;
using System.IO;

namespace c8c {
	class c8c {
		static void Main(string[] args) {
			if (args.Length == 0) {
				Console.WriteLine("No input specified. Nothing to do.");
				return;
			}

			try {
				FileInfo input = new FileInfo(args[0]);
				int dot = args[0].LastIndexOf('.');
				FileInfo output = new FileInfo(args[0].Substring(0, dot) + ".c8");
				compile(input, output);
				Console.WriteLine("Successfully compiled " + input + ", result : " + output);
			} catch (CompileException ex) {
				Console.WriteLine(ex.Message);
			}
		}

		private static void writeshort(ushort value, FileStream bytecode) {
			for (int i = 1; i >= 0; --i) {
				byte temp = (byte)((value >> (8 * i)) & 0xFF);
				bytecode.WriteByte(temp);
			}
		}

		private static ushort getOpCodeRegReg(byte head, byte tail, string[] args, int i) {
			if (args.Length != 2) {
				throw new CompileException("Invalid number of arguments", i + 1);
			}
			ushort r1, r2;
			if (args[0].Length != 2 || args[1].Length != 2 || args[0][0] != 'v' || !ushort.TryParse(args[0][1].ToString(), NumberStyles.AllowHexSpecifier, null, out r1) || !ushort.TryParse(args[1][1].ToString(), NumberStyles.AllowHexSpecifier, null, out r2)) {
				throw new CompileException("Invalid format", i);
			}
			return (ushort) ((head << 12) | (r1 << 8) | (r2 << 4) | tail);
		}

		private static ushort getOpCodeRegVal(byte head, string[] args, int i) {
			if (args.Length != 2) {
				throw new CompileException("Invalid number of arguments", i + 1);
			}
			ushort n, register;
			if (args[1].Length > 2 || args[0].Length != 2 || args[0][0] != 'v' || !ushort.TryParse(args[0][1].ToString(), NumberStyles.AllowHexSpecifier, null, out register) || !ushort.TryParse(args[1], NumberStyles.AllowHexSpecifier, null, out n)) {
				throw new CompileException("Invalid format", i);
			}
			return (ushort) ((head << 12) | (register << 8) | n);
		}

		private static ushort getOpCodeReg(byte head, byte tail, string[] args, int i) {
			if (args.Length != 1) {
				throw new CompileException("Invalid number of arguments", i + 1);
			}
			ushort register;
			if (args[0].Length != 2 || args[0][0] != 'v' || !ushort.TryParse(args[0][1].ToString(), NumberStyles.AllowHexSpecifier, null, out register)) {
				throw new CompileException("Invalid format", i);
			}
			return (ushort) ((head << 12) | (register << 8) | tail);
		}

		private static ushort getOpCodeRegRegVal(byte head, string[] args, int i) {
			if (args.Length != 3) {
				throw new CompileException("Invalid number of arguments", i + 1);
			}
			ushort r1, r2, n;
			if (args[0].Length != 2 || args[1].Length != 2 || args[2].Length != 1 || args[0][0] != 'v' || args[1][0] != 'v' || !ushort.TryParse(args[0][1].ToString(), NumberStyles.AllowHexSpecifier, null, out r1) || !ushort.TryParse(args[1][1].ToString(), NumberStyles.AllowHexSpecifier, null, out r2) || !ushort.TryParse(args[2][0].ToString(), NumberStyles.AllowHexSpecifier, null, out n)) {
				throw new CompileException("Invalid format", i);
			}
			return (ushort) ((head << 12) | (r1 << 8) | (r2 << 4) | n);
		}

		private static ushort getOpCodeAddr(byte head, string[] args, int i) {
			if (args.Length != 1) {
				throw new CompileException("Invalid number of arguments", i + 1);
			}
			ushort address;
			if (args[0].Length > 3 || !ushort.TryParse(args[0], NumberStyles.AllowHexSpecifier, null, out address)) {
				throw new CompileException("Invalid format", i);
			}
			return (ushort) ((head << 12) | (((address - 1) << 1) + 0x200));
		}

		public static void compile(FileInfo input, FileInfo output) {
			using (StreamReader reader = input.OpenText()) {
				using (FileStream bytecode = output.Create()) {
					char[] args_delims = new char[] {' ', ','};

					string line;
					int i = 0;
					while ((line = reader.ReadLine()) != null) {
						if (line.Length == 0) {
							continue;
						}
						i++;

						int space = line.IndexOf(' ');
						if (space == -1) {
							space = line.Length;
						}

						string instruction = line.Substring(0, space);
						string[] args = new string[0];
						if (space != line.Length) {
							args = line.Substring(space + 1).Split(args_delims, StringSplitOptions.RemoveEmptyEntries);
						}

						try {
							switch (instruction) {
								case "cls":
									writeshort(0x00E0, bytecode);
									break;
								case "rts":
									writeshort(0x00EE, bytecode);
									break;
								case "jmp":
									writeshort(getOpCodeAddr(0x1, args, i), bytecode);
									break;
								case "jsr":
									writeshort(getOpCodeAddr(0x2, args, i), bytecode);
									break;
								case "skeq":
									if (args.Length != 2) {
										throw new CompileException("Invalid number of arguments", i + 1);
									}
									writeshort(args[1][0] == 'v' ? getOpCodeRegReg(0x5, 0x0, args, i) : getOpCodeRegVal(0x3, args, i), bytecode);
									break;
								case "skne":
									if (args.Length != 2) {
										throw new CompileException("Invalid number of arguments", i + 1);
									}
									writeshort(args[1][0] == 'v' ? getOpCodeRegReg(0x9, 0x0, args, i) : getOpCodeRegVal(0x4, args, i), bytecode);
									break;
								case "mov":
									if (args.Length != 2) {
										throw new CompileException("Invalid number of arguments", i + 1);
									}
									writeshort(args[1][0] == 'v' ? getOpCodeRegReg(0x8, 0x0, args, i) : getOpCodeRegVal(0x6, args, i), bytecode);
									break;
								case "add":
									if (args.Length != 2) {
										throw new CompileException("Invalid number of arguments", i + 1);
									}
									writeshort(args[1][0] == 'v' ? getOpCodeRegReg(0x8, 0x4, args, i) : getOpCodeRegVal(0x7, args, i), bytecode);
									break;
								case "or":
									writeshort(getOpCodeRegReg(0x8, 0x1, args, i), bytecode);
									break;
								case "and":
									writeshort(getOpCodeRegReg(0x8, 0x2, args, i), bytecode);
									break;
								case "xor":
									writeshort(getOpCodeRegReg(0x8, 0x3, args, i), bytecode);
									break;
								case "sub":
									writeshort(getOpCodeRegReg(0x8, 0x5, args, i), bytecode);
									break;
								case "shr":
									writeshort(getOpCodeReg(0x8, 0x06, args, i), bytecode);
									break;
								case "rsb":
									writeshort(getOpCodeRegReg(0x8, 0x7, args, i), bytecode);
									break;
								case "shl":
									writeshort(getOpCodeReg(0x8, 0x0E, args, i), bytecode);
									break;
								case "mvi":
									writeshort(getOpCodeAddr(0xA, args, i), bytecode);
									break;
								case "jmi":
									writeshort(getOpCodeAddr(0xB, args, i), bytecode);
									break;
								case "rand":
									writeshort(getOpCodeRegVal(0xC, args, i), bytecode);
									break;
								case "sprite":
									writeshort(getOpCodeRegRegVal(0xD, args, i), bytecode);
									break;
								case "slpr":
									writeshort(getOpCodeReg(0xE, 0x9E, args, i), bytecode);
									break;
								case "skup":
									writeshort(getOpCodeReg(0xE, 0xA1, args, i), bytecode);
									break;
								case "gdelay":
									writeshort(getOpCodeReg(0xF, 0x07, args, i), bytecode);
									break;
								case "key":
									writeshort(getOpCodeReg(0xF, 0x0A, args, i), bytecode);
									break;
								case "sdelay":
									writeshort(getOpCodeReg(0xF, 0x15, args, i), bytecode);
									break;
								case "ssound":
									writeshort(getOpCodeReg(0xF, 0x18, args, i), bytecode);
									break;
								case "adi":
									writeshort(getOpCodeReg(0xF, 0x1E, args, i), bytecode);
									break;
								case "font":
									writeshort(getOpCodeReg(0xF, 0x29, args, i), bytecode);
									break;
								case "bcd":
									writeshort(getOpCodeReg(0xF, 0x33, args, i), bytecode);
									break;
								case "str":
									writeshort(getOpCodeReg(0xF, 0x55, args, i), bytecode);
									break;
								case "ldr":
									writeshort(getOpCodeReg(0xF, 0x65, args, i), bytecode);
									break;
								default:
									throw new CompileException("Unkown instruction \"" + instruction + "\"", i + 1);
							}
						} catch {
							throw;
						}
					}
				}
			}
		}

		private class CompileException : FormatException {

			public int Position {
				get;
				private set;
			}

			public CompileException(string message, int i) : base(message + " at line " + i + ".") {
				Position = i;
			}
		}
	}
}

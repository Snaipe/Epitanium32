using System;
using System.Windows.Forms;

namespace Epitanium32 {
	public class Emulator {

		public static readonly byte[] FONTSET = { 
		  0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
		  0x20, 0x60, 0x20, 0x20, 0x70, // 1
		  0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
		  0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
		  0x90, 0x90, 0xF0, 0x10, 0x10, // 4
		  0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
		  0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
		  0xF0, 0x10, 0x20, 0x40, 0x40, // 7
		  0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
		  0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
		  0xF0, 0x90, 0xF0, 0x90, 0x90, // A
		  0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
		  0xF0, 0x80, 0x80, 0x80, 0xF0, // C
		  0xE0, 0x90, 0x90, 0x90, 0xE0, // D
		  0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
		  0xF0, 0x80, 0xF0, 0x80, 0x80  // F
		};

		private ushort opcode;
		private byte[] mem;
		private byte[] V;

		private ushort I;
		private ushort pMem;

		private ushort[] stack;
		private ushort pStack;

		private GraphicsDevice gfx;
		private KeyBoard key;

		private byte delay_timer;
		private byte sound_timer;

		private bool running;
		private bool draw;

		public Emulator() {
			#region initialize cpu opcode references
			table = new Action[16] {
				cpu_0,
				cpu_jump_to,
				cpu_call,
				cpu_skip_if_equal,
				cpu_skip_if_nequal,
				cpu_skip_if_equal2,
				cpu_move,
				cpu_increment,
				cpu_arithmetic,
				cpu_skip_if_nequal2,
				cpu_adr_set,
				cpu_jump_after,
				cpu_random,
				cpu_draw,
				cpu_skips,
				cpu_other
			};

			arithmetic_table = new Action[16] {
				cpu_set,
				cpu_or,
				cpu_and,
				cpu_xor,
				cpu_add,
				cpu_sub,
				cpu_rshift,
				cpu_sub2,
				cpu_null,
				cpu_null,
				cpu_null,
				cpu_null,
				cpu_null,
				cpu_null,
				cpu_lshift,
				cpu_null
			};

			#endregion
		}

		public void init() {
			Console.SetWindowSize(64, 32);
			Console.SetBufferSize(64, 32);
			Console.CursorVisible = false;

			key = new KeyBoard();
			gfx = new GraphicsDevice(key);
			gfx.Focus();

			mem    = new byte[4096];
			V      = new byte[16];

			stack  = new ushort[16];
			pStack = 0;

			opcode = 0;
			I      = 0;
			pMem   = 0x200;

			// Fill section 0x050 - 0x0A0 with inbuild font
			for (int i = 0; i < 0x050; i++) {
				mem[i] = FONTSET[i];
			}
		}

		private void fetch() {
			opcode = (ushort) (mem[pMem] << 8 | mem[pMem + 1]);
			pMem += 2;
		}

		private void tick() {
			fetch();
			table[(opcode & 0xF000) >> 12]();

			gfx.Update();
			Application.DoEvents();

			if (delay_timer > 0) {
				--delay_timer;
			}

			if (sound_timer > 0 && sound_timer-- == 1) {
				Console.Beep();
			}
		}

		public void run() {
			running = true;

			Console.WriteLine("Starting Emulating 8bit cpu...");

			float tpc = 0;
			float occurences = 0;
			while (running) {
				long dt = Environment.TickCount;
				tick();

				if (draw) {
					gfx.draw();
					draw = false;
				}

				for (int i = 0; i < 100000; i++);

				if (pMem >= mem.Length) {
					exit();
				}

				tpc += (Environment.TickCount - dt);
				if (occurences == 10000) {
					int left = Console.CursorLeft;
					int top  = Console.CursorTop;
					Console.SetCursorPosition(0, 31);
					Console.Write("milliseconds per cycle : " + (tpc / occurences));
					Console.SetCursorPosition(left, top);
					occurences = tpc = 0;
				}
				occurences++;
				
			}
		}

		public void exit() {
			running = false;
		}

		public void loadRom(byte[] raw) {
			Array.Copy(raw, 0, mem, 0x200, 0xDFE);
			Console.WriteLine("Loaded raw data into memory.");
		}

		#region CPU functions

			private Action[] table;
			private Action[] arithmetic_table;

			private void cpu_null() {
				Console.WriteLine("Unknown opcode {0:X}", opcode);
			}

			// 0x0NNN
			private void cpu_0() {
				switch (opcode & 0x00FF) {
					case 0x00E0:
						cpu_gfx_clear();
						break;
					case 0x00EE:
						cpu_return();
						break;
					default:
						cpu_null();
						break;
				}
			}

			// 0x00E0
			private void cpu_gfx_clear() {
				gfx.clear();
			}

			// 0x00EE
			private void cpu_return() {
				pMem = stack[--pStack];
			}

			// 0x1NNN
			private void cpu_jump_to() {
				pMem = (ushort) (opcode & 0x0FFF);
			}

			// 0x2NNN
			private void cpu_call() {
				stack[pStack++] = pMem;
				pMem = (ushort) (opcode & 0x0FFF);
			}

			// 0x3XNN
			private void cpu_skip_if_equal() {
				if (V[(opcode & 0x0F00) >> 8] == (opcode & 0x00FF)) {
					pMem += 2;
				}
			}

			// 0x4XNN
			private void cpu_skip_if_nequal() {
				if (V[(opcode & 0x0F00) >> 8] != (opcode & 0x00FF)) {
					pMem += 2;
				}
			}

			// 0x5XY0
			private void cpu_skip_if_equal2() {
				if (V[(opcode & 0x0F00) >> 8] == V[(opcode & 0x00F0) >> 4]) {
					pMem += 2;
				}
			}

			// 0x6XNN
			private void cpu_move() {
				V[(opcode & 0x0F00) >> 8] = (byte) (opcode & 0x00FF);
			}

			// 0x7XNN
			private void cpu_increment() {
				V[(opcode & 0x0F00) >> 8] += (byte) (opcode & 0x00FF);
			}

			// 0x8XYN
			private void cpu_arithmetic() {
				arithmetic_table[opcode & 0x000F]();
			}

			#region arithmetics

				// 0x8XY0
				private void cpu_set() {
					V[(opcode & 0x0F00) >> 8] = V[(opcode & 0x00F0) >> 4];
				}

				// 0x8XY1
				private void cpu_or() {
					V[(opcode & 0x0F00) >> 8] |= V[(opcode & 0x00F0) >> 4];
				}

				// 0x8XY2
				private void cpu_and() {
					V[(opcode & 0x0F00) >> 8] &= V[(opcode & 0x00F0) >> 4];
				}

				// 0x8XY3
				private void cpu_xor() {
					V[(opcode & 0x0F00) >> 8] ^= V[(opcode & 0x00F0) >> 4];
				}

				// 0x8XY4
				private void cpu_add() {
					V[0xF] = (byte) (V[(opcode & 0x00F0) >> 4] > (0xFF - V[(opcode & 0x0F00) >> 8]) ? 1 : 0);
					V[(opcode & 0x0F00) >> 8] += V[(opcode & 0x00F0) >> 4];
				}

				// 0x8XY5
				private void cpu_sub() {
					V[0xF] = (byte) (V[(opcode & 0x00F0) >> 4] > V[(opcode & 0x0F00) >> 8] ? 0 : 1);
					V[(opcode & 0x0F00) >> 8] -= V[(opcode & 0x00F0) >> 4];
				}

				// 0x8XY6
				private void cpu_rshift() {
					V[0xF] = (byte) (V[(opcode & 0x0F00) >> 8] & 0x1);
					V[(opcode & 0x0F00) >> 8] >>= 1;
				}

				// 0x8XY7
				private void cpu_sub2() {
					V[0xF] = (byte) (V[(opcode & 0x0F00) >> 8] > V[(opcode & 0x00F0) >> 4] ? 0 : 1);
					V[(opcode & 0x0F00) >> 8] = (byte) (V[(opcode & 0x00F0) >> 4] - V[(opcode & 0x0F00) >> 8]);
				}

				// 0x8XYE
				private void cpu_lshift() {
					V[0xF] = (byte) (V[(opcode & 0x0F00) >> 8] >> 7);
					V[(opcode & 0x0F00) >> 8] <<= 1;
				}

			#endregion

			// 0x9XY0
			private void cpu_skip_if_nequal2() {
				if (V[(opcode & 0x0F00) >> 8] != V[(opcode & 0x00F0) >> 4]) {
					pMem += 2;
				}
			}

			// 0xANNN
			private void cpu_adr_set() {
				I = (ushort) (opcode & 0x0FFF);
			}

			// 0xBNNN
			private void cpu_jump_after() {
				pMem = (ushort) ((opcode & 0x0FFF) + V[0]);
			}

			// 0xCXNN
			private void cpu_random() {
				V[(opcode & 0x0F00) >> 8] = (byte) (new Random().Next(byte.MaxValue) & (opcode & 0x00FF));
			}

			// 0xDXYN
			private void cpu_draw() {
				ushort x = V[(opcode & 0x0F00) >> 8];
				ushort y = V[(opcode & 0x00F0) >> 4];
				ushort height = (ushort) (opcode & 0x000F);
				ushort pixel;
 
				V[0xF] = 0;
				for (int j = 0; j < height && y + j < 32; j++) {
					pixel = mem[I + j];
					for (int i = 0; i < 8 && x + i < 64; i++) {
						if ((pixel & (0x80 >> i)) != 0) {
							if (gfx[x + i, y + j] == 1) {
								V[0xF] = 1;
							}
							gfx[x + i, y + j] ^= 1;
						}
					}
				}
 
				draw = true;
			}

			// 0xEXNN
			private void cpu_skips() {
				switch (opcode & 0x00FF) {
					case 0x009E:
						cpu_skip_if_pressed();
						break;
					case 0x00A1:
						cpu_skip_if_not_pressed();
						break;
					default:
						cpu_null();
						break;
				}
			}

			#region skips

				// 0xEX9E
				private void cpu_skip_if_pressed() {
					if(key[V[(opcode & 0x0F00) >> 8]] != 0) {
						pMem += 2;
						key.clear();
					}
				}

				// 0xEXA1
				private void cpu_skip_if_not_pressed() {
					if(key[V[(opcode & 0x0F00) >> 8]] == 0) {
						pMem += 2;
						key.clear();
					}
				}

			#endregion

			// 0xFXNN
			private void cpu_other() {
				switch (opcode & 0x00FF) {
					case 0x0007:
						cpu_get_delay();
						break;
					case 0x000A:
						cpu_read_key();
						break;
					case 0x0015:
						cpu_set_delay();
						break;
					case 0x0018:
						cpu_set_sound_timer();
						break;
					case 0x001E:
						cpu_increment_index();
						break;
					case 0x0029:
						cpu_get_char_font();
						break;
					case 0x0033:
						cpu_bcd_convert();
						break;
					case 0x0055:
						cpu_to_mem();
						break;
					case 0x0065:
						cpu_from_mem();
						break;
					default:
						cpu_null();
						break;
				}
			}

			#region others

				// 0xFX07
				private void cpu_get_delay() {
					V[(opcode & 0x0F00) >> 8] = delay_timer;
				}

				// 0xFX0A
				private void cpu_read_key() {
					bool keyPress = false;
					for(int i = 0; i < 16; ++i) {
						if(key[i] != 0) {
							V[(opcode & 0x0F00) >> 8] = (byte) i;
							keyPress = true;
						}
					}

					if(!keyPress) {					
						pMem -= 2;
					}
					key.clear();
				}

				// 0xFX15
				private void cpu_set_delay() {
					delay_timer = V[(opcode & 0x0F00) >> 8];
				}

				// 0xFX18
				private void cpu_set_sound_timer() {
					sound_timer = V[(opcode & 0x0F00) >> 8];
				}

				// 0xFX1E
				private void cpu_increment_index() {
					V[0xF] = (byte) (I + V[(opcode & 0x0F00) >> 8] > 0xFFF ? 1 : 0);
					I += V[(opcode & 0x0F00) >> 8];
				}

				// 0xFX29
				private void cpu_get_char_font() {
					I = (ushort) (V[(opcode & 0x0F00) >> 8] * 0x5);
				}

				// 0xFX33
				private void cpu_bcd_convert() {
					mem[I]     = (byte) (V[(opcode & 0x0F00) >> 8] / 100);
					mem[I + 1] = (byte) ((V[(opcode & 0x0F00) >> 8] / 10)  % 10);
					mem[I + 2] = (byte) ((V[(opcode & 0x0F00) >> 8] % 100) % 10);
				}

				// 0xFX55
				private void cpu_to_mem() {
					for (int i = 0; i <= ((opcode & 0x0F00) >> 8); ++i) {
						mem[I + i] = V[i];
					}

					I += (ushort) (((opcode & 0x0F00) >> 8) + 1);
				}

				// 0xFX65
				private void cpu_from_mem() {
					for (int i = 0; i <= ((opcode & 0x0F00) >> 8); ++i) {
						V[i] = mem[I + i];
					}

					I += (ushort) (((opcode & 0x0F00) >> 8) + 1);
				}

			#endregion

		#endregion
	}
}

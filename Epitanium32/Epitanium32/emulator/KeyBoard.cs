using System.Windows.Forms;

namespace Epitanium32 {
	public class KeyBoard {

		private byte[] key;

		public byte this[int i] {
			set {
				key[i] = (byte) (value == 0 ? 0 : 1);
			}
			get {
				return key[i];
			}
		}

		public KeyBoard() {
			clear();
		}

		public void onKeyPress(object sender, KeyPressEventArgs e) {
			switch (e.KeyChar) {
				case '0':
					key[0x0] = 1;
					break;
				case '1':
					key[0x1] = 1;
					break;
				case '2':
					key[0x2] = 1;
					break;
				case '3':
					key[0x3] = 1;
					break;
				case '4':
					key[0x4] = 1;
					break;
				case '5':
					key[0x5] = 1;
					break;
				case '6':
					key[0x6] = 1;
					break;
				case '7':
					key[0x7] = 1;
					break;
				case '8':
					key[0x8] = 1;
					break;
				case '9':
					key[0x9] = 1;
					break;
				case 'a':
					key[0xA] = 1;
					break;
				case 'b':
					key[0xB] = 1;
					break;
				case 'c':
					key[0xC] = 1;
					break;
				case 'd':
					key[0xD] = 1;
					break;
				case 'e':
					key[0xE] = 1;
					break;
				case 'f':
					key[0xF] = 1;
					break;
				default:
					break;
			}
		}

		public void clear() {
			key = new byte[16];
		}
	}
}

using System.IO;

namespace Epitanium32 {
	class Program {
		static void Main(string[] args) {
			if (args.Length == 0) {
				return;
			}
			byte[] raw = new byte[0xDFF];
			using (FileStream stream = new FileStream(args[0], FileMode.Open)) {
				stream.Read(raw, 0, 0xDFF);
			}

			Emulator p = new Emulator();
				p.init();
				p.loadRom(raw);
				p.run();
		}
	}
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Epitanium32 {
	public class GraphicsDevice : Form {
		public const int WIDTH = 65, HEIGHT = 33;

		private Graphics graphics;

		private Bitmap scr;

		private byte[,] screen;

		public GraphicsDevice(KeyBoard key) {
			this.Show();
			this.MinimumSize = this.MaximumSize = this.Size = new Size(WIDTH * 6 + 16, HEIGHT * 6 + 16);
			this.ControlBox = false;
			this.TopMost = true;

			this.KeyPress += new KeyPressEventHandler(key.onKeyPress);
			this.MouseDown += new MouseEventHandler(onMouseDown);

			graphics = this.CreateGraphics();
				graphics.CompositingMode = CompositingMode.SourceOver;
				graphics.CompositingQuality = CompositingQuality.HighSpeed;
				graphics.SmoothingMode = SmoothingMode.None;
				graphics.PixelOffsetMode = PixelOffsetMode.Half;
				graphics.InterpolationMode = InterpolationMode.NearestNeighbor;

			clear();
		}

		#region user32.dll native functions
			private const int WM_NCLBUTTONDOWN = 0xA1;
			private const int HT_CAPTION = 0x2;

			[DllImportAttribute("user32.dll")]
			private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);

			[DllImportAttribute("user32.dll")]
			private static extern bool ReleaseCapture();
		#endregion

		private void onMouseDown(object sender, MouseEventArgs e) {     
			if (e.Button == MouseButtons.Left) {
				ReleaseCapture();
				SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
			}
		}

		public void draw() {
			graphics.DrawImage(scr, 0, 0);
		}

		public void clear() {
			screen = new byte[WIDTH, HEIGHT];
			scr = new Bitmap(WIDTH, HEIGHT, PixelFormat.Format32bppPArgb);
			scr.SetResolution(16, 16);
			graphics.Clear(Color.Black);
		}

		public byte this[int x, int y] {
			set {
				if (value == 0) {
					screen[x, y] = (byte) 0;
					scr.SetPixel(x, y, Color.Black);
				} else {
					screen[x, y] = (byte) 1;
					scr.SetPixel(x, y, Color.White);
				}
			}
			get {
				return screen[x, y];
			}
		}
	}
}

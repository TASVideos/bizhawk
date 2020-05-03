﻿using System;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Emulation.Cores.Sega.MasterSystem;
using System.Drawing.Imaging;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class SmsVdpViewer : ToolFormBase, IToolFormAutoConfig
	{
		[RequiredService]
		private SMS Sms { get; set; }
		private VDP Vdp => Sms.Vdp;

		private int _palIndex;

		public SmsVdpViewer()
		{
			InitializeComponent();

			bmpViewTiles.ChangeBitmapSize(256, 128);
			bmpViewPalette.ChangeBitmapSize(16, 2);
			bmpViewBG.ChangeBitmapSize(256, 256);
		}

		static unsafe void Draw8x8(byte* src, int* dest, int pitch, int* pal)
		{
			int inc = pitch - 8;
			dest -= inc;
			for (int i = 0; i < 64; i++)
			{
				if ((i & 7) == 0)
					dest += inc;
				*dest++ = pal[*src++];
			}
		}

		static unsafe void Draw8x8hv(byte* src, int* dest, int pitch, int* pal, bool hflip, bool vflip)
		{
			int incX = hflip ? -1 : 1;
			int incY = vflip ? -pitch : pitch;
			if (hflip)
				dest -= incX * 7;
			if (vflip)
				dest -= incY * 7;
			incY -= incX * 8;
			for (int j = 0; j < 8; j++)
			{
				for (int i = 0; i < 8; i++)
				{
					*dest = pal[*src++];
					dest += incX;
				}
				dest += incY;
			}
		}

		unsafe void DrawTiles(int *pal)
		{
			var lockData = bmpViewTiles.BMP.LockBits(new Rectangle(0, 0, 256, 128), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			fixed (byte* src = Vdp.PatternBuffer)
			{
				for (int tile = 0; tile < 512; tile++)
				{
					int srcAddr = tile * 64;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destAddr = ty * 8 * pitch + tx * 8;
					Draw8x8(src + srcAddr, dest + destAddr, pitch, pal);
				}
			}
			bmpViewTiles.BMP.UnlockBits(lockData);
			bmpViewTiles.Refresh();
		}

		unsafe void DrawBG(int* pal)
		{
			int bgHeight = Vdp.FrameHeight == 192 ? 224 : 256;
			int maxTile = bgHeight * 4;
			if (bgHeight != bmpViewBG.BMP.Height)
			{
				bmpViewBG.Height = bgHeight;
				bmpViewBG.ChangeBitmapSize(256, bgHeight);
			}

			var lockData = bmpViewBG.BMP.LockBits(new Rectangle(0, 0, 256, bgHeight), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			fixed (byte* src = Vdp.PatternBuffer)
			fixed (byte* vram = Vdp.VRAM)
			{
				short* map = (short*)(vram + Vdp.CalcNameTableBase());

				for (int tile = 0; tile < maxTile; tile++)
				{
					short bgent = *map++;
					bool hFlip = (bgent & 1 << 9) != 0;
					bool vFlip = (bgent & 1 << 10) != 0;
					int* tpal = pal + ((bgent & 1 << 11) >> 7);
					int srcAddr = (bgent & 511) * 64;
					int tx = tile & 31;
					int ty = tile >> 5;
					int destAddr = ty * 8 * pitch + tx * 8;
					Draw8x8hv(src + srcAddr, dest + destAddr, pitch, tpal, hFlip, vFlip);
				}
			}
			bmpViewBG.BMP.UnlockBits(lockData);
			bmpViewBG.Refresh();
		}

		unsafe void DrawPal(int* pal)
		{
			var lockData = bmpViewPalette.BMP.LockBits(new Rectangle(0, 0, 16, 2), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
			int* dest = (int*)lockData.Scan0;
			int pitch = lockData.Stride / sizeof(int);

			for (int j = 0; j < 2; j++)
			{
				for (int i = 0; i < 16; i++)
				{
					*dest++ = *pal++;
				}

				dest -= 16;
				dest += pitch;
			}
			bmpViewPalette.BMP.UnlockBits(lockData);
			bmpViewPalette.Refresh();
		}

		protected override void UpdateValuesBefore()
		{
			unsafe
			{
				fixed (int* pal = Vdp.Palette)
				{
					DrawTiles(pal + _palIndex * 16);
					DrawBG(pal);
					DrawPal(pal);
				}
			}
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		public void Restart()
		{
			UpdateValues();
		}

		private void bmpViewPalette_MouseClick(object sender, MouseEventArgs e)
		{
			int p = Math.Min(Math.Max(e.Y / 16, 0), 1);
			_palIndex = p;
			unsafe
			{
				fixed (int* pal = Vdp.Palette)
				{
					DrawTiles(pal + _palIndex * 16);
				}
			}
		}

		private void VDPViewer_KeyDown(object sender, KeyEventArgs e)
		{
			if (ModifierKeys.HasFlag(Keys.Control) && e.KeyCode == Keys.C)
			{
				// find the control under the mouse
				Point m = Cursor.Position;
				Control top = this;
				Control found;
				do
				{
					found = top.GetChildAtPoint(top.PointToClient(m));
					top = found;
				} while (found != null && found.HasChildren);

				if (found is BmpView bv)
				{
					Clipboard.SetImage(bv.BMP);
				}
			}
		}

		private void CloseMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void saveTilesScreenshotToolStripMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewTiles.SaveFile();
		}

		private void SavePalettesScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewPalette.SaveFile();
		}

		private void SaveBgScreenshotMenuItem_Click(object sender, EventArgs e)
		{
			bmpViewBG.SaveFile();
		}
	}
}

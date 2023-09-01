﻿using System;
using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IVideoProvider
	{
		public int[] GetVideoBuffer() => _vidBuff;

		public int VirtualWidth => 320;

		public int VirtualHeight => 224;

		public int BufferWidth { get; private set; }

		public int BufferHeight => _vheight;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		private int[] _vidBuff = new int[0];
		private int _vheight;

		private void UpdateVideoInitial()
		{
			// hack: you should call update_video() here, but that gives you 256x192 on frame 0
			// and we know that we only use GPGX to emulate genesis games that will always be 320x224 immediately afterwards

			// so instead, just assume a 320x224 size now; if that happens to be wrong, it'll be fixed soon enough.

			BufferWidth = 320;
			_vheight = 224;
			_vidBuff = new int[BufferWidth * _vheight];
			for (int i = 0; i < _vidBuff.Length; i++)
			{
				_vidBuff[i] = unchecked((int)0xff000000);
			}
		}

		private unsafe void UpdateVideo()
		{
			if (Frame == 0)
			{
				UpdateVideoInitial();
				return;
			}

			using (_elf.EnterExit())
			{
				var src = IntPtr.Zero;

				Core.gpgx_get_video(out int gpwidth, out int gpheight, out int gppitch, ref src);

				BufferWidth = gpwidth;
				_vheight = gpheight;

				if (_settings.PadScreen320 && BufferWidth == 256)
					BufferWidth = 320;

				int xpad = (BufferWidth - gpwidth) / 2;
				int xpad2 = BufferWidth - gpwidth - xpad;

				if (_vidBuff.Length < BufferWidth * _vheight)
					_vidBuff = new int[BufferWidth * _vheight];

				int rinc = (gppitch / 4) - gpwidth;
				fixed (int* pdst_ = _vidBuff)
				{
					int* pdst = pdst_;
					int* psrc = (int*)src;

					for (int j = 0; j < gpheight; j++)
					{
						for (int i = 0; i < xpad; i++)
							*pdst++ = unchecked((int)0xff000000);
						for (int i = 0; i < gpwidth; i++)
							*pdst++ = *psrc++;
						for (int i = 0; i < xpad2; i++)
							*pdst++ = unchecked((int)0xff000000);
						psrc += rinc;
					}
				}
			}
		}

	}
}

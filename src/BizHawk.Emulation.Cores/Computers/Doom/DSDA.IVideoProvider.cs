using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IVideoProvider
	{
		public int[] GetVideoBuffer() => _vidBuff;

		public int VirtualWidth => BufferWidth;

		public int VirtualHeight => BufferHeight;

		public int PaletteSize { get; private set; }

		public int BufferWidth { get; private set; }

		public int BufferHeight { get; private set; }

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		private int[] _palBuffer = [ ];
		private int[] _vidBuff = [ ];

		private unsafe void UpdateVideo()
		{
			using (_elf.EnterExit())
			{
				var videoBufferSrc = IntPtr.Zero;
				var palletteBufferSrc = IntPtr.Zero;
				Core.dsda_get_video(out var width, out var height, out var pitch, ref videoBufferSrc, out var paletteSize, ref palletteBufferSrc);

				// Handling pallette buffer
				PaletteSize = paletteSize;
				if (_palBuffer.Length < PaletteSize) _palBuffer = new int[PaletteSize];
				var paletteBuffer = (int*) palletteBufferSrc.ToPointer();
				for (var i = 0; i < _palBuffer.Length; i++) _palBuffer[i] = paletteBuffer[i];

				// Handling video buffer
				BufferWidth = width;
				BufferHeight = height;
				if (_vidBuff.Length < BufferWidth * BufferHeight) _vidBuff = new int[BufferWidth * BufferHeight];
				var videoBuffer = (byte*) videoBufferSrc.ToPointer();
				for (var i = 0; i < _vidBuff.Length; i++) _vidBuff[i] = _palBuffer[videoBuffer[i]];
			}
		}
	}
}

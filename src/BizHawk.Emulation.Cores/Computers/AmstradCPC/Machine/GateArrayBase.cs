using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// The abstract class that all emulated models will inherit from
	/// * Amstrad Gate Array *
	/// https://web.archive.org/web/20170612081209/http://www.grimware.org/doku.php/documentations/devices/gatearray
	/// </summary>
	public abstract class GateArrayBase : IVideoProvider
	{
		public int Z80ClockSpeed = 4000000;
		public int FrameLength = 79872;

		private readonly CPCBase _machine;
		private Z80A CPU => _machine.CPU;
		private CRCT_6845 CRCT => _machine.CRCT;
		private IPSG PSG => _machine.AYDevice;

		/// <summary>
		/// CRTC Register constants
		/// </summary>
		public const int HOR_TOTAL = 0;
		public const int HOR_DISPLAYED = 1;
		public const int HOR_SYNC_POS = 2;
		public const int HOR_AND_VER_SYNC_WIDTHS = 3;
		public const int VER_TOTAL = 4;
		public const int VER_TOTAL_ADJUST = 5;
		public const int VER_DISPLAYED = 6;
		public const int VER_SYNC_POS = 7;
		public const int INTERLACE_SKEW = 8;
		public const int MAX_RASTER_ADDR = 9;
		public const int CUR_START_RASTER = 10;
		public const int CUR_END_RASTER = 11;
		public const int DISP_START_ADDR_H = 12;
		public const int DISP_START_ADDR_L = 13;
		public const int CUR_ADDR_H = 14;
		public const int CUR_ADDR_L = 15;
		public const int LPEN_ADDR_H = 16;
		public const int LPEN_ADDR_L = 17;

		/// <summary>
		/// The standard CPC Pallete (ordered by firmware #)
		/// http://www.cpcwiki.eu/index.php/CPC_Palette
		/// </summary>
		private static readonly int[] CPCFirmwarePalette =
		{
			Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0x80), // Blue
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0x80, 0x00, 0x00), // Red            
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0x00, 0x80, 0x00), // Green
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow
            Colors.ARGB(0x80, 0x80, 0x80), // White
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
        };

		/// <summary>
		/// The standard CPC Pallete (ordered by hardware #)
		/// http://www.cpcwiki.eu/index.php/CPC_Palette
		/// </summary>
		private static readonly int[] CPCHardwarePalette =
		{
			Colors.ARGB(0x80, 0x80, 0x80), // White
            Colors.ARGB(0x80, 0x80, 0x80), // White (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow
            Colors.ARGB(0x00, 0x00, 0x80), // Blue
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple
            Colors.ARGB(0x00, 0x80, 0x80), // Cyan
            Colors.ARGB(0xFF, 0x80, 0x80), // Pink
            Colors.ARGB(0xFF, 0x00, 0x80), // Purple (duplicate)
            Colors.ARGB(0xFF, 0xFF, 0x80), // Pastel Yellow (duplicate)
            Colors.ARGB(0xFF, 0xFF, 0x00), // Bright Yellow
            Colors.ARGB(0xFF, 0xFF, 0xFF), // Bright White
            Colors.ARGB(0xFF, 0x00, 0x00), // Bright Red
            Colors.ARGB(0xFF, 0x00, 0xFF), // Bright Magenta
            Colors.ARGB(0xFF, 0x80, 0x00), // Orange
            Colors.ARGB(0xFF, 0x80, 0xFF), // Pastel Magenta
            Colors.ARGB(0x00, 0x00, 0x80), // Blue (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x80), // Sea Green (duplicate)
            Colors.ARGB(0x00, 0xFF, 0x00), // Bright Green
            Colors.ARGB(0x00, 0xFF, 0xFF), // Bright Cyan
            Colors.ARGB(0x00, 0x00, 0x00), // Black
            Colors.ARGB(0x00, 0x00, 0xFF), // Bright Blue
            Colors.ARGB(0x00, 0x80, 0x00), // Green
            Colors.ARGB(0x00, 0x80, 0xFF), // Sky Blue
            Colors.ARGB(0x80, 0x00, 0x80), // Magenta
            Colors.ARGB(0x80, 0xFF, 0x80), // Pastel Green
            Colors.ARGB(0x80, 0xFF, 0x00), // Lime
            Colors.ARGB(0x80, 0xFF, 0xFF), // Pastel Cyan
            Colors.ARGB(0x80, 0x00, 0x00), // Red     
            Colors.ARGB(0x80, 0x00, 0xFF), // Mauve
            Colors.ARGB(0x80, 0x80, 0x00), // Yellow            
            Colors.ARGB(0x80, 0x80, 0xFF), // Pastel Blue
        };

		public GateArrayBase(CPCBase machine)
		{
			_machine = machine;
			PenColours = new int[17];
			SetupScreenSize();
			Reset();
		}

		/// <summary>
		/// Inits the pen lookup table
		/// </summary>
		public void SetupScreenMapping()
		{
			for (int m = 0; m < 4; m++)
			{
				Lookup[m] = new int[256 * 8];
				int pos = 0;

				for (int b = 0; b < 256; b++)
				{
					switch (m)
					{
						case 1:
							int pc = (((b & 0x80) >> 7) | ((b & 0x80) >> 2));
							Lookup[m][pos++] = pc;
							Lookup[m][pos++] = pc;
							pc = (((b & 0x40) >> 6) | ((b & 0x04) >> 1));
							Lookup[m][pos++] = pc;
							Lookup[m][pos++] = pc;
							pc = (((b & 0x20) >> 5) | (b & 0x02));
							Lookup[m][pos++] = pc;
							Lookup[m][pos++] = pc;
							pc = (((b & 0x10) >> 4) | ((b & 0x01) << 1));
							break;

						case 2:
							for (int i = 7; i >= 0; i--)
							{
								bool pixel_on = ((b & (1 << i)) != 0);
								Lookup[m][pos++] = pixel_on ? 1 : 0;
							}
							break;

						case 3:
						case 0:
							int pc2 = (b & 0xAA);
							pc2 = (
									((pc2 & 0x80) >> 7) |
									((pc2 & 0x08) >> 2) |
									((pc2 & 0x20) >> 3) |
									((pc2 & 0x02) << 2));
							Lookup[m][pos++] = pc2;
							Lookup[m][pos++] = pc2;
							Lookup[m][pos++] = pc2;
							Lookup[m][pos++] = pc2;
							pc2 = (b & 0x55);
							pc2 = (
								((pc2 & 0x40) >> 6) |
								((pc2 & 0x04) >> 1) |
								((pc2 & 0x10) >> 2) |
								((pc2 & 0x01) << 3));

							Lookup[m][pos++] = pc2;
							Lookup[m][pos++] = pc2;
							Lookup[m][pos++] = pc2;
							Lookup[m][pos++] = pc2;
							break;
					}
				}
			}
		}

		private readonly int[] PenColours;
		private int CurrentPen;
		private int ScreenMode;
		private int INTScanlineCnt;
		//private int VSYNCDelyCnt;

		private readonly int[][] Lookup = new int[4][];

		//private bool DoModeUpdate;

		//private int LatchedMode;
		//private int buffPos;

		public bool FrameEnd;

		public bool WaitLine;

		/// <summary>
		/// The gatearray runs on a 16Mhz clock
		/// (for the purposes of emulation, we will use a 4Mhz clock)
		/// From this it generates:
		/// 1Mhz clock for the CRTC chip
		/// 1Mhz clock for the AY-3-8912 PSG
		/// 4Mhz clock for the Z80 CPU
		/// </summary>
		public void ClockCycle()
		{
			// 4-phase clock
			for (int i = 1; i < 5; i++)
			{
				switch (i)
				{
					// Phase 1
					case 1:
						CRCT.ClockCycle();
						CPU.ExecuteOne();
						break;
					// Phase 2
					case 2:
						CPU.ExecuteOne();
						break;
					// Phase 3
					case 3:
						// video fetch
						break;
					// Phase 4
					case 4:
						// video fetch
						break;
				}
			}
		}

		/// <summary>
		/// Selects the pen
		/// </summary>
		public virtual void SetPen(BitArray bi)
		{
			if (bi[4])
			{
				// border select
				CurrentPen = 16;
			}
			else
			{
				// pen select
				byte[] b = new byte[1];
				bi.CopyTo(b, 0);
				CurrentPen = b[0] & 0x0f;
			}
		}

		/// <summary>
		/// Selects colour for the currently selected pen
		/// </summary>
		public virtual void SetPenColour(BitArray bi)
		{
			byte[] b = new byte[1];
			bi.CopyTo(b, 0);
			var colour = b[0] & 0x1f;
			PenColours[CurrentPen] = colour;
		}

		/// <summary>
		/// Returns the actual ARGB pen colour value
		/// </summary>
		public virtual int GetPenColour(int idx)
		{
			return CPCHardwarePalette[PenColours[idx]];
		}

		/// <summary>
		/// Screen mode and ROM config
		/// </summary>
		public virtual void SetReg2(BitArray bi)
		{
			byte[] b = new byte[1];
			bi.CopyTo(b, 0);

			// screen mode
			var mode = b[0] & 0x03;
			ScreenMode = mode;

			// ROM

			// upper
			if ((b[0] & 0x08) != 0)
			{
				_machine.UpperROMPaged = false;
			}
			else
			{
				_machine.UpperROMPaged = true;
			}

			// lower
			if ((b[0] & 0x04) != 0)
			{
				_machine.LowerROMPaged = false;
			}
			else
			{
				_machine.LowerROMPaged = true;
			}

			// INT delay
			if ((b[0] & 0x10) != 0)
			{
				INTScanlineCnt = 0;
			}
		}

		/// <summary>
		/// Only available on machines with a 64KB memory expansion
		/// Default assume we don't have this
		/// </summary>
		public virtual void SetRAM(BitArray bi)
		{
			return;
		}

		public void InterruptACK()
		{
			INTScanlineCnt &= 0x01f;
		}


		public void Reset()
		{
			CurrentPen = 0;
			ScreenMode = 1;
			for (int i = 0; i < 17; i++)
				PenColours[i] = 0;
			INTScanlineCnt = 0;
			//VSYNCDelyCnt = 0;
		}

		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public bool ReadPort(ushort port, ref int result)
		{
			// gate array is OUT only
			return false;
		}

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		public bool WritePort(ushort port, int result)
		{
			BitArray portBits = new BitArray(BitConverter.GetBytes(port));
			BitArray dataBits = new BitArray(new[] {(byte)result});

			// The gate array responds to port 0x7F
			bool accessed = !portBits[15];
			if (!accessed)
				return false;

			// Bit 9 and 8 of the data byte define the function to access
			if (!dataBits[6] && !dataBits[7])
			{
				// select pen
				SetPen(dataBits);
			}

			if (dataBits[6] && !dataBits[7])
			{
				// select colour for selected pen
				SetPenColour(dataBits);
			}

			if (!dataBits[6] && dataBits[7])
			{
				// select screen mode, ROM configuration and interrupt control
				SetReg2(dataBits);
			}

			if (dataBits[6] && dataBits[7])
			{
				// RAM memory management
				SetRAM(dataBits);
			}

			return true;
		}

		/// <summary>
		/// Video output buffer
		/// </summary>
		public int[] ScreenBuffer;

		private int _virtualWidth;
		private int _virtualHeight;
		private int _bufferWidth;
		private int _bufferHeight;

		public int BackgroundColor => CPCHardwarePalette[16];

		public int VirtualWidth
		{
			get => _virtualWidth;
			set => _virtualWidth = value;
		}

		public int VirtualHeight
		{
			get => _virtualHeight;
			set => _virtualHeight = value;
		}

		public int BufferWidth
		{
			get => _bufferWidth;
			set => _bufferWidth = value;
		}

		public int BufferHeight
		{
			get => _bufferHeight;
			set => _bufferHeight = value;
		}

		public int VsyncNumerator
		{
			get => Z80ClockSpeed * 50;
			set { }
		}

		public int VsyncDenominator => Z80ClockSpeed;

		public int[] GetVideoBuffer() => ScreenBuffer;

		protected void SetupScreenSize()
		{
			/*
             *  Rect Pixels:        Mode 0: 160×200 pixels with 16 colors (4 bpp)
                Sqaure Pixels:      Mode 1: 320×200 pixels with 4 colors (2 bpp)
                Rect Pixels:        Mode 2: 640×200 pixels with 2 colors (1 bpp)
                Rect Pixels:        Mode 3: 160×200 pixels with 4 colors (2bpp) (this is not an official mode, but rather a side-effect of the hardware)
             * 
             * */

			// define maximum screen buffer size
			// to fit all possible resolutions, 640x400 should do it
			// therefore a scanline will take two buffer rows
			// and buffer columns will be:
			//  Mode 1: 2 pixels
			//  Mode 2: 1 pixel
			//  Mode 0: 4 pixels
			//  Mode 3: 4 pixels

			BufferWidth = 640;
			BufferHeight = 400;
			VirtualHeight = BufferHeight;
			VirtualWidth = BufferWidth;
			ScreenBuffer = new int[BufferWidth * BufferHeight];
			croppedBuffer = ScreenBuffer;
		}

		protected int[] croppedBuffer;
	}
}

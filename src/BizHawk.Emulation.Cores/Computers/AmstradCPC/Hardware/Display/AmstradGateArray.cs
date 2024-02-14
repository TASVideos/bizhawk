using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.Z80A;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// * Amstrad Gate Array *
	/// http://www.cpcwiki.eu/index.php/Gate_Array
	/// https://web.archive.org/web/20170612081209/http://www.grimware.org/doku.php/documentations/devices/gatearray
	/// </summary>
	public class AmstradGateArray : IPortIODevice, IVideoProvider
	{
		private readonly CPCBase _machine;
		private Z80A CPU => _machine.CPU;
		private CRCT_6845 CRCT => _machine.CRCT;
		//private CRTDevice CRT => _machine.CRT;
		private IPSG PSG => _machine.AYDevice;
		private NECUPD765 FDC => _machine.UPDDiskDevice;
		private DatacorderDevice DATACORDER => _machine.TapeDevice;
		private ushort BUSRQ => CPU.MEMRQ[CPU.bus_pntr];
		public const ushort PCh = 1;

		private GateArrayType ChipType;

		/// <summary>
		/// The standard CPC Pallete (ordered by firmware #)
		/// http://www.cpcwiki.eu/index.php/CPC_Palette
		/// </summary>
		public static readonly int[] CPCFirmwarePalette =
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
		public static readonly int[] CPCHardwarePalette =
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

		/// <summary>
		/// The Gate Array Clock Speed
		/// </summary>
		public int GAClockSpeed = 16000000;

		/// <summary>
		/// The CPU Clock Speed
		/// </summary>
		public int Z80ClockSpeed = 4000000;

		/// <summary>
		/// CRCT Clock Speed
		/// </summary>
		public int CRCTClockSpeed = 1000000;

		/// <summary>
		/// AY-3-8912 Clock Speed
		/// </summary>
		public int PSGClockSpeed = 1000000;

		/// <summary>
		/// Number of CPU cycles in one frame
		/// </summary>
		public int FrameLength = 79872;

		/// <summary>
		/// Number of Gate Array cycles within one frame
		/// </summary>
		public int GAFrameLength = 319488;

		public AmstradGateArray(CPCBase machine, GateArrayType chipType)
		{
			_machine = machine;
			ChipType = chipType;
			//PenColours = new int[17];
			borderType = _machine.CPC.SyncSettings.BorderType;
			SetupScreenSize();
			//Reset();

			CRCT.AttachHSYNCCallback(OnHSYNC);
			CRCT.AttachVSYNCCallback(OnVSYNC);

			CurrentLine = new CharacterLine();
			InitByteLookup();
			CalculateNextScreenMemory();
		}

		/// <summary>
		/// PENR (register 0) - Pen Selection
		/// This register can be used to select one of the 17 color-registers (pen 0 to 15 or the border). 
		/// It will remain selected until another PENR command is executed.
		/// PENR	    Index	
		/// 7	6	5	4	3	2	1	0	color register selected
		/// 0	0	0	0	n   n   n   n   pen n from 0 to 15 (4bits)
		/// 0	0	0	1	x   x   x   x   border
		/// 
		/// x can be 0 or 1, it doesn't matter
		/// </summary>        
		private byte _PENR;
		public byte PENR
		{
			get => _PENR;
			set
			{
				_PENR = value;
				if (_PENR.Bit(4))
				{
					// border select
					CurrentPen = 16;
				}
				else
				{
					// pen select
					CurrentPen = _PENR & 0x0f;
				}
			}
		}

		/// <summary>
		/// 0-15:   Pen Registers
		/// 16:     Border Colour
		/// </summary>
		public int[] ColourRegisters = new int[17];

		/// <summary>
		/// The currently selected Pen
		/// </summary>
		private int CurrentPen;

		/// <summary>
		/// INKR (register 1) - Colour Selection
		/// This register takes a 5bits parameter which is a color-code. This color-code range from 0 to 31 but there's only 27 differents colors 
		/// (because the Gate Array use a 3-states logic on the R,G and B signals, thus 3x3x3=27).
		/// INKR	    Color	
		/// 7	6	5	4	3	2	1	0	
		/// 0	1	0	n   n   n   n   n   where n is a color code (5 bits)
		/// 
		/// The PEN affected by the INKR command is updated (almost) immediatly
		/// </summary>        
		private byte _INKR;
		public byte INKR
		{
			get => _INKR;
			set
			{
				_INKR = value;
				ColourRegisters[CurrentPen] = _INKR & 0x1f;
			}
		}

		/// <summary>
		/// RMR (register 2) - Select screen mode and ROM configuration
		/// This register control the interrupt counter (reset), the upper and lower ROM paging and the video mode.
		/// RMR	        Commands
		/// 7	6	5	4	3	2	1	0
		/// 1	0	0	I   UR  LR  VM-->
		/// 
		/// I   : if set (1), this will reset the interrupt counter
		/// UR  : Enable (0) or Disable (1) the upper ROM paging (&amp;C000 to &amp;FFFF). You can select which upper ROM with the I/O address &amp;DF00
		/// LR  : Enable (0) or Disable (1) the lower ROM paging
		/// VM  : Select the video mode 0,1,2 or 3 (it will take effect after the next HSync)
		/// </summary>
		private byte _RMR;
		public byte RMR
		{
			get => _RMR;
			set
			{
				_RMR = value;
				//ScreenMode = _RMR & 0x03;
				var sm = _RMR & 0x03;
				if (sm != 1)
				{

				}

				if ((_RMR & 0x08) != 0)
					_machine.UpperROMPaged = false;
				else
					_machine.UpperROMPaged = true;

				if ((_RMR & 0x04) != 0)
					_machine.LowerROMPaged = false;
				else
					_machine.LowerROMPaged = true;

				if (_RMR.Bit(4))
				{
					// reset interrupt counter
					InterruptCounter = 0;
				}
			}
		}

		/// <summary>
		/// RAMR (register 3) - RAM Banking
		/// This register exists only in CPCs with 128K RAM (like the CPC 6128, or CPCs with Standard Memory Expansions)
		/// Note: In the CPC 6128, the register is a separate PAL that assists the Gate Array chip
		/// 
		/// Bit	Value	Function
		/// 7	1	    Gate Array function 3
		/// 6	1
		/// 5	b	    64K bank number(0..7); always 0 on an unexpanded CPC6128, 0-7 on Standard Memory Expansions
		/// 4	b
		/// 3	b
		/// 2	x       RAM Config(0..7)
		/// 1	x       ""  
		/// 0	x       ""
		/// 
		/// The 3bit RAM Config value is used to access the second 64K of the total 128K RAM that is built into the CPC 6128 or the additional 64K-512K of standard memory expansions. 
		/// These contain up to eight 64K ram banks, which are selected with bit 3-5. A standard CPC 6128 only contains bank 0. Normally the register is set to 0, so that only the 
		/// first 64K RAM are used (identical to the CPC 464 and 664 models). The register can be used to select between the following eight predefined configurations only:
		/// 
		/// -Address-   0       1       2       3       4       5       6       7
		/// 0000-3FFF   RAM_0   RAM_0   RAM_4   RAM_0   RAM_0   RAM_0   RAM_0   RAM_0
		/// 4000-7FFF   RAM_1   RAM_1   RAM_5   RAM_3   RAM_4   RAM_5   RAM_6   RAM_7
		/// 8000-BFFF   RAM_2   RAM_2   RAM_6   RAM_2   RAM_2   RAM_2   RAM_2   RAM_2
		/// C000-FFFF   RAM_3   RAM_7   RAM_7   RAM_7   RAM_3   RAM_3   RAM_3   RAM_3
		/// 
		/// The Video RAM is always located in the first 64K, VRAM is in no way affected by this register
		/// </summary>
		private byte _RAMR;
		/// <summary>
		/// This is actually implemented outside of here. These values do nothing.
		/// </summary>
		public byte RAMR
		{
			get => _RAMR;
			set => _RAMR = value;
		}

		/// <summary>
		/// The selected screen mode (updated after every HSYNC)
		/// </summary>
		private int ScreenMode;

		/// <summary>
		/// Simulates the internal 6bit INT counter
		/// </summary>
		private int _interruptCounter;
		public int InterruptCounter
		{
			get => _interruptCounter;
			set => _interruptCounter = value;
		}

		/// <summary>
		/// Interrupts are delayed when a VSYNC occurs
		/// </summary>
		private int VSYNCDelay;

		/// <summary>
		/// Signals that the frame end has been reached
		/// </summary>
		public bool FrameEnd;

		/// <summary>
		/// Internal phase clock
		/// </summary>
		private int ClockCounter;

		/// <summary>
		/// Master frame clock counter
		/// </summary>
		public int FrameClock;

		/// <summary>
		/// Simulates the gate array memory /WAIT line
		/// </summary>
		private bool WaitLine;

		/// <summary>
		/// 16-bit address - read from the CRCT
		/// </summary>
		private short _MA;
		private short MA
		{
			get
			{
				_MA = CRCT.MA;
				return _MA;
			}
		}

		/// <summary>
		/// Set when the HSYNC signal is detected from the CRCT
		/// </summary>
		private bool HSYNC;

		//      /// <summary>
		//      /// Is set when an initial HSYNC is seen from the CRCT
		//      /// On real hardware interrupt generation is based on the falling edge of the HSYNC signal
		//      /// So in this emulation, once the falling edge is detected, interrupt processing happens
		//      /// </summary>
		//      private bool HSYNC_falling;

		/// <summary>
		/// Used to count HSYNCs during a VSYNC
		/// </summary>
		private int HSYNC_counter;

		/// <summary>
		/// Set when the VSYNC signal is detected from the CRCT
		/// </summary>
		private bool VSYNC;

		/// <summary>
		/// TRUE when the /INT pin is held low
		/// </summary>
		private bool InterruptRaised;

		/// <summary>
		/// Counts the GA cycles that the /INT pin should be held low
		/// </summary>
		private int InterruptHoldCounter;

		/// <summary>
		/// Set at the start of a new frame
		/// </summary>
		public bool IsNewFrame;

		/// <summary>
		/// Set when a new line is beginning
		/// </summary>
		public bool IsNewLine;

		/// <summary>
		/// Horizontal Character Counter
		/// </summary>
		private int HCC;

		/// <summary>
		/// Vertical Line Counter
		/// </summary>
		private int VLC;

		/// <summary>
		/// The first video byte fetched
		/// </summary>
		private byte VideoByte1;

		/// <summary>
		/// The second video byte fetched
		/// </summary>
		private byte VideoByte2;

		/// <summary>
		/// Called every CPU cycle
		/// In reality the GA is clocked at 16Mhz (4 times the frequency of the CPU)
		/// Therefore this method has to take care of:
		/// 4 GA cycles
		/// 1 CRCT cycle every 4 calls
		/// 1 PSG cycle every 4 calls
		/// 1 CPU cycle (uncontended)
		/// </summary>
		public void ClockCycle()
		{
			// gatearray uses 4-phase clock to supply clocks to other devices
			switch (ClockCounter)
			{
				case 0:
					CRCT.ClockCycle();
					WaitLine = false;
					break;
				case 1:
					WaitLine = true;
					// detect new scanline and upcoming new frame on next render cycle
					//FrameDetector();
					break;
				case 2:
					// video fetch
					WaitLine = true;
					//FetchByte(1);
					break;
				case 3:
					// video fetch and render
					WaitLine = true;
					//FetchByte(2);
					GACharacterCycle();
					//PixelGenerator();
					break;
			}

			if (!HSYNC && CRCT.HSYNC)
			{
				HSYNC = true;
			}

			// run the interrupt generator routine
			InterruptGenerator();

			if (!CRCT.HSYNC)
			{
				HSYNC = false;
			}

			// conditional CPU cycle
			DoConditionalCPUCycle();

			AdvanceClock();
		}

		/// <summary>
		/// Increments the internal clock counters by one
		/// </summary>
		private void AdvanceClock()
		{
			FrameClock++;
			ClockCounter++;

			if (ClockCounter == 4)
				ClockCounter = 0;

			// check for frame end
			if (FrameClock == FrameLength)
			{
				FrameEnd = true;
			}
		}

		/// <summary>
		/// Runs a 4 Mhz CPU cycle if neccessary
		/// /WAIT line status is a factor here
		/// </summary>
		private void DoConditionalCPUCycle()
		{
			if (!WaitLine)
			{
				// /WAIT line is NOT active
				CPU.ExecuteOne();
				return;
			}

			// /WAIT line is active            
			switch (ClockCounter)
			{
				case 2:
				case 3:
					// gate array video fetch is occuring
					// check for memory access
					if (BUSRQ > 0)
					{
						// memory action upcoming - CPU clock is halted
						CPU.TotalExecutedCycles++;
					}
					break;

				case 1:
					// CPU accesses RAM if it's performing a non-opcode read or write
					// assume for now that an opcode fetch is always looking at PC
					if (BUSRQ == PCh)
					{
						// opcode fetch memory action upcoming - CPU clock is halted
						CPU.TotalExecutedCycles++;
					}
					else
					{
						// no fetch, or non-opcode fetch
						CPU.ExecuteOne();
					}
					break;
			}
		}

		/// <summary>
		/// The CRCT builds the picture in a strange way, so that the top left of the display area is the first pixel from
		/// video RAM. The borders come either side of the HSYNC and VSYNCs later on:
		/// https://web.archive.org/web/20170501112330im_/http://www.grimware.org/lib/exe/fetch.php/documentations/devices/crtc.6845/crtc.standard.video.frame.png?w=800&amp;h=500
		/// Therefore when the gate array initialises, we will attempt end the frame early in order to
		/// sync up at the point where VSYNC is active and HSYNC just begins. This is roughly how a CRT monitor would display the picture.
		/// The CRT would start a new line at the point where an HSYNC is detected.
		/// </summary>
		private void FrameDetector()
		{
			if (CRCT.HSYNC && !IsNewLine)
			{
				// start of a new line on the next render cycle
				IsNewLine = true;

				// process scanline
				//CRT.CurrentLine.CommitScanline();

				// check for end of frame
				if (CRCT.VSYNC && !IsNewFrame)
				{
					// start of a new frame on the next render cycle
					IsNewFrame = true;
					//FrameEnd = true;                    
					VLC = 0;
				}
				else if (!CRCT.VSYNC)
				{
					// increment line counter
					VLC++;
					IsNewFrame = false;
				}

				HCC = 0;

				// update screenmode
				//ScreenMode = RMR & 0x03;
				//CRT.CurrentLine.InitScanline(ScreenMode, VLC);
			}
			else if (!CRCT.HSYNC)
			{
				// reset the flags
				IsNewLine = false;
				IsNewFrame = false;
			}
		}

		/// <summary>
		/// Handles interrupt generation
		/// </summary>
		private void InterruptGenerator()
		{
			if (HSYNC && !CRCT.HSYNC)
			{
				// falling edge of the HSYNC detected
				InterruptCounter++;

				if (CRCT.VSYNC)
				{
					if (HSYNC_counter >= 2)
					{
						// x2 HSYNC have happened during VSYNC
						if (InterruptCounter >= 32)
						{
							// no interrupt
							InterruptCounter = 0;
						}
						else if (InterruptCounter < 32)
						{
							// interrupt
							InterruptRaised = true;
							InterruptCounter = 0;
						}

						HSYNC_counter = 0;
					}
					else
					{
						HSYNC_counter++;
					}
				}

				if (InterruptCounter == 52)
				{
					// gatearray should raise an interrupt
					InterruptRaised = true;
					InterruptCounter = 0;
				}
			}

			if (InterruptRaised)
			{
				// interrupt should been raised
				CPU.FlagI = true;
				InterruptHoldCounter++;

				// the INT signal should be held low for 1.4us.
				// in gatearray cycles, this equates to 22.4
				// we will round down to 22 for emulation purposes
				if (InterruptHoldCounter >= 22)
				{
					CPU.FlagI = false;
					InterruptRaised = false;
					InterruptHoldCounter = 0;
				}
			}
		}

		/// <summary>
		/// Builds up current scanline character information
		/// Ther GA modifies HSYNC and VSYNC signals before they are sent to the monitor
		/// This is handled here
		/// Runs at 1Mhz
		/// </summary>
		private void GACharacterCycle()
		{
			if (CRCT.VSYNC && CRCT.HSYNC)
			{
				// both hsync and vsync active
				CurrentLine.AddCharacter(Phase.HSYNCandVSYNC);
			}
			else if (CRCT.VSYNC)
			{
				// vsync is active but hsync is not
				CurrentLine.AddCharacter(Phase.VSYNC);
			}
			else if (CRCT.HSYNC)
			{
				// hsync is active but vsync is not
				CurrentLine.AddCharacter(Phase.HSYNC);
			}
			else if (!CRCT.DISPTMG)
			{
				// border generation
				CurrentLine.AddCharacter(Phase.BORDER);
			}
			else if (CRCT.DISPTMG)
			{
				// pixels generated from video RAM
				CurrentLine.AddCharacter(Phase.DISPLAY);
			}
		}

		/// <summary>
		/// Holds the upcoming video RAM addresses for the next scanline
		/// Firmware default size is 80 (40 characters - 2 bytes per character)
		/// </summary>
		private ushort[] NextVidRamLine = new ushort[40 * 2];

		/// <summary>
		/// The current character line we are working from
		/// </summary>
		private readonly CharacterLine CurrentLine;

		/// <summary>
		/// List of screen lines as they are built up
		/// </summary>
		private readonly List<CharacterLine> ScreenLines = new List<CharacterLine>();

		/// <summary>
		/// Pixel value lookups for every scanline byte value
		/// Based on the lookup at https://github.com/gavinpugh/xnacpc
		/// </summary>
		private readonly int[][] ByteLookup = new int[4][];
		private void InitByteLookup()
		{
			int pix;
			for (int m = 0; m < 4; m++)
			{
				int pos = 0;
				ByteLookup[m] = new int[256 * 8];
				for (int b = 0; b < 256; b++)
				{
					switch (m)
					{
						case 0:
							pix = b & 0xaa;
							pix = (((pix & 0x80) >> 7) | ((pix & 0x08) >> 2) | ((pix & 0x20) >> 3) | ((pix & 0x02) << 2));
							for (int c = 0; c < 4; c++)
								ByteLookup[m][pos++] = pix;
							pix = b & 0x55;
							pix = (((pix & 0x40) >> 6) | ((pix & 0x04) >> 1) | ((pix & 0x10) >> 2) | ((pix & 0x01) << 3));
							for (int c = 0; c < 4; c++)
								ByteLookup[m][pos++] = pix;
							break;
						case 1:
							pix = (((b & 0x80) >> 7) | ((b & 0x08) >> 2));
							ByteLookup[m][pos++] = pix;
							ByteLookup[m][pos++] = pix;
							pix = (((b & 0x40) >> 6) | ((b & 0x04) >> 1));
							ByteLookup[m][pos++] = pix;
							ByteLookup[m][pos++] = pix;
							pix = (((b & 0x20) >> 5) | (b & 0x02));
							ByteLookup[m][pos++] = pix;
							ByteLookup[m][pos++] = pix;
							pix = (((b & 0x10) >> 4) | ((b & 0x01) << 1));
							ByteLookup[m][pos++] = pix;
							ByteLookup[m][pos++] = pix;
							break;
						case 2:
							for (int i = 7; i >= 0; i--)
								ByteLookup[m][pos++] = ((b & (1 << i)) != 0) ? 1 : 0;
							break;
						case 3:
							pix = b & 0xaa;
							pix = (((pix & 0x80) >> 7) | ((pix & 0x08) >> 2) | ((pix & 0x20) >> 3) | ((pix & 0x02) << 2));
							for (int c = 0; c < 4; c++)
								ByteLookup[m][pos++] = pix;
							pix = b & 0x55;
							pix = (((pix & 0x40) >> 6) | ((pix & 0x04) >> 1) | ((pix & 0x10) >> 2) | ((pix & 0x01) << 3));
							for (int c = 0; c < 4; c++)
								ByteLookup[m][pos++] = pix;
							break;
					}
				}
			}
		}

		/// <summary>
		/// Runs at HSYNC *AFTER* the scanline has been commmitted
		/// Sets up the upcoming memory addresses for the next scanline
		/// </summary>
		private void CalculateNextScreenMemory()
		{
			var verCharCount = CRCT.VCC;
			var verRasCount = CRCT.VLC;

			var screenWidthByteCount = CRCT.DisplayWidth * 2;
			NextVidRamLine = new ushort[screenWidthByteCount * 2];
			var screenHeightCharCount = CRCT.DisplayHeightInChars;
			var screenAddress = CRCT.MA;

			int baseAddress = ((screenAddress << 2) & 0xf000);
			int offset = (screenAddress * 2) & 0x7ff;

			int x = offset + ((verCharCount * screenWidthByteCount) & 0x7ff);
			int y = baseAddress + (verRasCount * 0x800);

			for (int b = 0; b < screenWidthByteCount; b++)
			{
				NextVidRamLine[b] = (ushort)(y + x);
				x++;
				x &= 0x7ff;
			}
		}

		/// <summary>
		/// Called at the start of HSYNC, this renders the currently built-up scanline
		/// </summary>
		private void RenderScanline()
		{
			// memory addresses
			int cRow = CRCT.VCC;
			int cRas = CRCT.VLC;

			int screenByteWidth = CRCT.DisplayWidth * 2;
			var screenHeightCharCount = CRCT.DisplayHeightInChars;
			//CalculateNextScreenMemory();
			var crctAddr = CRCT.DStartHigh << 8;
			crctAddr |= CRCT.DStartLow;
			var baseAddr = ((crctAddr << 2) & (0xF000)); //CRCT.VideoPageBase;//
			var baseOffset = (crctAddr * 2) & 0x7FF; //CRCT.VideoRAMOffset * 2; //
			var xA = baseOffset + ((cRow * screenByteWidth) & 0x7ff);
			var yA = baseAddr + (cRas * 2048);

			// border and display
			int pix = 0;
			int scrByte = 0;

			for (int i = 0; i < CurrentLine.PhaseCount; i++)
			{
				// every character renders 8 pixels
				switch (CurrentLine.Phases[i])
				{
					case Phase.NONE:
						break;

					case Phase.HSYNC:
						break;
					case Phase.HSYNCandVSYNC:
						break;
					case Phase.VSYNC:
						break;
					case Phase.BORDER:
						// output current border colour
						for (pix = 0; pix < 16; pix++)
						{
							CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[16]]);
						}
						break;
					case Phase.DISPLAY:
						// each character references 2 bytes in video RAM
						byte data;

						for (int by = 0; by < 2; by++)
						{
							ushort addr = (ushort)(yA + xA);
							data = _machine.FetchScreenMemory(addr);
							scrByte++;

							switch (CurrentLine.ScreenMode)
							{
								case 0:
									pix = data & 0xaa;
									pix = (((pix & 0x80) >> 7) | ((pix & 0x08) >> 2) | ((pix & 0x20) >> 3) | ((pix & 0x02) << 2));
									for (int c = 0; c < 4; c++)
										CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									pix = data & 0x55;
									pix = (((pix & 0x40) >> 6) | ((pix & 0x04) >> 1) | ((pix & 0x10) >> 2) | ((pix & 0x01) << 3));
									for (int c = 0; c < 4; c++)
										CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									break;
								case 1:
									pix = (((data & 0x80) >> 7) | ((data & 0x08) >> 2));
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									pix = (((data & 0x40) >> 6) | ((data & 0x04) >> 1));
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									pix = (((data & 0x20) >> 5) | (data & 0x02));
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									pix = (((data & 0x10) >> 4) | ((data & 0x01) << 1));
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									break;
								case 2:
									pix = data;
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(7) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(6) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(5) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(4) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(3) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(2) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(1) ? 1 : 0]]);
									CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix.Bit(0) ? 1 : 0]]);
									break;
								case 3:
									pix = data & 0xaa;
									pix = (((pix & 0x80) >> 7) | ((pix & 0x08) >> 2) | ((pix & 0x20) >> 3) | ((pix & 0x02) << 2));
									for (int c = 0; c < 4; c++)
										CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									pix = data & 0x55;
									pix = (((pix & 0x40) >> 6) | ((pix & 0x04) >> 1) | ((pix & 0x10) >> 2) | ((pix & 0x01) << 3));
									for (int c = 0; c < 4; c++)
										CurrentLine.Pixels.Add(CPCHardwarePalette[ColourRegisters[pix]]);
									break;
							}

							xA++;
							xA &= 0x7ff;
						}

						break;
				}
			}

			// add to the list
			ScreenLines.Add(new CharacterLine
			{
				ScreenMode = CurrentLine.ScreenMode,
				Phases = CurrentLine.Phases.ToList(),
				Pixels = CurrentLine.Pixels.ToList()
			});
		}

		/// <summary>
		/// Called when the Z80 acknowledges an interrupt
		/// </summary>
		public void IORQA()
		{
			// bit 5 of the interrupt counter is reset
			InterruptCounter &= ~(1 << 5);
		}

		private int slCounter = 0;
		private int slBackup = 0;

		/// <summary>
		/// Fired when the CRCT flags HSYNC
		/// </summary>
		public void OnHSYNC()
		{
			HSYNC = true;
			slCounter++;

			// commit the scanline
			RenderScanline();

			// setup vid memory for next scanline
			CalculateNextScreenMemory();

			if (CRCT.VLC == 0)
			{
				// update screenmode
				ScreenMode = _RMR & 0x03;
			}

			// setup scanline for next
			CurrentLine.Clear(ScreenMode);
		}

		/// <summary>
		/// Fired when the CRCT flags VSYNC
		/// </summary>
		public void OnVSYNC()
		{
			FrameEnd = true;
			slBackup = slCounter;
			slCounter = 0;
		}

		public int[] ScreenBuffer;

		private int _virtualWidth;
		private int _virtualHeight;
		private int _bufferWidth;
		private int _bufferHeight;

		public int BackgroundColor => CPCHardwarePalette[0];

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

		public int SysBufferWidth;
		public int SysBufferHeight;

		public int VsyncNumerator
		{
			get => 200000000;
			set { }
		}

		public int VsyncDenominator => Z80ClockSpeed;

		public int[] GetVideoBuffer()
		{
			// get only lines that have pixel data
			var lines = ScreenLines.Where(a => a.Pixels.Count > 0);

			int pos = 0;
			int lCount = 0;
			foreach (var l in lines)
			{
				var lCop = l.Pixels.ToList();
				var len = l.Pixels.Count;
				if (l.Phases.Contains(Phase.VSYNC) && l.Phases.Contains(Phase.BORDER))
					continue;

				if (len < 320)
					continue;

				var pad = BufferWidth - len;
				if (pad < 0)
				{
					// trim the left and right
					var padPos = pad * -1;
					var excessL = padPos / 2;
					var excessR = excessL + (padPos % 2);
					for (var i = 0; i < excessL; i++) lCop.RemoveAt(0);
					for (var i = 0; i < excessL; i++) lCop.RemoveAt(lCop.Count - 1); //TODO should be using excessR?
				}

				var lPad = pad / 2;
				var rPad = lPad + (pad % 2);

				for (int i = 0; i < 2; i++)
				{
					lCount++;

					for (int pL = 0; pL < lPad; pL++)
					{
						ScreenBuffer[pos++] = 0;
					}

					for (int pix = 0; pix < lCop.Count; pix++)
					{
						ScreenBuffer[pos++] = lCop[pix];
					}

					for (int pR = 0; pR < rPad; pR++)
					{
						ScreenBuffer[pos++] = 0;
					}
				}

				if (lCount >= BufferHeight - 2)
				{
					break;
				}
			}

			ScreenLines.Clear();

			return ScreenBuffer;
			/*
            switch (borderType)
            {
                // crop to 768x272 (544)
                // borders 64px - 64 scanlines
                case AmstradCPC.BorderType.Uniform:
                    /*
                    var slSize = 64;
                    var dispLines = (24 * 8) * 2;
                    var origTopBorder = (7 * 8) * 2;                    
                    var origBotBorder = (5 * 8) * 2;

                    var lR = 16;
                    var rR = 16;

                    var trimTop = origTopBorder - slSize;

                    var startP = SysBufferWidth * (origTopBorder - 64);
                    var index1 = 0;

                    // line by line
                    int cnt = 0;
                    for (int line = startP; line < ScreenBuffer.Length; line += SysBufferWidth)
                    {
                        cnt++;
                        // pixels in line
                        for (int p = lR; p < SysBufferWidth - rR; p++)
                        {
                            if (index1 == croppedBuffer.Length)
                                break;

                            croppedBuffer[index1++] = ScreenBuffer[line + p];
                        }
                    }
                    return croppedBuffer;
                    */
			/*
			var slWidth = BufferWidth;
			return ScreenBuffer;

			break;


	}
	*/
			//return ScreenBuffer;
		}

		public void SetupScreenSize()
		{
			SysBufferWidth = 800;
			SysBufferHeight = 600;
			BufferHeight = SysBufferHeight;
			BufferWidth = SysBufferWidth;
			VirtualHeight = BufferHeight;
			VirtualWidth = BufferWidth;
			ScreenBuffer = new int[BufferWidth * BufferHeight];
			croppedBuffer = ScreenBuffer;

			switch (borderType)
			{
				case AmstradCPC.BorderType.Uncropped:
					break;

				case AmstradCPC.BorderType.Uniform:
					BufferWidth = 800;
					BufferHeight = 600;
					VirtualHeight = BufferHeight;
					VirtualWidth = BufferWidth;
					croppedBuffer = new int[BufferWidth * BufferHeight];
					break;

				case AmstradCPC.BorderType.Widescreen:
					break;
			}
		}

		protected int[] croppedBuffer;

		private AmstradCPC.BorderType _borderType;

		public AmstradCPC.BorderType borderType
		{
			get => _borderType;
			set => _borderType = value;
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
			byte portUpper = (byte)(port >> 8);
			byte portLower = (byte)(port & 0xff);

			// The gate array is selected when bit 15 of the I/O port address is set to "0" and bit 14 of the I/O port address is set to "1"
			bool accessed = false;
			if (!portUpper.Bit(7) && portUpper.Bit(6))
				accessed = true;

			if (!accessed)
				return accessed;

			// Bit 9 and 8 of the data byte define the function to access
			if (!dataBits[6] && !dataBits[7])
			{
				// select pen
				PENR = (byte)result;
			}

			if (dataBits[6] && !dataBits[7])
			{
				// select colour for selected pen
				INKR = (byte)result;
			}

			if (!dataBits[6] && dataBits[7])
			{
				// select screen mode, ROM configuration and interrupt control
				RMR = (byte)result;
			}

			if (dataBits[6] && dataBits[7])
			{
				// RAM memory management
				RAMR = (byte)result;
			}

			return true;
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("GateArray");
			ser.SyncEnum(nameof(ChipType), ref ChipType);
			ser.Sync(nameof(_PENR), ref _PENR);
			ser.Sync(nameof(_INKR), ref _INKR);
			ser.Sync(nameof(_RMR), ref _RMR);
			ser.Sync(nameof(_RAMR), ref _RAMR);
			ser.Sync(nameof(ColourRegisters), ref ColourRegisters, false);
			ser.Sync(nameof(CurrentPen), ref CurrentPen);
			ser.Sync(nameof(ClockCounter), ref ClockCounter);
			ser.Sync(nameof(FrameClock), ref FrameClock);
			ser.Sync(nameof(FrameEnd), ref FrameEnd);
			ser.Sync(nameof(WaitLine), ref WaitLine);
			ser.Sync(nameof(_interruptCounter), ref _interruptCounter);
			ser.Sync(nameof(VSYNCDelay), ref VSYNCDelay);
			ser.Sync(nameof(ScreenMode), ref ScreenMode);
			ser.Sync(nameof(HSYNC), ref HSYNC);
			//ser.Sync(nameof(HSYNC_falling), ref HSYNC_falling);
			ser.Sync(nameof(HSYNC_counter), ref HSYNC_counter);
			ser.Sync(nameof(VSYNC), ref VSYNC);
			ser.Sync(nameof(InterruptRaised), ref InterruptRaised);
			ser.Sync(nameof(InterruptHoldCounter), ref InterruptHoldCounter);
			ser.Sync(nameof(_MA), ref _MA);
			ser.Sync(nameof(IsNewFrame), ref IsNewFrame);
			ser.Sync(nameof(IsNewLine), ref IsNewLine);
			ser.Sync(nameof(HCC), ref HCC);
			ser.Sync(nameof(VLC), ref VLC);
			ser.Sync(nameof(VideoByte1), ref VideoByte1);
			ser.Sync(nameof(VideoByte2), ref VideoByte2);
			ser.Sync(nameof(NextVidRamLine), ref NextVidRamLine, false);
			ser.EndSection();
		}

		/// <summary>
		/// Represents a single scanline (in characters)
		/// </summary>
		public class CharacterLine
		{
			/// <summary>
			/// Screenmode is defined at each HSYNC (start of a new character line)
			/// Therefore we pass the mode in via constructor
			/// </summary>
			//public CharacterLine(int screenMode)
			//{
			//ScreenMode = screenMode;
			//}

			public int ScreenMode = 1;
			public List<Phase> Phases = new List<Phase>();
			public List<int> Pixels = new List<int>();

			/// <summary>
			/// Adds a new horizontal character to the list
			/// </summary>
			public void AddCharacter(Phase phase)
			{
				Phases.Add(phase);
			}

			public int PhaseCount => Phases.Count;

			public void Clear(int screenMode)
			{
				ScreenMode = screenMode;
				Phases.Clear();
				Pixels.Clear();
			}
		}

		[Flags]
		public enum Phase : int
		{
			/// <summary>
			/// Nothing
			/// </summary>
			NONE = 0,
			/// <summary>
			/// Border is being rendered
			/// </summary>
			BORDER = 1,
			/// <summary>
			/// Display rendered from video RAM
			/// </summary>
			DISPLAY = 2,
			/// <summary>
			/// HSYNC in progress
			/// </summary>
			HSYNC = 3,
			/// <summary>
			/// VSYNC in process
			/// </summary>
			VSYNC = 4,
			/// <summary>
			/// HSYNC occurs within a VSYNC
			/// </summary>
			HSYNCandVSYNC = 5
		}

		public enum GateArrayType
		{
			/// <summary>
			/// CPC 464
			/// The first version of the Gate Array is the 40007 and was released with the CPC 464
			/// </summary>
			Amstrad40007,
			/// <summary>
			/// CPC 664
			/// Later, the CPC 664 came out fitted with the 40008 version (and at the same time, the CPC 464 was also upgraded with this version). 
			/// This version is pinout incompatible with the 40007 (that's why the upgraded 464 of this period have two Gate Array slots on the motherboard, 
			/// one for a 40007 and one for a 40008)
			/// </summary>
			Amstrad40008,
			/// <summary>
			/// CPC 6128
			/// The CPC 6128 was released with the 40010 version (and the CPC 464 and 664 manufactured at that time were also upgraded to this version). 
			/// The 40010 is pinout compatible with the previous 40008
			/// </summary>
			Amstrad40010,
			/// <summary>
			/// Costdown CPC
			/// In the last serie of CPC 464 and 6128 produced by Amstrad in 1988, a small ASIC chip have been used to reduce the manufacturing costs. 
			/// This ASIC emulates the Gate Array, the PAL and the CRTC 6845. And no, there is no extra features like on the Amstrad Plus. 
			/// The only noticeable difference seems to be about the RGB output levels which are not exactly the same than those produced with a real Gate Array
			/// </summary>
			Amstrad40226,
			/// <summary>
			/// Plus &amp; GX-4000
			/// All the Plus range is built upon a bigger ASIC chip which is integrating many features of the classic CPC (FDC, CRTC, PPI, Gate Array/PAL) and all 
			/// the new Plus specific features. The Gate Array on the Plus have a new register, named RMR2, to expand the ROM mapping functionnalities of the machine. 
			/// This register requires to be unlocked first to be available. And finally, the RGB levels produced by the ASIC on the Plus are noticeably differents
			/// </summary>
			Amstrad40489,
		}
	}
}

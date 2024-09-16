﻿using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System.Collections;
using System.ComponentModel.Design;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CATHODE RAY TUBE CONTROLLER (CRTC) IMPLEMENTATION
	/// http://www.cpcwiki.eu/index.php/CRTC
	/// http://cpctech.cpc-live.com/docs/cpcplus.html
	/// This implementation aims to emulate all the various CRTC chips that appear within
	/// the CPC, CPC+ and GX4000 ranges. The CPC community have assigned them type numbers.
	/// If different implementations share the same type number it indicates that they are functionally identical:
	/// 
	/// Part No.      Manufacturer    Type No.    Info.
	/// ------------------------------------------------------------------------------------------------------
	/// HD6845S       Hitachi         0
	/// Datasheet:    http://www.cpcwiki.eu/imgs/c/c0/Hd6845.hitachi.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// UM6845        UMC             0
	/// Datasheet:    http://www.cpcwiki.eu/imgs/1/13/Um6845.umc.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// UM6845R       UMC             1
	/// Datasheet:    http://www.cpcwiki.eu/imgs/b/b5/Um6845r.umc.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// MC6845        Motorola        2
	/// Datasheet:    http://www.cpcwiki.eu/imgs/d/da/Mc6845.motorola.pdf &amp; http://bitsavers.trailing-edge.com/components/motorola/_dataSheets/6845.pdf
	/// ------------------------------------------------------------------------------------------------------
	/// AMS40489      Amstrad         3           Only exists in the CPC464+, CPC6128+ and GX4000 and is integrated into a single CPC+ ASIC chip (along with the gatearray)
	/// Datasheet:    {none}
	/// ------------------------------------------------------------------------------------------------------
	/// AMS40041      Amstrad         4           'Pre-ASIC' IC. The CRTC is integrated into a aingle ASIC IC with functionality being almost identical to the AMS40489
	/// (or 40226)                                Used in the 'Cost-Down' range of CPC464 and CPC6128 systems
	/// Datasheet:    {none}
	///
	/// </summary>
	public class CRTC : IPortIODevice
	{
		/// <summary>
		/// Type number as assigned above
		/// </summary>
		private int _crtcType;

		/// <summary>
		/// CPC register default values
		/// </summary>
		private readonly byte[] RegDefaults = { 63, 40, 46, 142, 38, 0, 25, 30, 0, 7, 0, 0, 48, 0, 192, 7, 0, 0 };


		/// <summary>
		/// The ClK isaTTUMOS-compatible input used to synchronize all CRT' functions except for the processor interface. 
		/// An external dot counter is used to derive this signal which is usually the character rate in an alphanumeric CRT.
		/// The active transition is high-to-low
		/// </summary>
		public bool CLK;

		/// <summary>
		/// This TTL compatible  output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
		/// This signal determines the horizontal position of the displayed text. 
		/// </summary>
		public bool HSYNC
		{
			get => _HSYNC;
			private set
			{
				if (value != _HSYNC)
				{
					// value has changed
					if (value) { HSYNC_On_Callbacks(); }
					else { HSYNC_Off_Callbacks(); }
				}
				_HSYNC = value;
			}
		}
		private bool _HSYNC;

		/// <summary>
		/// This TTL compatible output is an active high signal which drives the monitor directly or is fed to Video Processing Logic for composite generation.
		/// This signal determines the vertical position of the displayed text.
		/// </summary>
		public bool VSYNC
		{
			get => _VSYNC;
			private set
			{
				if (value != _VSYNC)
				{
					// value has changed
					if (value) { VSYNC_On_Callbacks(); }
					else { VSYNC_Off_Callbacks(); }
				}
				_VSYNC = value;
			}
		}
		private bool _VSYNC;

		public int FIELD => ~field & R8_Interlace & 0x01;

		/// <summary>
		/// This TTL compatible output is an active high signal which indicates the CRTC is providing addressing in the active Display Area.
		/// </summary>      
		public bool DISPTMG
		{
			get => _DISPTMG;
			private set => _DISPTMG = value;
		}
		private bool _DISPTMG;

		/// <summary>
		/// This TTL compatible output indicates Cursor Display to external Video Processing Logic.Active high signal. 
		/// </summary>       
		public bool CUDISP
		{
			get => _CUDISP;
			private set => _CUDISP = value;
		}
		private bool _CUDISP;

		/// <summary>
		/// Linear Address Generator
		/// Character pos address (0 index).
		/// Feeds the MA lines
		/// </summary>
		private int _LA;

		/// <summary>
		/// Generated by the Vertical Control Raster Counter
		/// Feeds the RA lines
		/// </summary>
		private int _RA;

		/// <summary>
		/// This 16-bit property emulates how the Amstrad CPC Gate Array is wired up to the CRTC
		/// Built from LA, RA and CLK
		/// 
		/// Memory Address Signal    Signal source    Signal name
		/// A15                      6845             MA13
		/// A14                      6845             MA12
		/// A13                      6845             RA2
		/// A12                      6845             RA1
		/// A11                      6845             RA0
		/// A10                      6845             MA9
		/// A9                       6845             MA8
		/// A8                       6845             MA7
		/// A7                       6845             MA6
		/// A6                       6845             MA5
		/// A5                       6845             MA4
		/// A4                       6845             MA3
		/// A3                       6845             MA2
		/// A2                       6845             MA1
		/// A1                       6845             MA0
		/// A0                       Gate-Array       CLK
		/// </summary>		
		public ushort MA_Address
		{
			get
			{
				BitArray MA = new BitArray(16);
				MA[0] = CLK;
				MA[1] = _LA.Bit(0);
				MA[2] = _LA.Bit(1);
				MA[3] = _LA.Bit(2);
				MA[4] = _LA.Bit(3);
				MA[5] = _LA.Bit(4);
				MA[6] = _LA.Bit(5);
				MA[7] = _LA.Bit(6);
				MA[8] = _LA.Bit(7);
				MA[9] = _LA.Bit(8);
				MA[10] = _LA.Bit(9);
				MA[11] = _RA.Bit(0);
				MA[12] = _RA.Bit(1);
				MA[13] = _RA.Bit(2);
				MA[14] = _LA.Bit(12);
				MA[15] = _LA.Bit(13);
				int[] array = new int[1];
				MA.CopyTo(array, 0);
				return (ushort)array[0];
			}
		}

		/// <summary>
		/// Public Delegate
		/// </summary>
		public delegate void CallBack();
		/// <summary>
		/// Fired on CRTC HSYNC signal rising edge
		/// </summary>
		private CallBack HSYNC_On_Callbacks;
		/// <summary>
		/// Fired on CRTC HSYNC signal falling edge
		/// </summary>
		private CallBack HSYNC_Off_Callbacks;
		/// <summary>
		/// Fired on CRTC VSYNC signal rising edge
		/// </summary>
		private CallBack VSYNC_On_Callbacks;
		/// <summary>
		/// Fired on CRTC VSYNC signal falling edge
		/// </summary>
		private CallBack VSYNC_Off_Callbacks;

		public void AttachHSYNCOnCallback(CallBack hCall)
		{
			HSYNC_On_Callbacks += hCall;
		}
		public void AttachHSYNCOffCallback(CallBack hCall)
		{
			HSYNC_Off_Callbacks += hCall;
		}
		public void AttachVSYNCOnCallback(CallBack vCall)
		{
			VSYNC_On_Callbacks += vCall;
		}
		public void AttachVSYNCOffCallback(CallBack vCall)
		{
			VSYNC_Off_Callbacks += vCall;
		}

		/// <summary>
		/// Reset Counter
		/// </summary>
		private int _inReset;

		/// <summary>
		/// This is a 5 bit register which is used as a pointer to direct data transfers to and from the system MPU
		/// </summary>
		private byte AddressRegister
		{
			get => (byte)(_addressRegister & 0x1F);
			set => _addressRegister = value;
		}
		private byte _addressRegister;

		/// <summary>
		/// This 8 bit write-only register determines the horizontal frequency of HS. 
		/// It is the total of displayed plus non-displayed character time units minus one.
		/// </summary>
		private const int R0_H_TOTAL = 0;
		/// <summary>
		/// This 8 bit write-only register determines the number of displayed characters per horizontal line.
		/// </summary>
		private const int R1_H_DISPLAYED = 1;
		/// <summary>
		/// This 8 bit write-only register determines the horizontal sync postiion on the horizontal line.
		/// </summary>
		private const int R2_H_SYNC_POS = 2;
		/// <summary>
		/// This 4 bit  write-only register determines the width of the HS pulse. It may not be apparent why this width needs to be programmed.However, 
		/// consider that all timing widths must be programmed as multiples of the character clock period which varies.If HS width were fixed as an integral 
		/// number of character times, it would vary with character rate and be out of tolerance for certain monitors.
		/// The rate programmable feature allows compensating HS width.
		/// NOTE: Dependent on chiptype this also may include VSYNC width - check the UpdateWidths() method
		/// </summary>
		private const int R3_SYNC_WIDTHS = 3;

		/* Vertical Timing Register Constants */
		/// <summary>
		/// The vertical frequency of VS is determined by both R4 and R5.The calculated number of character I ine times is usual I y an integer plus a fraction to 
		/// get exactly a 50 or 60Hz vertical refresh rate. The integer number of character line times minus one is programmed in the 7 bit write-only Vertical Total Register; 
		/// the fraction is programmed in the 5 bit write-only Vertical Scan Adjust Register as a number of scan line times.
		/// </summary>
		private const int R4_V_TOTAL = 4;
		private const int R5_V_TOTAL_ADJUST = 5;
		/// <summary>
		/// This 7 bit write-only register determines the number of displayed character rows on the CRT screen, and is programmed in character row times.
		/// </summary>
		private const int R6_V_DISPLAYED = 6;
		/// <summary>
		/// This 7 bit write-only register determines the vertical sync position with respect to the reference.It is programmed in character row times.
		/// </summary>
		private const int R7_V_SYNC_POS = 7;
		/// <summary>
		/// This 2 bit write-only  register controls the raster scan mode(see Figure 11 ). When bit 0 and bit 1 are reset, or bit 0 is reset and bit 1 set, 
		/// the non· interlace raster scan mode is selected.Two interlace modes are available.Both are interlaced 2 fields per frame.When bit 0 is set and bit 1 is reset, 
		/// the interlace sync raster scan mode is selected.Also when bit 0 and bit 1 are set, the interlace sync and video raster scan mode is selected.
		/// </summary>
		private const int R8_INTERLACE_MODE = 8;
		/// <summary>
		/// This 5 bit write·only register determines the number of scan lines per character row including spacing.
		/// The programmed value is a max address and is one less than the number of scan l1nes.
		/// </summary>
		private const int R9_MAX_SL_ADDRESS = 9;
		/// <summary>
		/// This 7 bit write-only register controls the cursor format(see Figure 10). Bit 5 is the blink timing control.When bit 5 is low, the blink frequency is 1/16 of the 
		/// vertical field rate, and when bit 5 is high, the blink frequency is 1/32 of the vertical field rate.Bit 6 is used to enable a blink.
		/// The cursor start scan line is set by the lower 5 bits. 
		/// </summary>
		private const int R10_CURSOR_START = 10;
		/// <summary>
		/// This 5 bit write-only register sets the cursor end scan line
		/// </summary>
		private const int R11_CURSOR_END = 11;
		/// <summary>
		/// Start Address Register is a 14 bit write-only register which determines the first address put out as a refresh address after vertical blanking.
		/// It consists of an 8 bit lower register, and a 6 bit higher register.
		/// </summary>
		private const int R12_START_ADDR_H = 12;
		private const int R13_START_ADDR_L = 13;
		/// <summary>
		/// This 14 bit read/write register stores the cursor location.This register consists of an 8 bit lower and 6 bit higher register.
		/// </summary>
		private const int R14_CURSOR_H = 14;
		private const int R15_CURSOR_L = 15;
		/// <summary>
		/// This 14 bit read -only register is used to store the contents of the Address Register(H &amp; L) when the LPSTB input pulses high.
		/// This register consists of an 8 bit lower and 6 bit higher register.
		/// </summary>
		private const int R16_LIGHT_PEN_H = 16;
		private const int R17_LIGHT_PEN_L = 17;

		/// <summary>
		/// Storage for main MPU registers 
		/// 
		/// RegIdx    Register Name                 Type
		///                                         0             1             2             3                      4
		/// 0         Horizontal Total              Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 1         Horizontal Displayed          Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 2         Horizontal Sync Position      Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 3         H and V Sync Widths           Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 4         Vertical Total                Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 5         Vertical Total Adjust         Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 6         Vertical Displayed            Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 7         Vertical Sync position        Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 8         Interlace and Skew            Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 9         Maximum Raster Address        Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 10        Cursor Start Raster           Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 11        Cursor End Raster             Write Only    Write Only    Write Only    (note 2)               (note 3)
		/// 12        Disp. Start Address (High)    Read/Write    Write Only    Write Only    Read/Write (note 2)    (note 3)
		/// 13        Disp. Start Address (Low)     Read/Write    Write Only    Write Only    Read/Write (note 2)    (note 3)
		/// 14        Cursor Address (High)         Read/Write    Read/Write    Read/Write    Read/Write (note 2)    (note 3)
		/// 15        Cursor Address (Low)          Read/Write    Read/Write    Read/Write    Read/Write (note 2)    (note 3)
		/// 16        Light Pen Address (High)      Read Only     Read Only     Read Only     Read Only (note 2)     (note 3)
		/// 
		/// 18-31	  Not Used
		/// 
		/// 1. On type 0 and 1, if a Write Only register is read from, "0" is returned.
		/// 2. See the document "Extra CPC Plus Hardware Information" for more details.
		/// 3. CRTC type 4 is the same as CRTC type 3. The registers also repeat as they do on the type 3.
		/// </summary>
		private byte[] Register = new byte[32];

		/// <summary>
		/// Internal Status Register specific to the Type 1 UM6845R
		/// </summary>
		private byte StatusRegister;

		/// <summary>
		/// CRTC-type horizontal total independent helper function
		/// </summary>
		private int R0_HorizontalTotal
		{
			get
			{
				int ht = Register[R0_H_TOTAL];
				return ht;
			}
		}

		/// <summary>
		/// CRTC-type horizontal displayed independent helper function
		/// </summary>
		private int R1_HorizontalDisplayed
		{
			get
			{
				int hd = Register[R1_H_DISPLAYED];
				return hd;
			}
		}

		/// <summary>
		/// CRTC-type horizontal sync position independent helper function
		/// </summary>
		private int R2_HorizontalSyncPosition
		{
			get
			{
				int hsp = Register[R2_H_SYNC_POS];
				return hsp;
			}
		}

		/// <summary>
		/// CRTC-type horizontal sync width independent helper function 
		/// </summary>
		private int R3_HorizontalSyncWidth
		{
			get
			{
				int swr;

				// Bits 3..0 define Horizontal Sync Width
				var sw = Register[R3_SYNC_WIDTHS] & 0x0F;

				switch (_crtcType)
				{
					case 0:
					case 1:
						// If 0 is programmed no HSYNC is generated
						swr = sw;
						break;
					case 2:
					case 3:
					case 4:
					default:
						// If 0 is programmed this gives a HSYNC width of 16
						swr = sw > 0 ? sw : 16;
						break;
				}

				return swr;
			}
		}

		/// <summary>
		/// CRTC-type vertical sync width independent helper function 
		/// </summary>
		private int R3_VerticalSyncWidth
		{
			get
			{
				int swr;

				//Bits 7..4 define Vertical Sync Width
				var sw = (Register[R3_SYNC_WIDTHS] >> 4) & 0x0F;

				switch (_crtcType)
				{
					case 0:
					case 3:
					case 4:
					default:
						// If 0 is programmed this gives 16 lines of VSYNC
						swr = sw > 0 ? sw : 16;
						break;
					case 1:
					case 2:
						// Vertical Sync is fixed at 16 lines
						swr = 16;
						break;
				}

				return swr;
			}
		}

		/// <summary>
		/// CRTC-type vertical total independent helper function
		/// </summary>
		private int R4_VerticalTotal
		{
			get
			{
				int vt = Register[R4_V_TOTAL];
				return vt;
			}
		}

		/// <summary>
		/// CRTC-type vertical total adjust independent helper function
		/// </summary>
		private int R5_VerticalTotalAdjust
		{
			get
			{
				int vta = Register[R5_V_TOTAL_ADJUST];
				return vta;
			}
		}

		/// <summary>
		/// CRTC-type vertical displayed independent helper function
		/// </summary>
		private int R6_VerticalDisplayed
		{
			get
			{
				int vd = Register[R6_V_DISPLAYED];
				return vd;
			}
		}

		/// <summary>
		/// CRTC-type vertical sync position independent helper function
		/// </summary>
		private int R7_VerticalSyncPosition
		{
			get
			{
				int vsp = Register[R7_V_SYNC_POS];
				return vsp;
			}
		}

		/// <summary>
		/// CRTC-type DISPTMG Active Display Skew helper function
		/// </summary>
		private int R8_Skew
		{
			get
			{
				int skew = 0;
				switch (_crtcType)
				{
					case 0:
						// For Hitachi HD6845:
						// 0 = no skew
						// 1 = one-character skew
						// 2 = two-character skew
						// 3 = non-output
						skew = (Register[R8_INTERLACE_MODE] >> 4) & 0x03;
						break;
					case 1:
						// skew not implemented
						break;
					case 2:
						// skew not implemented
						break;
					default:
						return skew;
				}
				return skew;
			}
		}

		/// <summary>
		/// CRTC-type Interlace Mode helper function
		/// </summary>
		private int R8_Interlace
		{
			get
			{
				int interlace = 0;
				switch (_crtcType)
				{
					
					case 0:
					case 1:
					case 2:
						// 0 = Non-interlace
						// 1 = Interlace SYNC Raster Scan
						// 2 = Interlace SYNC and Video Raster Scan
						interlace = Register[R8_INTERLACE_MODE] & 0x03;
						if (!interlace.Bit(0))
						{
							interlace = 0;
						}
						break;
					default:
						break;
				}
				return interlace;
			}
		}

		/// <summary>
		/// Max Scanlines
		/// </summary>
		private int R9_MaxScanline
		{
			get
			{
				int max = Register[R9_MAX_SL_ADDRESS];
				return max;
			}
		}

		/// <summary>
		/// Horizontal Character Counter
		/// 8-bit
		/// </summary>		
		private int HCC
		{
			get => _hcCTR & 0xFF;
			set => _hcCTR = value & 0xFF;
		}
		private int _hcCTR;

		/// <summary>
		/// Horizontal Sync Width Counter (HSYNC)
		/// 4-bit
		/// </summary>		
		private int HSC
		{
			get => _hswCTR & 0x0F;
			set => _hswCTR = value & 0x0F;
		}
		private int _hswCTR;

		/// <summary>
		/// Vertical Character Row Counter
		/// 7-bit
		/// </summary>
		private int VCC
		{
			get => _rowCTR & 0x7F;
			set => _rowCTR = value & 0x7F;
		}
		private int _rowCTR;

		/// <summary>
		/// Vertical Sync Width Counter (VSYNC)
		/// 4-bit
		/// </summary>
		private int VSC
		{
			get => _vswCTR & 0x0F;
			set => _vswCTR = value & 0x0F;
		}
		private int _vswCTR;

		/// <summary>
		/// Vertical Line Counter (Scanline Counter)
		/// 5-bit
		/// If not in IVM mode, this counter is exposed on CRTC pins RA0..RA4
		/// </summary>
		private int VLC
		{
			get => _lineCTR & 0x1F;
			set => _lineCTR = value & 0x1F;
		}
		private int _lineCTR;

		/// <summary>
		/// Vertical Total Adjust Counter
		/// 5-bit??
		/// This counter does not exist on CRTCs 0/3/4. C9 (VLC) is reused instead
		/// </summary>
		private int VTAC
		{
			get
			{
				switch (_crtcType)
				{
					case 0:
					case 3:
					case 4:
						return VLC;
					default:
						return _vtac & 0x1F;
				}
			}
			set
			{
				switch (_crtcType)
				{
					case 0:
					case 3:
					case 4:
						VLC = value;
						break;
					default:
						_vtac = value & 0x1F;
						break;
				}
			}
		}
		private int _vtac;


		/// <summary>
		/// Constructor
		/// </summary>
		public CRTC(int crtcType)
		{
			_crtcType = crtcType;
			Reset();
		}

		// persistent control signals
		private bool latch_hdisp;
		private bool latch_vdisp;
		private bool latch_idisp;

		private bool latch_hsync;
		private bool latch_vadjust;
		private bool latch_skew;

		private bool hclock;
		private bool hhclock;
		private bool hend;
		private bool hsend;

		private int r_addr;
		private bool adjusting;
		private int field;
		

		/// <summary>
		/// CRTC is clocked by the gatearray at 1MHz (every 16 GA clocks / pixel clocks)
		/// </summary>
		public void Clock()
		{
			if (_inReset > 0)
			{
				// reset takes a whole CRTC clock cycle
				_inReset--;

				HCC = 0;
				HSC = 0;
				VCC = 0;
				VSC = 0;
				VLC = 0;
				VTAC = 0;

				// set regs to default
				for (int i = 0; i < 18; i++)
					Register[i] = RegDefaults[i];

				return;
			}
			else
				_inReset = -1;

			/**********************************/
			/* CLK - Linear Address Generator */
			/**********************************/

			// running the LAG before the other stuff so that initial addressing is correct
			if (VCC == 0)
			{
				r_addr = (Register[R12_START_ADDR_H] << 8) | Register[R13_START_ADDR_L];
			}

			_LA = r_addr + HCC;
			_RA = VLC;

			/*****************************/
			/* CLK - Horizontal Counters */
			/*****************************/

			if (HCC == R0_HorizontalTotal)
			{
				// H-Clock is generated
				hclock = true;
				// H-Display is active
				latch_hdisp = true;
			}

			if (HCC == (R0_HorizontalTotal / 2))
			{
				// HH-Clock is generated
				hhclock = true;
			}

			if (HCC == R1_HorizontalDisplayed - 1)
			{
				// H-Display is made inactive
				latch_hdisp = false;
				// HEND is generated
				hend = true;
			}

			if (HCC == R2_HorizontalSyncPosition - 1)
			{
				// HSYNC is generated				
				HSYNC = true;
			}

			// clock the horiz char counter
			HCC++;

			if (HSYNC)
			{
				// HSYNC also triggers CE on the horizontal sync width counter which means it can start counting with every CLK
				if (HSC == R3_HorizontalSyncWidth - 1)
				{
					// HSC is reset
					HSC = 0;
					// end of HSYNC - start of displayable area again
					HSYNC = false;
					// vertical control is clocked
					hsend = true;
				}
				else
				{
					// clock the horizontal sync width counter
					HSC++;
				}
			}

			/******************************/
			/* H-CLOCK - Vertical Control */
			/******************************/

			if (hclock)
			{
				// hclock is a single clock, not latched
				hclock = false;				

				// hclock clocks the scanline counter
				if (VLC == R9_MaxScanline)
				{
					// linear address generator is clocked
					//todo

					// character row counter is clocked
					if (VCC == R6_VerticalDisplayed - 1)
					{
						// start of border
						latch_vdisp = false;
					}

					if (VCC == R7_VerticalSyncPosition - 1)
					{
						// start of VSYNC
						VSYNC = true;
					}

					if (VCC == R4_VerticalTotal - 1)
					{
						// start of addressable display
						latch_vdisp = true;

						// vertical control is clocked
						//todo

						// vertical character counter is reset
						VCC = 0;

						// VSYNC disabled
						VSYNC = false;
					}
					else
					{
						VCC++;
					}					

					// scanline counter reset (via an OR gate with the LAG)
					VLC = 0;
				}
				else
				{
					// clock the vertical scanline counter
					VLC++;
				}

				if (hhclock)
				{
					// need to work out what to do here
					hhclock = false;
				}

				HCC = 0;
			}


			// DISPTMG Generation
			if (latch_hdisp || latch_vdisp)
			{
				// HSYNC output pin is fed through a NOR gate with either 2 or 3 inputs
				// - H Display
				// - V Display
				// - R8 DISPTMG Skew (only on certain CRTC types)
				DISPTMG = true;
			}
			else
			{
				DISPTMG = false;
			}
		}


		private void HClock()
		{

		}

		/// <summary>
		/// Selects a specific register
		/// </summary>
		private void SelectRegister(int value)
		{
			var v = (byte)(value & 0x1F);
			AddressRegister = v;
		}

		/// <summary>
		/// Attempts to read from the currently selected register
		/// </summary>
		private bool ReadRegister(ref int data)
		{
			switch (_crtcType)
			{
				case 0:
					switch (AddressRegister)
					{
						case R0_H_TOTAL:
						case R1_H_DISPLAYED:
						case R2_H_SYNC_POS:
						case R3_SYNC_WIDTHS:
						case R4_V_TOTAL:
						case R5_V_TOTAL_ADJUST:
						case R6_V_DISPLAYED:
						case R7_V_SYNC_POS:
						case R8_INTERLACE_MODE:
						case R9_MAX_SL_ADDRESS:
						case R10_CURSOR_START:
						case R11_CURSOR_END:
							// write-only registers return 0x0 on Type 0 CRTC
							data = 0;
							break;
						case R12_START_ADDR_H:
						case R14_CURSOR_H:
						case R16_LIGHT_PEN_H:
							// read/write registers (6bit)
							data = Register[AddressRegister] & 0x3F;
							break;
						case R13_START_ADDR_L:
						case R15_CURSOR_L:
						case R17_LIGHT_PEN_L:
							// read/write regiters (8bit)
							data = Register[AddressRegister];
							break;
						default:
							// non-existent registers return 0x0
							data = 0;
							break;
					}
					break;

				case 1:
					switch (AddressRegister)
					{
						case R0_H_TOTAL:
						case R1_H_DISPLAYED:
						case R2_H_SYNC_POS:
						case R3_SYNC_WIDTHS:
						case R4_V_TOTAL:
						case R5_V_TOTAL_ADJUST:
						case R6_V_DISPLAYED:
						case R7_V_SYNC_POS:
						case R8_INTERLACE_MODE:
						case R9_MAX_SL_ADDRESS:
						case R10_CURSOR_START:
						case R11_CURSOR_END:
						case R12_START_ADDR_H:
						case R13_START_ADDR_L:
							// write-only registers return 0x0 on Type 1 CRTC
							data = 0;
							break;
						case R14_CURSOR_H:
							data = Register[AddressRegister] & 0x3F;
							break;
						case R15_CURSOR_L:
							data = Register[AddressRegister];
							break;
						case R16_LIGHT_PEN_H:
							// read/write registers (6bit)
							data = Register[AddressRegister] & 0x3F;
							// reading from R16 resets bit6 of the status register
							StatusRegister &= byte.MaxValue ^ (1 << 6);
							break;
						case R17_LIGHT_PEN_L:
							// read/write regiters (8bit)
							data = Register[AddressRegister];
							// reading from R17 resets bit6 of the status register
							StatusRegister &= byte.MaxValue ^ (1 << 6);
							break;
						case 31:
							// Dummy Register. Datasheet describes this as N/A but CPCWIKI suggests that reading from it return 0xFF;
							data = 0xFF;
							break;
						default:
							// non-existent registers return 0x0
							data = 0;
							break;
					}
					break;

				case 2:
					switch (AddressRegister)
					{
						case R0_H_TOTAL:
						case R1_H_DISPLAYED:
						case R2_H_SYNC_POS:
						case R3_SYNC_WIDTHS:
						case R4_V_TOTAL:
						case R5_V_TOTAL_ADJUST:
						case R6_V_DISPLAYED:
						case R7_V_SYNC_POS:
						case R8_INTERLACE_MODE:
						case R9_MAX_SL_ADDRESS:
						case R10_CURSOR_START:
						case R11_CURSOR_END:
						case R12_START_ADDR_H:
						case R13_START_ADDR_L:
							// write-only registers do not respond on type 2
							return false;
						case R14_CURSOR_H:
						case R16_LIGHT_PEN_H:
							// read/write registers (6bit)
							data = Register[AddressRegister] & 0x3F;
							break;
						case R17_LIGHT_PEN_L:
						case R15_CURSOR_L:
							// read/write regiters (8bit)
							data = Register[AddressRegister];
							break;
						default:
							// non-existent registers return 0x0
							data = 0;
							break;
					}
					break;

				case 3:
				case 4:
					// http://cpctech.cpc-live.com/docs/cpcplus.html
					switch (AddressRegister & 0x6F)
					{
						case 0:
							data = Register[R16_LIGHT_PEN_H] & 0x3F;
							break;
						case 1:
							data = Register[R17_LIGHT_PEN_L];
							break;
						case 2:
							// Status 1
							break;
						case 3:
							// Status 2
							break;
						case 4:
							data = Register[R12_START_ADDR_H] & 0x3F;
							break;
						case 5:
							data = Register[R13_START_ADDR_L];
							break;
						case 6:
						case 7:
							data = 0;
							break;
					}
					break;
			}

			return true;
		}

		/// <summary>
		/// Attempts to write to the currently selected register
		/// </summary>
		private void WriteRegister(int data)
		{
			byte v = (byte)data;

			switch (_crtcType)
			{
				case 0:
					switch (AddressRegister)
					{
						case R0_H_TOTAL:
						case R1_H_DISPLAYED:
						case R2_H_SYNC_POS:
						case R3_SYNC_WIDTHS:
						case R13_START_ADDR_L:
						case R15_CURSOR_L:
							// 8-bit registers
							Register[AddressRegister] = v;
							break;
						case R4_V_TOTAL:
						case R6_V_DISPLAYED:
						case R7_V_SYNC_POS:
						case R10_CURSOR_START:
							// 7-bit registers
							Register[AddressRegister] = (byte)(v & 0x7F);
							break;
						case R12_START_ADDR_H:
						case R14_CURSOR_H:
							// 6-bit registers
							Register[AddressRegister] = (byte)(v & 0x3F);
							break;
						case R5_V_TOTAL_ADJUST:
						case R9_MAX_SL_ADDRESS:
						case R11_CURSOR_END:
							// 5-bit registers
							Register[AddressRegister] = (byte)(v & 0x1F);
							break;
						case R8_INTERLACE_MODE:
							// Interlace & skew masks bits 2 & 3
							Register[AddressRegister] = (byte)(v & 0xF3);
							break;
					}
					break;

				case 1:
					switch (AddressRegister)
					{
						case R0_H_TOTAL:
						case R1_H_DISPLAYED:
						case R2_H_SYNC_POS:
						case R13_START_ADDR_L:
						case R15_CURSOR_L:
							// 8-bit registers
							Register[AddressRegister] = v;
							break;
						case R4_V_TOTAL:
						case R6_V_DISPLAYED:
						case R7_V_SYNC_POS:
						case R10_CURSOR_START:
							// 7-bit registers
							Register[AddressRegister] = (byte)(v & 0x7F);
							break;
						case R12_START_ADDR_H:
						case R14_CURSOR_H:
							// 6-bit registers
							Register[AddressRegister] = (byte)(v & 0x3F);
							break;
						case R5_V_TOTAL_ADJUST:
						case R9_MAX_SL_ADDRESS:
						case R11_CURSOR_END:
							// 5-bit registers
							Register[AddressRegister] = (byte)(v & 0x1F);
							break;
						case R3_SYNC_WIDTHS:
							// 4-bit register
							Register[AddressRegister] = (byte)(v & 0x0F);
							break;
						case R8_INTERLACE_MODE:
							// Interlace & skew - 2bit
							Register[AddressRegister] = (byte)(v & 0x03);
							break;
					}
					break;

				case 2:
					switch (AddressRegister)
					{
						case R0_H_TOTAL:
						case R1_H_DISPLAYED:
						case R2_H_SYNC_POS:
						case R13_START_ADDR_L:
						case R15_CURSOR_L:
							// 8-bit registers
							Register[AddressRegister] = v;
							break;
						case R4_V_TOTAL:
						case R6_V_DISPLAYED:
						case R7_V_SYNC_POS:
						case R10_CURSOR_START:
							// 7-bit registers
							Register[AddressRegister] = (byte)(v & 0x7F);
							break;
						case R12_START_ADDR_H:
						case R14_CURSOR_H:
							// 6-bit registers
							Register[AddressRegister] = (byte)(v & 0x3F);
							break;
						case R5_V_TOTAL_ADJUST:
						case R9_MAX_SL_ADDRESS:
						case R11_CURSOR_END:
							// 5-bit registers
							Register[AddressRegister] = (byte)(v & 0x1F);
							break;
						case R3_SYNC_WIDTHS:
							// 4-bit register
							Register[AddressRegister] = (byte)(v & 0x0F);
							break;
						case R8_INTERLACE_MODE:
							// Interlace & skew - 2bit
							Register[AddressRegister] = (byte)(v & 0x03);
							break;
					}
					break;

				case 3:
				case 4:
					byte v3 = (byte)data;
					switch (AddressRegister)
					{
						case 16:
						case 17:
							// read only registers
							return;
						default:
							if (AddressRegister < 16)
							{
								Register[AddressRegister] = v3;
							}
							else
							{
								// read only dummy registers
								return;
							}
							break;
					}
					break;
			}
		}

		/// <summary>
		/// Attempts to read from the internal status register (if present)
		/// </summary>
		private bool ReadStatus(ref int data)
		{
			//todo 
			return false;
		}

		/// <summary>
		/// Device responds to an IN instruction
		/// </summary>
		public bool ReadPort(ushort port, ref int result)
		{
			byte portUpper = (byte)(port >> 8);
			byte portLower = (byte)(port & 0xff);

			bool accessed = false;

			// The 6845 is selected when bit 14 of the I/O port address is set to "0"
			if (portUpper.Bit(6))
				return accessed;

			// Bit 9 and 8 of the I/O port address define the function to access
			if (portUpper.Bit(1) && !portUpper.Bit(0))
			{
				// read status register
				accessed = ReadStatus(ref result);
			}
			else if ((portUpper & 3) == 3)
			{
				// read data register
				accessed = ReadRegister(ref result);
			}
			else
			{
				result = 0;
			}

			return accessed;
		}

		/// <summary>
		/// Device responds to an OUT instruction
		/// </summary>
		public bool WritePort(ushort port, int result)
		{
			byte portUpper = (byte)(port >> 8);
			byte portLower = (byte)(port & 0xff);

			bool accessed = false;

			// The 6845 is selected when bit 14 of the I/O port address is set to "0"
			if (portUpper.Bit(6))
				return accessed;

			var func = portUpper & 3;

			switch (func)
			{
				// reg select
				case 0:
					SelectRegister(result);
					break;

				// data write
				case 1:
					WriteRegister(result);
					break;
			}

			return accessed;
		}

		/// <summary>
		/// Simulates the RESET pin
		/// This should take at least one cycle
		/// </summary>
		public void Reset()
		{
			_inReset = 1;			
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("CRTC");
			ser.Sync(nameof(_crtcType), ref _crtcType);
			ser.Sync(nameof(CLK), ref CLK);
			ser.Sync(nameof(_VSYNC), ref _VSYNC);
			ser.Sync(nameof(_HSYNC), ref _HSYNC);
			ser.Sync(nameof(_DISPTMG), ref _DISPTMG);
			ser.Sync(nameof(_CUDISP), ref _CUDISP);
			ser.Sync(nameof(_LA), ref _LA);
			ser.Sync(nameof(_RA), ref _RA);
			ser.Sync(nameof(_addressRegister), ref _addressRegister);
			ser.Sync(nameof(Register), ref Register, false);
			ser.Sync(nameof(StatusRegister), ref StatusRegister);
			ser.Sync(nameof(_hcCTR), ref _hcCTR);
			ser.Sync(nameof(_hswCTR), ref _hswCTR);
			ser.Sync(nameof(_vswCTR), ref _vswCTR);
			ser.Sync(nameof(_rowCTR), ref _rowCTR);
			ser.Sync(nameof(_lineCTR), ref _lineCTR);
			ser.Sync(nameof(latch_hdisp), ref latch_hdisp);
			ser.Sync(nameof(latch_vdisp), ref latch_vdisp);
			ser.Sync(nameof(latch_hsync), ref latch_hsync);
			ser.Sync(nameof(latch_vadjust), ref latch_vadjust);
			ser.Sync(nameof(latch_skew), ref latch_skew);
			ser.Sync(nameof(field), ref field);
			ser.Sync(nameof(adjusting), ref adjusting);
			ser.Sync(nameof(r_addr), ref r_addr);
			ser.Sync(nameof(_inReset), ref _inReset);
			ser.Sync(nameof(_vtac), ref _vtac);			
			ser.EndSection();
		}
	}
}

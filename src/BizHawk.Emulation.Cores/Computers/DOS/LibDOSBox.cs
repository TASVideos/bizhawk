using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public abstract class LibDOSBox : LibWaterboxCore
	{

		public const int PAL_WIDTH = 720;
		public const int NTSC_WIDTH = PAL_WIDTH;
		// the core renders 576 which is what libretro displays
		// but default window height is 568 in original PUAE and WinUAE
		// this lets us hide a black line and a weird artifact that our A600 config has there
		public const int PAL_HEIGHT = 574;
		// WinUAE displays 484 lines for NTSC
		// but libretro port only renders 482 and then only displays 480
		public const int NTSC_HEIGHT = 482;
		// libretro defines PUAE_VIDEO_HZ_PAL as 49.9204101562500000f
		public const int VIDEO_NUMERATOR_PAL = 102237;
		public const int VIDEO_DENOMINATOR_PAL = 2048;
		// libretro defines PUAE_VIDEO_HZ_NTSC as 59.8260993957519531f
		public const int VIDEO_NUMERATOR_NTSC = 299130497;
		public const int VIDEO_DENOMINATOR_NTSC = 5000000;

		public const int FASTMEM_AUTO = -1;
		public const int MAX_FLOPPIES = 4;
		public const int FILENAME_MAXLENGTH = 64;
		public const int KEY_COUNT = 0x68;


		public const byte MouseButtonsMask =
		(byte) (AllButtons.Button_1
		| AllButtons.Button_2
		| AllButtons.Button_3);
		public const byte JoystickMask =
			(byte) (AllButtons.Up
			| AllButtons.Down
			| AllButtons.Left
			| AllButtons.Right
			| AllButtons.Button_1
			| AllButtons.Button_2
			| AllButtons.Button_3);
		public const short Cd32padMask =
			(short) (AllButtons.Up
			| AllButtons.Down
			| AllButtons.Left
			| AllButtons.Right
			| AllButtons.Play
			| AllButtons.Rewind
			| AllButtons.Forward
			| AllButtons.Green
			| AllButtons.Yellow
			| AllButtons.Red

			| AllButtons.Blue);
		[BizImport(CC, Compatibility = true)]
		public abstract bool Init(int argc, string[] argv);

		[BizImport(CC)]
		public abstract void SetLEDCallback(EmptyCallback callback);

		[StructLayout(LayoutKind.Sequential)]
		public new class FrameInfo : LibWaterboxCore.FrameInfo
		{
			public ControllerState Port1;
			public ControllerState Port2;
			public KeyBuffer Keys;
			public struct KeyBuffer
			{
				public unsafe fixed byte Buffer[KEY_COUNT];
			}
			public int CurrentDrive;
			public DriveAction Action;
			public FileName Name;
			public struct FileName
			{
				public unsafe fixed byte Buffer[FILENAME_MAXLENGTH];
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct ControllerState
		{
			public ControllerType Type;
			public AllButtons Buttons;
			public int MouseX;
			public int MouseY;
		}

#if true
		public enum ControllerType : int
		{
			None = 0,
			[Display(Name = "Joystick")]
			DJoy = 1,
			Mouse = 2,
			[Display(Name = "CD32 pad")]
			CD32Joy = 3,
		}
#else //TODO matches upstream -- requires unmanaged change
		public enum ControllerType : int
		{
			None = 0,
			Mouse = 1,
//			MouseNoWheel,
			[Display(Name = "Joystick")]
			DJoy = 3,
//			Gamepad,
//			AJoy,
//			CDTVJoy,
			[Display(Name = "CD32 pad")]
			CD32Joy = 7,
//			Lightpen,
		}
#endif

		public enum DriveAction : int
		{
			None,
			EjectDisk,
			InsertDisk
		}

		[Flags]
		public enum AllButtons : short
		{
			Up       = 0b0000000000000001,
			Down     = 0b0000000000000010,
			Left     = 0b0000000000000100,
			Right    = 0b0000000000001000,
			Button_1 = 0b0000000000010000,
			Button_2 = 0b0000000000100000,
			Button_3 = 0b0000000001000000,
			Play     = 0b0000000010000000,
			Rewind   = 0b0000000100000000,
			Forward  = 0b0000001000000000,
			Green    = 0b0000010000000000,
			Yellow   = 0b0000100000000000,
			Red      = 0b0001000000000000,
			Blue     = 0b0010000000000000
		}

		// https://wiki.amigaos.net/wiki/Keymap_Library
		public enum UAEKeyboard : int
		{
			Key_Backquote      = 0x00,
			Key_1              = 0x01,
			Key_2              = 0x02,
			Key_3              = 0x03,
			Key_4              = 0x04,
			Key_5              = 0x05,
			Key_6              = 0x06,
			Key_7              = 0x07,
			Key_8              = 0x08,
			Key_9              = 0x09,
			Key_0              = 0x0A,
			Key_Minus          = 0x0B,
			Key_Equal          = 0x0C,
			Key_Backslash      = 0x0D,
//			Undefined          = 0x0E,
			Key_NP_0           = 0x0F,
			Key_Q              = 0x10,
			Key_W              = 0x11,
			Key_E              = 0x12,
			Key_R              = 0x13,
			Key_T              = 0x14,
			Key_Y              = 0x15,
			Key_U              = 0x16,
			Key_I              = 0x17,
			Key_O              = 0x18,
			Key_P              = 0x19,
			Key_Left_Bracket   = 0x1A,
			Key_Right_Bracket  = 0x1B,
//			Undefined          = 0x1C,
			Key_NP_1           = 0x1D,
			Key_NP_2           = 0x1E,
			Key_NP_3           = 0x1F,
			Key_A              = 0x20,
			Key_S              = 0x21,
			Key_D              = 0x22,
			Key_F              = 0x23,
			Key_G              = 0x24,
			Key_H              = 0x25,
			Key_J              = 0x26,
			Key_K              = 0x27,
			Key_L              = 0x28,
			Key_Semicolon      = 0x29,
			Key_Quote          = 0x2A,
			Key_Number_Sign    = 0x2B, // not on most USA keyboards
//			Undefined          = 0x2C,
			Key_NP_4           = 0x2D,
			Key_NP_5           = 0x2E,
			Key_NP_6           = 0x2F,
			Key_Less           = 0x30, // not on most USA keyboards
			Key_Z              = 0x31,
			Key_X              = 0x32,
			Key_C              = 0x33,
			Key_V              = 0x34,
			Key_B              = 0x35,
			Key_N              = 0x36,
			Key_M              = 0x37,
			Key_Comma          = 0x38,
			Key_Period         = 0x39,
			Key_Slash          = 0x3A,
//			Undefined          = 0x3B,
			Key_NP_Delete      = 0x3C,
			Key_NP_7           = 0x3D,
			Key_NP_8           = 0x3E,
			Key_NP_9           = 0x3F,
			Key_Space          = 0x40,
			Key_Backspace      = 0x41,
			Key_Tab            = 0x42,
			Key_NP_Enter       = 0x43,
			Key_Return         = 0x44,
			Key_Escape         = 0x45,
			Key_Delete         = 0x46,
//			Undefined          = 0x47,
//			Undefined          = 0x48,
//			Undefined          = 0x49,
			Key_NP_Sub         = 0x4A,
//			Undefined          = 0x4B,
			Key_Up             = 0x4C,
			Key_Down           = 0x4D,
			Key_Right          = 0x4E,
			Key_Left           = 0x4F,
			Key_F1             = 0x50,
			Key_F2             = 0x51,
			Key_F3             = 0x52,
			Key_F4             = 0x53,
			Key_F5             = 0x54,
			Key_F6             = 0x55,
			Key_F7             = 0x56,
			Key_F8             = 0x57,
			Key_F9             = 0x58,
			Key_F10            = 0x59,
			Key_NP_Left_Paren  = 0x5A,
			Key_NP_Right_Paren = 0x5B,
			Key_NP_Div         = 0x5C,
			Key_NP_Mul         = 0x5D,
			Key_NP_Add         = 0x5E,
			Key_Help           = 0x5F,
			Key_Left_Shift     = 0x60,
			Key_Right_Shift    = 0x61,
			Key_Caps_Lock      = 0x62,
			Key_Ctrl           = 0x63,
			Key_Left_Alt       = 0x64,
			Key_Right_Alt      = 0x65,
			Key_Left_Amiga     = 0x66,
			Key_Right_Amiga    = 0x67,
		}
	}
}
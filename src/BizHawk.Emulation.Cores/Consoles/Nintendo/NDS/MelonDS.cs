using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

using Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.MelonDS, "Arisotura", "0.9.3", "http://melonds.kuribo64.net/", singleInstance: true, isReleased: false)]
	public class NDS : WaterboxCore, ISaveRam,
		ISettable<NDS.Settings, NDS.SyncSettings>
	{
		private readonly LibMelonDS _core;

		[CoreConstructor("NDS")]
		public NDS(CoreLoadParameters<Settings, SyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 256,
				DefaultHeight = 384,
				MaxWidth = 256,
				MaxHeight = 384,
				MaxSamples = 1024,
				DefaultFpsNumerator = 33513982,
				DefaultFpsDenominator = 560190,
				SystemId = "NDS"
			})
		{
			var roms = lp.Roms.Select(r => r.RomData).ToList();

			if (roms.Count > 3)
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			bool gbacartpresent = roms.Count > 1;

			_core = PreInit<LibMelonDS>(new WaterboxOptions
			{
				Filename = "melonDS.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 4,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 512 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			_syncSettings = lp.SyncSettings ?? new SyncSettings();
			_settings = lp.Settings ?? new Settings();

			var bios7 = _syncSettings.UseRealBIOS
				? lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios7"))
				: null;

			var bios9 = _syncSettings.UseRealBIOS
				? lp.Comm.CoreFileProvider.GetFirmwareOrThrow(new("NDS", "bios9"))
				: null;

			var fw = lp.Comm.CoreFileProvider.GetFirmware(new("NDS", "firmware"));

			bool skipfw = _syncSettings.SkipFirmware || !_syncSettings.UseRealBIOS || fw == null;

			LibMelonDS.LoadFlags flags = LibMelonDS.LoadFlags.NONE;

			if (_syncSettings.UseRealBIOS)
				flags |= LibMelonDS.LoadFlags.USE_REAL_BIOS;
			if (skipfw)
				flags |= LibMelonDS.LoadFlags.SKIP_FIRMWARE;
			if (gbacartpresent)
				flags |= LibMelonDS.LoadFlags.GBA_CART_PRESENT;
			if (_settings.AccurateAudioBitrate)
				flags |= LibMelonDS.LoadFlags.ACCURATE_AUDIO_BITRATE;
			if (_syncSettings.FirmwareOverride || lp.DeterministicEmulationRequested)
				flags |= LibMelonDS.LoadFlags.FIRMWARE_OVERRIDE;

			var fwSettings = new LibMelonDS.FirmwareSettings();
			var name = Encoding.UTF8.GetBytes(_syncSettings.FirmwareUsername);
			fwSettings.FirmwareUsernameLength = name.Length;
			fwSettings.FirmwareLanguage = _syncSettings.FirmwareLanguage;
			if (_syncSettings.FirmwareBootStyle == SyncSettings.BootStyle.AutoBoot) fwSettings.FirmwareLanguage |= (SyncSettings.Language)0x40;
			fwSettings.FirmwareBirthdayMonth = _syncSettings.FirmwareBirthdayMonth;
			fwSettings.FirmwareBirthdayDay = _syncSettings.FirmwareBirthdayDay;
			fwSettings.FirmwareFavouriteColour = _syncSettings.FirmwareFavouriteColour;
			var message = Encoding.UTF8.GetBytes(_syncSettings.FirmwareMessage);
			fwSettings.FirmwareMessageLength = message.Length;

			_exe.AddReadonlyFile(roms[0], "game.rom");
			if (gbacartpresent)
			{
				_exe.AddReadonlyFile(roms[1], "gba.rom");
				if (roms[2] != null)
				{
					_exe.AddReadonlyFile(roms[2], "gba.ram");
				}
			}
			if (_syncSettings.UseRealBIOS)
			{
				_exe.AddReadonlyFile(bios7, "bios7.rom");
				_exe.AddReadonlyFile(bios9, "bios9.rom");
			}
			if (fw != null)
			{
				MaybeWarnIfBadFw(fw);
				SanitizeFw(fw);
				_exe.AddReadonlyFile(fw, "firmware.bin");
			}

			unsafe
			{
				fixed (byte* namePtr = &name[0], messagePtr = &message[0])
				{
					fwSettings.FirmwareUsername = (IntPtr)namePtr;
					fwSettings.FirmwareMessage = (IntPtr)messagePtr;
					if (!_core.Init(flags, fwSettings))
					{
						throw new InvalidOperationException("Init returned false!");
					}
				}
			}


			_exe.RemoveReadonlyFile("game.rom");
			if (gbacartpresent)
			{
				_exe.RemoveReadonlyFile("gba.rom");
				if (roms[2] != null)
				{
					_exe.RemoveReadonlyFile("gba.ram");
				}
			}
			if (_syncSettings.UseRealBIOS)
			{
				_exe.RemoveReadonlyFile("bios7.rom");
				_exe.RemoveReadonlyFile("bios9.rom");
			}
			if (fw != null)
			{
				_exe.RemoveReadonlyFile("firmware.bin");
			}

			PostInit();

			DeterministicEmulation = lp.DeterministicEmulationRequested || (!_syncSettings.UseRealTime);
			InitializeRtc(_syncSettings.InitialTime);

			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DEFAULT, 32768, 44100, 32768, 44100, null, this);
			_serviceProvider.Register<ISoundProvider>(_resampler);
		}

		private SpeexResampler _resampler;
		public override void Dispose()
		{
			base.Dispose();
			if (_resampler != null)
			{
				_resampler.Dispose();
				_resampler = null;
			}
		}

		public override ControllerDefinition ControllerDefinition => NDSController;

		public static readonly ControllerDefinition NDSController = new ControllerDefinition
		{
			Name = "NDS Controller",
			BoolButtons =
			{
				"Up", "Down", "Left", "Right", "Start", "Select", "B", "A", "Y", "X", "L", "R", "LidOpen", "LidClose", "Touch", "Power"
			}
		}.AddXYPair("Touch {0}", AxisPairOrientation.RightAndUp, 0.RangeTo(255), 128, 0.RangeTo(191), 96)
			.AddAxis("Mic Input", 0.RangeTo(2047), 0)
				.AddAxis("GBA Light Sensor", 0.RangeTo(10), 0);
		private LibMelonDS.Buttons GetButtons(IController c)
		{
			LibMelonDS.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibMelonDS.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibMelonDS.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibMelonDS.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibMelonDS.Buttons.RIGHT;
			if (c.IsPressed("Start"))
				b |= LibMelonDS.Buttons.START;
			if (c.IsPressed("Select"))
				b |= LibMelonDS.Buttons.SELECT;
			if (c.IsPressed("B"))
				b |= LibMelonDS.Buttons.B;
			if (c.IsPressed("A"))
				b |= LibMelonDS.Buttons.A;
			if (c.IsPressed("Y"))
				b |= LibMelonDS.Buttons.Y;
			if (c.IsPressed("X"))
				b |= LibMelonDS.Buttons.X;
			if (c.IsPressed("L"))
				b |= LibMelonDS.Buttons.L;
			if (c.IsPressed("R"))
				b |= LibMelonDS.Buttons.R;
			if (c.IsPressed("LidOpen"))
				b |= LibMelonDS.Buttons.LIDOPEN;
			if (c.IsPressed("LidClose"))
				b |= LibMelonDS.Buttons.LIDCLOSE;
			if (c.IsPressed("Touch"))
				b |= LibMelonDS.Buttons.TOUCH;
			if (c.IsPressed("Power"))
				b |= LibMelonDS.Buttons.POWER;

			return b;
		}

		public new bool SaveRamModified => _core.SaveRamIsDirty();

		public new byte[] CloneSaveRam()
		{
			int length = _core.GetSaveRamLength();

			if (length > 0)
			{
				byte[] ret = new byte[length];
				_core.GetSaveRam(ret);
				return ret;
			}

			return new byte[0];
		}

		public new void StoreSaveRam(byte[] data)
		{
			if (data.Length > 0)
			{
				_core.PutSaveRam(data, (uint)data.Length);
			}
		}

		private Settings _settings;
		private SyncSettings _syncSettings;

		public enum ScreenLayoutKind
		{
			Vertical,
			Horizontal,
			Top,
			Bottom,
		}

		public enum ScreenRotationKind
		{
			Rotate0,
			Rotate90,
			Rotate180,
			Rotate270
		}

		public class Settings
		{
			[DisplayName("Screen Layout")]
			[Description("Adjusts the layout of the screens")]
			[DefaultValue(ScreenLayoutKind.Vertical)]
			public ScreenLayoutKind ScreenLayout { get; set; }

			[DisplayName("Invert Screens")]
			[Description("Inverts the order of the screens.")]
			[DefaultValue(false)]
			public bool ScreenInvert { get; set; }

			[DisplayName("Rotation")]
			[Description("Adjusts the orientation of the screens")]
			[DefaultValue(ScreenRotationKind.Rotate0)]
			public ScreenRotationKind ScreenRotation { get; set; }

			[DisplayName("Screen Gap")]
			[DefaultValue(0)]
			public int ScreenGap { get; set; }

			[DisplayName("Accurate Audio Bitrate")]
			[Description("If true, the audio bitrate will be set to 10. Otherwise, it will be set to 16.")]
			[DefaultValue(true)]
			public bool AccurateAudioBitrate { get; set; }

			public Settings Clone()
			{
				return (Settings)MemberwiseClone();
			}

			public static bool NeedsReboot(Settings x, Settings y)
			{
				return false;
			}

			public Settings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public class SyncSettings
		{
			private static readonly DateTime minDate = new DateTime(2000, 1, 1);
			private static readonly DateTime maxDate = new DateTime(2099, 12, 31, 23, 59, 59);

			[JsonIgnore]
			private DateTime _initaltime;

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2000-01-01")]
			public DateTime InitialTime
			{
				get => _initaltime;
				set
				{
					if (DateTime.Compare(minDate, value) > 0)
					{
						_initaltime = minDate;
					}
					else if (DateTime.Compare(maxDate, value) < 0)
					{
						_initaltime = maxDate;
					}
					else
					{
						_initaltime = value;
					}

				}
			}

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(false)]
			public bool UseRealTime { get; set; }

			[DisplayName("Use Real BIOS")]
			[Description("If true, real BIOS files will be used.")]
			[DefaultValue(false)]
			public bool UseRealBIOS { get; set; }

			[DisplayName("Skip Firmware")]
			[Description("If true, initial firmware boot will be skipped. Forced true if firmware cannot be booted (no real bios or missing firmware).")]
			[DefaultValue(false)]
			public bool SkipFirmware { get; set; }

			[DisplayName("Firmware Override")]
			[Description("If true, the firmware settings will be overriden by provided settings. Forced true when recording a movie.")]
			[DefaultValue(false)]
			public bool FirmwareOverride { get; set; }

			public enum BootStyle : int
			{
				AutoBoot,
				ManualBoot,
			}

			[DisplayName("Firmware Boot Style")]
			[Description("Style which the NDS boots. Only applicable if firmware override is in effect.")]
			[DefaultValue(BootStyle.AutoBoot)]
			public BootStyle FirmwareBootStyle { get; set; }

			[JsonIgnore]
			private string _firmwareusername;

			[DisplayName("Firmware Username")]
			[Description("Username in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("MelonDS")]
			public string FirmwareUsername
			{
				get => _firmwareusername;
				set => _firmwareusername = value.Substring(0, Math.Min(value.Length, 10));
			}

			public enum Language : int
			{
				Japanese,
				English,
				French,
				German,
				Italian,
				Spanish,
			}

			[DisplayName("Firmware Language")]
			[Description("Language in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Language.English)]
			public Language FirmwareLanguage { get; set; }

			public enum Month : int
			{
				January = 1,
				February,
				March,
				April,
				May,
				June,
				July,
				August,
				September,
				October,
				November,
				December,
			}

			[JsonIgnore]
			private Month _firmwarebirthdaymonth;

			[JsonIgnore]
			private int _firmwarebirthdayday;

			[DisplayName("Firmware Birthday Month")]
			[Description("Birthday month in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Month.May)]
			public Month FirmwareBirthdayMonth
			{
				get => _firmwarebirthdaymonth;
				set
				{
					FirmwareBirthdayDay = SanitizeBirthdayDay(FirmwareBirthdayDay, value);
					_firmwarebirthdaymonth = value;
				}
			}

			[DisplayName("Firmware Birthday Day")]
			[Description("Birthday day in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(15)]
			public int FirmwareBirthdayDay
			{
				get => _firmwarebirthdayday;
				set => _firmwarebirthdayday = SanitizeBirthdayDay(value, FirmwareBirthdayMonth);
			}

			public enum Color : int
			{
				GreyishBlue,
				Brown,
				Red,
				LightPink,
				Orange,
				Yellow,
				Lime,
				LightGreen,
				DarkGreen,
				Turqoise,
				LightBlue,
				Blue,
				DarkBlue,
				DarkPurple,
				LightPurple,
				DarkPink,
			}

			[DisplayName("Firmware Favorite Color")]
			[Description("Favorite color in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue(Color.Red)]
			public Color FirmwareFavouriteColour { get; set; }

			[JsonIgnore]
			private string _firmwaremessage;

			[DisplayName("Firmware Message")]
			[Description("Message in firmware. Only applicable if firmware override is in effect.")]
			[DefaultValue("Melons Taste Great!")]
			public string FirmwareMessage
			{
				get => _firmwaremessage;
				set => _firmwaremessage = value.Substring(0, Math.Min(value.Length, 26));
			}

			public SyncSettings Clone()
			{
				return (SyncSettings)MemberwiseClone();
			}

			public static bool NeedsReboot(SyncSettings x, SyncSettings y)
			{
				return !DeepEquality.DeepEquals(x, y);
			}

			public SyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}
		}

		public Settings GetSettings()
		{
			return _settings.Clone();
		}

		public SyncSettings GetSyncSettings()
		{
			return _syncSettings.Clone();
		}

		public PutSettingsDirtyBits PutSettings(Settings o)
		{
			var ret = Settings.NeedsReboot(_settings, o);
			_settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(SyncSettings o)
		{
			var ret = SyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		private static int SanitizeBirthdayDay(int day, SyncSettings.Month fwMonth)
		{
			int maxdays;
			switch (fwMonth)
			{
				case SyncSettings.Month.February:
				{
					maxdays = 29;
					break;
				}
				case SyncSettings.Month.April:
				case SyncSettings.Month.June:
				case SyncSettings.Month.September:
				case SyncSettings.Month.November:
				{
					maxdays = 30;
					break;
				}
				default:
				{
					maxdays = 31;
					break;
				}
			}

			return Math.Max(1, Math.Min(day, maxdays));
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibMelonDS.FrameInfo
			{
				Time = GetRtcTime(!DeterministicEmulation),
				Keys = GetButtons(controller),
				TouchX = (byte)controller.AxisValue("Touch X"),
				TouchY = (byte)controller.AxisValue("Touch Y"),
				MicInput = (short)controller.AxisValue("Mic Input"),
				GBALightSensor = (byte)controller.AxisValue("GBA Light Sensor"),
			};
		}

		// c++ -> c# port of melon's firmware verification code
		private static unsafe ushort Crc16(byte* data, int len, int seed)
		{
			var poly = new ushort[8] { 0xC0C1, 0xC181, 0xC301, 0xC601, 0xCC01, 0xD801, 0xF001, 0xA001 };

			for (int i = 0; i < len; i++)
			{
				seed ^= data[i];

				for (int j = 0; j < 8; j++)
				{
					if ((seed & 0x1) != 0)
					{
						seed >>= 1;
						seed ^= (poly[j] << (7 - j));
					}
					else
					{
						seed >>= 1;
					}
				}
			}

			return (ushort)(seed & 0xFFFF);
		}

		private unsafe bool VerifyCrc16(byte[] fw, int startaddr, int len, int seed, int crcaddr)
		{
			ushort storedCrc16 = (ushort)((fw[crcaddr + 1] << 8) | fw[crcaddr]);
			fixed (byte* start = &fw[startaddr])
			{
				ushort actualCrc16 = Crc16(start, len, seed);
				return storedCrc16 == actualCrc16;
			}
		}

		private void MaybeWarnIfBadFw(byte[] fw)
		{
			if (fw.Length != 0x20000 && fw.Length != 0x40000 && fw.Length != 0x80000)
			{
				CoreComm.ShowMessage("Bad firmware length detected! Firmware might not work!");
				return;
			}
			int fwMask = fw.Length - 1;
			string badCrc16s = "";
			if (!VerifyCrc16(fw, 0x2C, (fw[0x2C + 1] << 8) | fw[0x2C], 0x0000, 0x2A))
			{
				badCrc16s += " Wifi ";
			}
			if (!VerifyCrc16(fw, 0x7FA00 & fwMask, 0xFE, 0x0000, 0x7FAFE & fwMask))
			{
				badCrc16s += " AP1 ";
			}
			if (!VerifyCrc16(fw, 0x7FB00 & fwMask, 0xFE, 0x0000, 0x7FBFE & fwMask))
			{
				badCrc16s += " AP2 ";
			}
			if (!VerifyCrc16(fw, 0x7FC00 & fwMask, 0xFE, 0x0000, 0x7FCFE & fwMask))
			{
				badCrc16s += " AP3 ";
			}
			if (!VerifyCrc16(fw, 0x7FE00 & fwMask, 0x70, 0xFFFF, 0x7FE72 & fwMask))
			{
				badCrc16s += " USER0 ";
			}
			if (!VerifyCrc16(fw, 0x7FF00 & fwMask, 0x70, 0xFFFF, 0x7FF72 & fwMask))
			{
				badCrc16s += " USER1 ";
			}
			if (badCrc16s != "")
			{
				CoreComm.ShowMessage("Bad Firmware CRC16(s) detected! Firmware might not work! Bad CRC16(s): " + badCrc16s);
			}
		}

		private static void SanitizeFw(byte[] fw)
		{
			int fwMask = fw.Length - 1;
			int[] apstart = new int[3] { 0x07FA00 & fwMask, 0x07FB00 & fwMask, 0x07FC00 & fwMask };

			for (int i = 0; i < 3; i++)
			{
				for (int j = 0; j < 0x100; j++)
				{
					fw[apstart[i] + j] = 0;
				}
			}

			// gbatek marks these as unknown, they seem to depend on the mac address???
			// bytes 4 (upper nibble only) and 5 also seem to be just random?
			// various combinations noted (noting last 2 bytes are crc16)
			// F8 98 C1 E6 CC DD A9 E1 85 D4 9B
			// F8 98 C1 E6 CC 1D 66 E1 85 D8 A4
			// F8 98 C1 E6 CC 9D 6B E1 85 60 A7
			// F8 98 C1 E6 CC 5D 92 E1 85 8C 96
			// different mac address
			// 18 90 15 E9 7C 1D F1 E1 85 74 02
			byte[] macdependentbytes = new byte[11] { 0xF8, 0x98, 0xC1, 0xE6, 0xCC, 0x9D, 0xBE, 0xE1, 0x85, 0x71, 0x5F };

			int apoffset = 0xF5;

			for (int i = 0; i < 2; i++)
			{
				for (int j = 0; j < 11; j++)
				{
					fw[apstart[i] + apoffset + j] = macdependentbytes[j];
				}
			}

			int ffoffset = 0xE7;

			for (int i = 0; i < 3; i++)
			{
				fw[apstart[i] + ffoffset] = 0xFF;
			}

			// slot 3 doesn't have those mac dependent bytes???
			fw[apstart[2] + 0xFE] = 0x0A;
			fw[apstart[2] + 0xFF] = 0xF0;
		}
	}
}

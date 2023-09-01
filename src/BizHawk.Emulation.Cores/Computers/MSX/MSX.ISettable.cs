﻿using System.ComponentModel;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : ISettable<MSX.MSXSettings, MSX.MSXSyncSettings>
	{
		public MSXSettings GetSettings() => Settings.Clone();

		public MSXSyncSettings GetSyncSettings() => SyncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(MSXSettings o)
		{
			bool ret = MSXSettings.RebootNeeded(Settings, o);
			Settings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(MSXSyncSettings o)
		{
			bool ret = MSXSyncSettings.RebootNeeded(SyncSettings, o);
			SyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		internal MSXSettings Settings { get; private set; }
		internal MSXSyncSettings SyncSettings { get; private set; }

		public class MSXSettings
		{
			// graphics settings
			[DisplayName("Show Background")]
			[Description("Display BG Layer")]
			[DefaultValue(true)]
			public bool DispBG { get; set; }

			[DisplayName("Show Sprites")]
			[Description("Display Sprites")]
			[DefaultValue(true)]
			public bool DispOBJ { get; set; }

			public MSXSettings Clone() => (MSXSettings)MemberwiseClone();

			public MSXSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool RebootNeeded(MSXSettings x, MSXSettings y) => false;
		}

		public class MSXSyncSettings
		{
			public enum ContrType
			{
				Joystick,
				Keyboard
			}

			public enum RegionType
			{
				USA,
				JPN
			}

			[DisplayName("Controller Configuration")]
			[Description("Pick Between Controller Types")]
			[DefaultValue(ContrType.Joystick)]
			public ContrType Contr_Setting { get; set; }

			[DisplayName("Region Configuration")]
			[Description("Select Console Region")]
			[DefaultValue(RegionType.USA)]
			public RegionType Region_Setting { get; set; }


			public MSXSyncSettings Clone() => (MSXSyncSettings)MemberwiseClone();

			public MSXSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool RebootNeeded(MSXSyncSettings x, MSXSyncSettings y) => true;
		}
	}
}

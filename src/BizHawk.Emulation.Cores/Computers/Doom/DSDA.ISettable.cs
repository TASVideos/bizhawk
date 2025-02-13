using System.ComponentModel;

using BizHawk.Emulation.Common;
using BizHawk.Common;
using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : ISettable<DSDA.DoomSettings, DSDA.DoomSyncSettings>
	{
		public enum CompatibilityLevelEnum : int
		{
			[Display(Name = "0 - Doom v1.2")]
			C0 = 0,
			[Display(Name = "1 - Doom v1.666")]
			C1 = 1,
			[Display(Name = "2 - Doom v1.9")]
			C2 = 2,
			[Display(Name = "3 - Ultimate Doom & Doom95")]
			C3 = 3,
			[Display(Name = "4 - Final Doom")]
			C4 = 4,
			[Display(Name = "5 - DOSDoom")]
			C5 = 5,
			[Display(Name = "6 - TASDoom")]
			C6 = 6,
			[Display(Name = "7 - Boom's Inaccurate Vanilla Compatibility Mode")]
			C7 = 7,
			[Display(Name = "8 - Boom v2.01")]
			C8 = 8,
			[Display(Name = "9 - Boom v2.02")]
			C9 = 9,
			[Display(Name = "10 - LxDoom")]
			C10 = 10,
			[Display(Name = "11 - MBF")]
			C11 = 11,
			[Display(Name = "12 - PrBoom v2.03beta")]
			C12 = 12,
			[Display(Name = "13 - PrBoom v2.1.0")]
			C13 = 13,
			[Display(Name = "14 - PrBoom v2.1.1 - 2.2.6")]
			C14 = 14,
			[Display(Name = "15 - PrBoom v2.3.x")]
			C15 = 15,
			[Display(Name = "16 - PrBoom v2.4.0")]
			C16 = 16,
			[Display(Name = "17 - PrBoom Latest Default")]
			C17 = 17,
			[Display(Name = "21 - MBF21")]
			C21 = 21
		}

		public enum SkillLevelEnum : int
		{
			[Display(Name = "1 - I'm too young to die")]
			S1 = 1,
			[Display(Name = "2 - Hey, not too rough")]
			S2 = 2,
			[Display(Name = "3 - Hurt me plenty")]
			S3 = 3,
			[Display(Name = "4 - Ultra-Violence")]
			S4 = 4,
			[Display(Name = "5 - Nightmare!")]
			S5 = 5

		}

		public enum MultiplayerModeEnum : int
		{
			[Display(Name = "0 - Single Player / Coop")]
			M0 = 0,
			[Display(Name = "1 - Deathmatch")]
			M1 = 1,
			[Display(Name = "2 - Altdeath")]
			M2 = 2
		}

		public enum HexenClassEnum : int
		{
			[Display(Name = "Fighter")]
			C1 = 1,
			[Display(Name = "Cleric")]
			C2 = 2,
			[Display(Name = "Mage")]
			C3 = 3
		}

		public const int TURBO_AUTO = -1;

		private DoomSettings _settings;
		private DoomSyncSettings _syncSettings;

		public DoomSettings GetSettings()
			=> _settings.Clone();

		public DoomSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(object o)
			=> PutSettingsDirtyBits.None;

		public PutSettingsDirtyBits PutSyncSettings(DoomSyncSettings o)
		{
			var ret = DoomSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class DoomSettings
		{
			[JsonIgnore]
			[DisplayName("Player Point of View")]
			[Description("Which of the players' point of view to use during rendering")]
			[Range(1, 4)]
			[DefaultValue(1)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int DisplayPlayer { get; set; }

			public DoomSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public DoomSettings Clone()
				=> (DoomSettings) MemberwiseClone();

			public static bool NeedsReboot(DoomSettings x, DoomSettings y)
				=> false;
		}
		public PutSettingsDirtyBits PutSettings(DoomSettings o)
		{
			var ret = DoomSettings.NeedsReboot(_settings, o);
			_settings = o;
			if (_settings.DisplayPlayer == 1 && !_syncSettings.Player1Present) throw new Exception($"Trying to set display player '{_settings.DisplayPlayer}' but it is not active in this movie.");
			if (_settings.DisplayPlayer == 2 && !_syncSettings.Player2Present) throw new Exception($"Trying to set display player '{_settings.DisplayPlayer}' but it is not active in this movie.");
			if (_settings.DisplayPlayer == 3 && !_syncSettings.Player3Present) throw new Exception($"Trying to set display player '{_settings.DisplayPlayer}' but it is not active in this movie.");
			if (_settings.DisplayPlayer == 4 && !_syncSettings.Player4Present) throw new Exception($"Trying to set display player '{_settings.DisplayPlayer}' but it is not active in this movie.");
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class DoomSyncSettings
		{
			[DefaultValue(DoomControllerTypes.Doom)]
			[DisplayName("Input Format")]
			[Description("The format provided for the players' input.")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public DoomControllerTypes InputFormat { get; set; }

			[DisplayName("Player 1 Present")]
			[Description("Specifies if player 1 is present")]
			[DefaultValue(true)]
			public bool Player1Present { get; set; }

			[DisplayName("Player 2 Present")]
			[Description("Specifies if player 2 is present")]
			[DefaultValue(false)]
			public bool Player2Present { get; set; }

			[DisplayName("Player 3 Present")]
			[Description("Specifies if player 3 is present")]
			[DefaultValue(false)]
			public bool Player3Present { get; set; }

			[DisplayName("Player 4 Present")]
			[Description("Specifies if player 4 is present")]
			[DefaultValue(false)]
			public bool Player4Present { get; set; }

			[DisplayName("Compatibility Mode")]
			[Description("The version of Doom or its ports that this movie is meant to emulate.")]
			[DefaultValue(CompatibilityLevelEnum.C2)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public CompatibilityLevelEnum CompatibilityMode { get; set; }

			[DisplayName("Skill Level")]
			[Description("Establishes the general difficulty settings.")]
			[DefaultValue(SkillLevelEnum.S4)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SkillLevelEnum SkillLevel { get; set; }

			[DisplayName("Multiplayer Mode")]
			[Description("Indicates the multiplayer mode")]
			[DefaultValue(MultiplayerModeEnum.M0)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public MultiplayerModeEnum MultiplayerMode { get; set; }

			[DisplayName("Initial Episode")]
			[Description("Selects the initial episode. Use '0' for non-episodic IWads (e.g., DOOM2)")]
			[DefaultValue(0)]
			public int InitialEpisode { get; set; }

			[DisplayName("Initial Map")]
			[Description("Selects the initial map.")]
			[DefaultValue(1)]
			public int InitialMap { get; set; }

			[DisplayName("Turbo")]
			[Description("Modifies the player running / strafing speed [0-255]. -1 means Disabled.")]
			[Range(TURBO_AUTO, 255)]
			[DefaultValue(TURBO_AUTO)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int Turbo { get; set; }

			[DisplayName("Fast Monsters")]
			[Description("Makes monsters move and attack much faster (overriden to true when playing Nightmare! difficulty)")]
			[DefaultValue(false)]
			public bool FastMonsters { get; set; }

			[DisplayName("Monsters Respawn")]
			[Description("Makes monsters respawn shortly after dying (overriden to true when playing Nightmare! difficulty)")]
			[DefaultValue(false)]
			public bool MonstersRespawn { get; set; }

			[DisplayName("No Monsters")]
			[Description("Removes all monsters from the level.")]
			[DefaultValue(false)]
			public bool NoMonsters { get; set; }

			[DisplayName("Player 1 Hexen Class")]
			[Description("The Hexen class to use for player 1. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClassEnum.C1)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClassEnum Player1Class { get; set; }

			[DisplayName("Player 2 Hexen Class")]
			[Description("The Hexen class to use for player 2. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClassEnum.C1)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClassEnum Player2Class { get; set; }

			[DisplayName("Player 3 Hexen Class")]
			[Description("The Hexen class to use for player 3. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClassEnum.C1)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClassEnum Player3Class { get; set; }

			[DisplayName("Player 4 Hexen Class")]
			[Description("The Hexen class to use for player 4. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClassEnum.C1)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClassEnum Player4Class { get; set; }

			[DisplayName("Chain Episodes")]
			[Description("Completing one episode leads to the next without interruption.")]
			[DefaultValue(false)]
			public bool ChainEpisodes { get; set; }

			[DisplayName("Strict Mode")]
			[Description("Sets strict mode restrictions, preventing TAS-only inputs.")]
			[DefaultValue(true)]
			public bool StrictMode { get; set; }

			[DisplayName("Prevent Level Exit")]
			[Description("Level exit triggers won't have an effect. This is useful for debugging / optimizing / botting purposes.")]
			[DefaultValue(false)]
			public bool PreventLevelExit { get; set; }

			[DisplayName("Prevent Game End")]
			[Description("Game end triggers won't have an effect. This is useful for debugging / optimizing / botting purposes.")]
			[DefaultValue(false)]
			public bool PreventGameEnd { get; set; }

			[DisplayName("Mouse Running Sensitivity")]
			[Description("How fast the Doom player will run when using the mouse")]
			[DefaultValue(10)]
			public int MouseRunSensitivity { get; set; }

			[DisplayName("Mouse Turning Sensitivity")]
			[Description("How fast the Doom player will turn when using the mouse")]
			[DefaultValue(1)]
			public int MouseTurnSensitivity { get; set; }

			public CInterface.InitSettings GetNativeSettings(GameInfo game)
			{
				return new CInterface.InitSettings
				{
					_Player1Present = Player1Present ? 1 : 0,
					_Player2Present = Player2Present ? 1 : 0,
					_Player3Present = Player3Present ? 1 : 0,
					_Player4Present = Player4Present ? 1 : 0,
					_CompatibilityMode = (int)CompatibilityMode,
					_SkillLevel = (int) SkillLevel,
					_MultiplayerMode = (int) MultiplayerMode,
					_InitialEpisode = InitialEpisode,
					_InitialMap = InitialMap,
					_Turbo = Turbo,
					_FastMonsters = FastMonsters ? 1 : 0,
					_MonstersRespawn = MonstersRespawn ? 1 : 0,
					_NoMonsters = NoMonsters ? 1 : 0,
					_Player1Class = (int) Player1Class,
					_Player2Class = (int) Player2Class,
					_Player3Class = (int) Player3Class,
					_Player4Class = (int) Player4Class,
					_ChainEpisodes = ChainEpisodes ? 1 : 0,
					_StrictMode = StrictMode ? 1 : 0,
					_PreventLevelExit = PreventLevelExit ? 1 : 0,
					_PreventGameEnd = PreventGameEnd ? 1 : 0
					// MouseRunSensitivity is handled at Bizhawk level
					// MouseTurnSensitivity is handled at Bizhawk level
				};
			}

			public DoomSyncSettings Clone()
				=> (DoomSyncSettings)MemberwiseClone();

			public DoomSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(DoomSyncSettings x, DoomSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}

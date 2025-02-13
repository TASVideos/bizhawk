using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

namespace BizHawk.Client.Common
{
	// LMP file format: https://doomwiki.org/wiki/Demo#Technical_information
	// In better detail, from archive.org: http://web.archive.org/web/20070630072856/http://demospecs.planetquake.gamespy.com/lmp/lmp.html
	[ImporterFor("Doom", ".lmp")]
	internal class DoomLmpImport : MovieImporter
	{
		protected override void RunImport()
		{
			var input = SourceFile.OpenRead().ReadAllBytes();
			var i = 0;
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.DSDA;
			Result.Movie.SystemID = VSystemID.Raw.Doom;

			// Try to decide game version based on signature
			var signature = input[i];
			DSDA.CompatibilityLevelEnum presumedCompatibilityLevel;
			if (signature <= 102)
			{
				// there is no signature, the first byte is the skill level, so don't advance
				Console.WriteLine("Reading DOOM LMP demo version: <=1.12");
				presumedCompatibilityLevel = DSDA.CompatibilityLevelEnum.C0;
			}
			else
			{
				i++;
				Console.WriteLine("Reading DOOM LMP demo version: {0}", signature);
				presumedCompatibilityLevel = signature < 109
					? DSDA.CompatibilityLevelEnum.C1 // 1.666
					: DSDA.CompatibilityLevelEnum.C2; // 1.9
			}

			DSDA.DoomSyncSettings syncSettings = new()
			{
				InputFormat = DoomControllerTypes.Doom,
				CompatibilityMode = presumedCompatibilityLevel,
				SkillLevel = (DSDA.SkillLevelEnum) (1 + input[i++]),
				InitialEpisode = input[i++],
				InitialMap = input[i++],
				MultiplayerMode = (DSDA.MultiplayerModeEnum) input[i++],
				MonstersRespawn = input[i++] is not 0,
				FastMonsters = input[i++] is not 0,
				NoMonsters = input[i++] is not 0,
			};
			_ = input[i++]; // DisplayPlayer is a non-sync setting so importers can't* set it
			syncSettings.Player1Present = input[i++] is not 0;
			syncSettings.Player2Present = input[i++] is not 0;
			syncSettings.Player3Present = input[i++] is not 0;
			syncSettings.Player4Present = input[i++] is not 0;
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);

			var doomController1 = new DoomController(1);
			var controller = new SimpleController(doomController1.Definition);
			controller.Definition.BuildMnemonicsCache(Result.Movie.SystemID);
			void ParsePlayer(string playerPfx)
			{
				controller.AcceptNewAxis(playerPfx + "Run Speed", unchecked((sbyte) input[i++]));

				controller.AcceptNewAxis(playerPfx + "Strafing Speed", unchecked((sbyte) input[i++]));

				controller.AcceptNewAxis(playerPfx + "Turning Speed", unchecked((sbyte) input[i++]));

				var specialValue = input[i++];
				controller[playerPfx + "Fire"] = (specialValue & 0b00000001) is not 0;
				controller[playerPfx + "Action"] = (specialValue & 0b00000010) is not 0;
				controller.AcceptNewAxis(playerPfx + "Weapon Select", (specialValue & 0b00011100) >> 2);
				controller[playerPfx + "Alt Weapon"] = (specialValue & 0b00100000) is not 0;
			}
			do
			{
				if (syncSettings.Player1Present) ParsePlayer("P1 ");
				if (syncSettings.Player2Present) ParsePlayer("P2 ");
				if (syncSettings.Player3Present) ParsePlayer("P3 ");
				if (syncSettings.Player4Present) ParsePlayer("P4 ");
				Result.Movie.AppendFrame(controller);
				if (i == input.Length) throw new Exception("Reached end of input movie stream without finalization byte");
			}
			while (input[i] is not 0x80);
		}
	}
}

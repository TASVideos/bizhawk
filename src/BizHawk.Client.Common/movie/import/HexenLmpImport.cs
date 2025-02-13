using BizHawk.Common.IOExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Computers.Doom;

namespace BizHawk.Client.Common
{
	// LMP file format: https://doomwiki.org/wiki/Demo#Technical_information
	// In better detail, from archive.org: http://web.archive.org/web/20070630072856/http://demospecs.planetquake.gamespy.com/lmp/lmp.html
	[ImporterFor("Hexen", ".hexenlmp")]
	internal class HexenLmpImport : MovieImporter
	{ 
		protected override void RunImport()
		{
			var input = SourceFile.OpenRead().ReadAllBytes();
			var i = 0;
			Result.Movie.HeaderEntries[HeaderKeys.Core] = CoreNames.DSDA;
			Result.Movie.SystemID = VSystemID.Raw.Doom;
			DSDA.DoomSyncSettings syncSettings = new()
			{
				InputFormat = DoomControllerTypes.Hexen,
				MultiplayerMode = DSDA.MultiplayerModeEnum.M0,
				MonstersRespawn = false,
				FastMonsters = false,
				NoMonsters = false,
				CompatibilityMode = DSDA.CompatibilityLevelEnum.C0,
				SkillLevel = (DSDA.SkillLevelEnum) (1 + input[i++]),
				InitialEpisode = input[i++],
				InitialMap = input[i++],
				Player1Present = input[i++] is not 0,
				Player1Class = (DSDA.HexenClassEnum) input[i++],
				Player2Present = input[i++] is not 0,
				Player2Class = (DSDA.HexenClassEnum) input[i++],
				Player3Present = input[i++] is not 0,
				Player3Class = (DSDA.HexenClassEnum) input[i++],
				Player4Present = input[i++] is not 0,
				Player4Class = (DSDA.HexenClassEnum) input[i++],
			};
			_ = input[i++]; // player 5 isPresent
			_ = input[i++]; // player 5 class
			_ = input[i++]; // player 6 isPresent
			_ = input[i++]; // player 6 class
			_ = input[i++]; // player 7 isPresent
			_ = input[i++]; // player 7 class
			_ = input[i++]; // player 8 isPresent
			_ = input[i++]; // player 8 class
			Result.Movie.SyncSettingsJson = ConfigService.SaveWithType(syncSettings);

			var hexenController = new HexenController(1);
			var controller = new SimpleController(hexenController.Definition);
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

				controller.AcceptNewAxis(playerPfx + "Fly / Look", unchecked((sbyte) input[i++]));

				var useArtifact = input[i++];
				controller.AcceptNewAxis(playerPfx + "Use Artifact", useArtifact & 0b00111111);
				controller[playerPfx + "End Player"] = (useArtifact & 0b01000000) is not 0;
				controller[playerPfx + "Jump"] = (useArtifact & 0b10000000) is not 0;
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

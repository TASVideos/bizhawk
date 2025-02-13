using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public abstract class CInterface
	{
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int load_archive_cb(string filename, IntPtr buffer, int maxsize);

		[StructLayout(LayoutKind.Sequential)]
		public struct InitSettings
		{
			public int _Player1Present;
			public int _Player2Present;
			public int _Player3Present;
			public int _Player4Present;
			public int _CompatibilityMode;
			public int _SkillLevel;
			public int _MultiplayerMode;
			public int _InitialEpisode;
			public int _InitialMap;
			public int _Turbo;
			public int _FastMonsters;
			public int _MonstersRespawn;
			public int _NoMonsters;
			public int _Player1Class;
			public int _Player2Class;
			public int _Player3Class;
			public int _Player4Class;
			public int _ChainEpisodes;
			public int _StrictMode;
			public int _PreventLevelExit;
			public int _PreventGameEnd;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedPlayerInput
		{
			public int _RunSpeed;
			public int _StrafingSpeed;
			public int _TurningSpeed;
			public int _WeaponSelect;
			public int _Fire;
			public int _Action;
			public int _AltWeapon;

			// Hexen + Heretic (Raven Games)
			public int _FlyLook;
			public int _ArtifactUse;

			// Hexen only
			public int _Jump;
			public int _EndPlayer;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct PackedRenderInfo
		{
			public int _RenderVideo;
			public int _RenderAudio;
			public int _PlayerPointOfView;
		}

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_audio(ref int n, ref IntPtr buffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_init(ref InitSettings settings);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_frame_advance(
			ref PackedPlayerInput player1Inputs,
			ref PackedPlayerInput player2Inputs,
			ref PackedPlayerInput player3Inputs,
			ref PackedPlayerInput player4Inputs,
			ref PackedRenderInfo renderInfo);

		[BizImport(CallingConvention.Cdecl)]
		public abstract void dsda_get_video(out int w, out int h, out int pitch, ref IntPtr buffer, out int palSize, ref IntPtr palBuffer);

		[BizImport(CallingConvention.Cdecl)]
		public abstract bool dsda_add_wad_file(
			string fileName,
			int fileSize,
			load_archive_cb feload_archive_cb);
	}
}

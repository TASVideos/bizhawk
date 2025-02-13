using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	[PortedCore(
		name: CoreNames.DSDA,
		author: "The DSDA Team",
		portedVersion: "0.28.2 (fe0dfa0)",
		portedUrl: "https://github.com/kraflab/dsda-doom")]
	[ServiceNotApplicable(typeof(ISaveRam))]
	public partial class DSDA : IRomInfo
	{
		[CoreConstructor(VSystemID.Raw.Doom)]
		public DSDA(CoreLoadParameters<DoomSettings, DoomSyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			_syncSettings = lp.SyncSettings ?? new DoomSyncSettings();
			_settings = lp.Settings ?? new DoomSettings();
			_controllerDeck = new DoomControllerDeck(_syncSettings.InputFormat, _syncSettings.Player1Present, _syncSettings.Player2Present, _syncSettings.Player3Present, _syncSettings.Player4Present);
			_loadCallback = LoadCallback;

			// Gathering information for the rest of the wads
			_wadFiles = lp.Roms;

			// Checking for correct IWAD configuration
			bool foundIWAD = false;
			string IWADName = "";
			foreach (var wadFile in _wadFiles)
			{
				bool recognized = false;
				if (wadFile.RomData is [ (byte) 'I', (byte) 'W', (byte) 'A', (byte) 'D', .. ])
				{
					// Check not more than one IWAD is provided
					if (foundIWAD) throw new Exception($"More than one IWAD provided. Trying to load '{wadFile.RomPath}', but IWAD '{IWADName}' was already provided");
					IWADName = wadFile.RomPath;
					foundIWAD = true;
					recognized = true;
				}
				else if (wadFile.RomData is [ (byte) 'P', (byte) 'W', (byte) 'A', (byte) 'D', .. ])
				{
					recognized = true;
				}
				if (!recognized) throw new Exception($"Unrecognized WAD provided: '{wadFile.RomPath}' has non-standard header.");
			}

			// Check at least one IWAD was provided
			if (!foundIWAD) throw new Exception($"No IWAD was provided");

			// Getting dsda-doom.wad -- required by DSDA
			_dsdaWadFileData = Zstd.DecompressZstdStream(new MemoryStream(Resources.DSDA_DOOM_WAD.Value)).ToArray();

			// Getting sum of wad sizes for the accurate calculation of the invisible heap
			uint totalWadSize = (uint)_dsdaWadFileData.Length;
			foreach (var wadFile in _wadFiles) totalWadSize += (uint) wadFile.FileData.Length;
			uint totalWadSizeKb = (totalWadSize / 1024) + 1;
			Console.WriteLine("Reserving {0}kb for WAD file memory", totalWadSizeKb);

			_elf = new WaterboxHost(new WaterboxOptions
			{
				Path = PathUtils.DllDirectoryPath,
				Filename = "dsda.wbx",
				SbrkHeapSizeKB = 64 * 1024, // This core loads quite a bunch of things on global mem -- reserve enough memory
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = totalWadSizeKb + 4 * 1024, // Make sure there's enough space for the wads
				PlainHeapSizeKB = 4 * 1024,
				MmapHeapSizeKB = 128 * 1024, // Allow the game to malloc quite a lot of objects to support one of those big wads
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			try
			{
				var callingConventionAdapter = CallingConventionAdapters.MakeWaterbox(
				[
					_loadCallback
				], _elf);

				using (_elf.EnterExit())
				{
					Core = BizInvoker.GetInvoker<CInterface>(_elf, _elf, callingConventionAdapter);

					// Adding dsda-doom wad file
					Core.dsda_add_wad_file(_dsdaWadFileName, _dsdaWadFileData.Length, _loadCallback);

					// Adding rom files
					foreach (var wadFile in _wadFiles)
					{
						var loadWadResult = Core.dsda_add_wad_file(wadFile.RomPath, wadFile.RomData.Length, _loadCallback);
						if (!loadWadResult) throw new Exception($"Could not load WAD file: '{wadFile.RomPath}'");
					}

					var initSettings = _syncSettings.GetNativeSettings(lp.Game);
					var initResult = Core.dsda_init(ref initSettings);
					if (!initResult) throw new Exception($"{nameof(Core.dsda_init)}() failed");

					int fps = 35;
					VsyncNumerator = fps;
					VsyncDenominator = 1;

					RomDetails = $"{lp.Game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(_wadFiles[0].RomData)}\r\n{MD5Checksum.ComputePrefixedHex(_wadFiles[0].RomData)}";

					_elf.Seal();
				}

				// pull the default video size from the core
				UpdateVideo();

				// Registering memory domains
				SetupMemoryDomains();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		// Remembering mouse position
		private const int MOUSE_NO_INPUT = -65535;
		private int _player1LastMouseRunningValue = MOUSE_NO_INPUT;
		private int _player1LastMouseTurningValue = MOUSE_NO_INPUT;
		private int _player2LastMouseRunningValue = MOUSE_NO_INPUT;
		private int _player2LastMouseTurningValue = MOUSE_NO_INPUT;
		private int _player3LastMouseRunningValue = MOUSE_NO_INPUT;
		private int _player3LastMouseTurningValue = MOUSE_NO_INPUT;
		private int _player4LastMouseRunningValue = MOUSE_NO_INPUT;
		private int _player4LastMouseTurningValue = MOUSE_NO_INPUT;

		// IRegionable
		public DisplayType Region { get; }

		// IRomInfo
		public string RomDetails { get; }

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly CInterface.load_archive_cb _loadCallback;

		private readonly string _dsdaWadFileName = "dsda-doom.wad";
		private readonly byte[] _dsdaWadFileData;
		private List<IRomAsset> _wadFiles;
		
		private readonly CInterface Core;
		private readonly WaterboxHost _elf;

		private readonly DoomControllerDeck _controllerDeck;

		/// <summary>
		/// core callback for file loading
		/// </summary>
		/// <param name="filename">string identifying file to be loaded</param>
		/// <param name="buffer">buffer to load file to</param>
		/// <param name="maxsize">maximum length buffer can hold</param>
		/// <returns>actual size loaded, or 0 on failure</returns>
		private int LoadCallback(string filename, IntPtr buffer, int maxsize)
		{
			byte[] srcdata = null;

			if (buffer == IntPtr.Zero)
			{
				Console.WriteLine("Couldn't satisfy firmware request {0} because buffer == NULL", filename);
				return 0;
			}

			if (filename == _dsdaWadFileName)
			{
				if (_dsdaWadFileData == null)
				{
					Console.WriteLine("Could not read from 'dsda-doom.wad'. File must be missing from the Resources folder.");
					return 0;
				}
				srcdata = _dsdaWadFileData;
			}

			foreach (var wadFile in _wadFiles)
			{
				if (filename == wadFile.RomPath)
				{
					if (wadFile.FileData == null)
					{
						Console.WriteLine("Could not read from WAD file '{0}'", filename);
						return 0;
					}
					srcdata = wadFile.FileData;
				}
			}

			if (srcdata != null)
			{
				if (srcdata.Length > maxsize)
				{
					Console.WriteLine("Couldn't satisfy firmware request {0} because {1} > {2}", filename, srcdata.Length, maxsize);
					return 0;
				}
				else
				{
					Console.WriteLine("Copying Data from " + srcdata + " to " + buffer + " Size: " + srcdata.Length);
					Marshal.Copy(srcdata, 0, buffer, srcdata.Length);
					Console.WriteLine("Firmware request {0} satisfied at size {1}", filename, srcdata.Length);
					return srcdata.Length;
				}
			}
			else
			{
				throw new InvalidOperationException($"Unknown error processing file '{filename}'");
			}
		}
	}
}

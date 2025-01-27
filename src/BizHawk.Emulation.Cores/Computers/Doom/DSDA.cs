﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	[PortedCore(
		name: CoreNames.DSDA,
		author: "DSDA Team",
		portedVersion: "0.28.2 (fe0dfa0)",
		portedUrl: "https://github.com/kraflab/dsda-doom",
		isReleased: true)]
	public partial class DSDA : WaterboxCore
	{
		private static readonly Configuration ConfigPAL = new Configuration
		{
			SystemId = VSystemID.Raw.Amiga,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibDSDA.PAL_WIDTH,
			DefaultHeight = LibDSDA.PAL_HEIGHT,
			MaxWidth = LibDSDA.PAL_WIDTH,
			MaxHeight = LibDSDA.PAL_HEIGHT,
			DefaultFpsNumerator = LibDSDA.VIDEO_NUMERATOR_PAL,
			DefaultFpsDenominator = LibDSDA.VIDEO_DENOMINATOR_PAL
		};

		private static readonly Configuration ConfigNTSC = new Configuration
		{
			SystemId = VSystemID.Raw.Amiga,
			MaxSamples = 8 * 1024,
			DefaultWidth = LibDSDA.NTSC_WIDTH,
			DefaultHeight = LibDSDA.NTSC_HEIGHT,
			// games never switch region, and video dumping won't be happy, but amiga can still do it
			MaxWidth = LibDSDA.PAL_WIDTH,
			MaxHeight = LibDSDA.PAL_HEIGHT,
			DefaultFpsNumerator = LibDSDA.VIDEO_NUMERATOR_NTSC,
			DefaultFpsDenominator = LibDSDA.VIDEO_DENOMINATOR_NTSC
		};
		
		private readonly LibWaterboxCore.EmptyCallback _ledCallback;
		private readonly List<IRomAsset> _roms;
		private const int _messageDuration = 4;
		private const int _driveNullOrEmpty = -1;
		private int[] _driveSlots;
		private List<string> _args;
		private int _currentDrive;
		private int _currentSlot;
		private bool _ejectPressed;
		private bool _insertPressed;
		private bool _nextSlotPressed;
		private bool _nextDrivePressed;
		private int _correctedWidth;
		private string _chipsetCompatible = "";
		private string GetFullName(IRomAsset rom) => rom.Game.Name + rom.Extension;

		public override int VirtualWidth => _correctedWidth;

		private void LEDCallback()
		{
		}

		[CoreConstructor(VSystemID.Raw.Doom)]
		public DSDA(CoreLoadParameters<object, DSDASyncSettings> lp)
			: base(lp.Comm, lp.SyncSettings?.Region is VideoStandard.NTSC ? ConfigNTSC : ConfigPAL)
		{
			_roms = lp.Roms;
			_syncSettings = lp.SyncSettings ?? new();
			_syncSettings.FloppyDrives = Math.Min(LibDSDA.MAX_FLOPPIES, _syncSettings.FloppyDrives);
			DeterministicEmulation = lp.DeterministicEmulationRequested || _syncSettings.FloppySpeed is FloppySpeed._100;
			var filesToRemove = new List<string>();

			_ports = [
				_syncSettings.ControllerPort1,
				_syncSettings.ControllerPort2
			];
			_driveSlots = Enumerable.Repeat(_driveNullOrEmpty, LibDSDA.MAX_FLOPPIES).ToArray();

			UpdateVideoStandard(true);
			CreateArguments(_syncSettings);
			ControllerDefinition = CreateControllerDefinition(_syncSettings);
			_ledCallback = LEDCallback;

			var uae = PreInit<LibDSDA>(new WaterboxOptions
			{
				Filename = "dsda.wbx",
				SbrkHeapSizeKB = 1024,
				SealedHeapSizeKB = 512,
				InvisibleHeapSizeKB = 512,
				PlainHeapSizeKB = 512,
				MmapHeapSizeKB = 20 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			}, new Delegate[] { _ledCallback });

			for (var index = 0; index < lp.Roms.Count; index++)
			{
				var rom = lp.Roms[index];
				_exe.AddReadonlyFile(rom.FileData, FileNames.FD + index);
				if (index < _syncSettings.FloppyDrives)
				{
					_driveSlots[index] = index;
					AppendSetting($"floppy{index}={FileNames.FD}{index}");
					AppendSetting($"floppy{index}type={(int) DriveType.DRV_35_DD}");
					AppendSetting("floppy_write_protect=true");
				}
			}

			var (kickstartData, kickstartInfo) = CoreComm.CoreFileProvider.GetFirmwareWithGameInfoOrThrow(
				new(VSystemID.Raw.Amiga, _chipsetCompatible),
				"Firmware files are required!");
			_exe.AddReadonlyFile(kickstartData, kickstartInfo.Name);
			filesToRemove.Add(kickstartInfo.Name);
			_args.AddRange(
			[
				"-r", kickstartInfo.Name
			]);

			Console.WriteLine();
			Console.WriteLine(string.Join(" ", _args));
			Console.WriteLine();

			if (!uae.Init(_args.Count, _args.ToArray()))
				throw new InvalidOperationException("Core rejected the rom!");

			foreach (var f in filesToRemove)
			{
				_exe.RemoveReadonlyFile(f);
			}

			PostInit();

			uae.SetLEDCallback(_syncSettings.FloppyDrives > 0 ? _ledCallback : null);
		}

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			var fi = new LibDSDA.FrameInfo
			{
				Port1 = new LibDSDA.ControllerState
				{
					Type = _ports[0],
					Buttons = 0
				},
				Port2 = new LibDSDA.ControllerState
				{
					Type = _ports[1],
					Buttons = 0
				},
				Action = LibDSDA.DriveAction.None
			};

			for (int port = 1; port <= 2; port++)
			{
				ref var currentPort = ref (port is 1 ? ref fi.Port1 : ref fi.Port2);

				switch (_ports[port - 1])
				{
					case LibDSDA.ControllerType.Joystick:
						{
							foreach (var (name, button) in _joystickMap)
							{
								if (controller.IsPressed($"P{port} {Inputs.Joystick} {name}"))
								{
									currentPort.Buttons |= button;
								}
							}
							break;
						}
					case LibDSDA.ControllerType.CD32_pad:
						{
							foreach (var (name, button) in _cd32padMap)
							{
								if (controller.IsPressed($"P{port} {Inputs.Cd32Pad} {name}"))
								{
									currentPort.Buttons |= button;
								}
							}
							break;
						}
					case LibDSDA.ControllerType.Mouse:
						{
							if (controller.IsPressed($"P{port} {Inputs.MouseLeftButton}"))
							{
								currentPort.Buttons |= LibDSDA.AllButtons.Button_1;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseRightButton}"))
							{
								currentPort.Buttons |= LibDSDA.AllButtons.Button_2;
							}

							if (controller.IsPressed($"P{port} {Inputs.MouseMiddleButton}"))
							{
								currentPort.Buttons |= LibDSDA.AllButtons.Button_3;
							}

							currentPort.MouseX = controller.AxisValue($"P{port} {Inputs.MouseX}");
							currentPort.MouseY = controller.AxisValue($"P{port} {Inputs.MouseY}");
							break;
						}
				}
			}

			if (controller.IsPressed(Inputs.EjectDisk))
			{
				if (!_ejectPressed)
				{
					fi.Action = LibDSDA.DriveAction.EjectDisk;
					if (_driveSlots[_currentDrive] == _driveNullOrEmpty)
					{
						CoreComm.Notify($"Drive FD{_currentDrive} is already empty!", _messageDuration);
					}
					else
					{
						CoreComm.Notify($"Ejected drive FD{_currentDrive}: {GetFullName(_roms[_driveSlots[_currentDrive]])}", _messageDuration);
						_driveSlots[_currentDrive] = _driveNullOrEmpty;
					}
				}
			}
			else if (controller.IsPressed(Inputs.InsertDisk))
			{
				if (!_insertPressed)
				{
					fi.Action = LibDSDA.DriveAction.InsertDisk;
					unsafe
					{
						var str = FileNames.FD + _currentSlot;
						fixed (char* filename = str)
						{
							fixed (byte* buffer = fi.Name.Buffer)
							{
								Encoding.ASCII.GetBytes(filename, str.Length, buffer, LibDSDA.FILENAME_MAXLENGTH);
							}
						}
					}
					_driveSlots[_currentDrive] = _currentSlot;
					CoreComm.Notify($"Insterted drive FD{_currentDrive}: {GetFullName(_roms[_driveSlots[_currentDrive]])}", _messageDuration);
				}
			}

			if (controller.IsPressed(Inputs.NextSlot))
			{
				if (!_nextSlotPressed)
				{
					_currentSlot++;
					_currentSlot %= _roms.Count;
					var selectedFile = _roms[_currentSlot];
					CoreComm.Notify($"Selected slot {_currentSlot}: {GetFullName(selectedFile)}", _messageDuration);
				}
			}

			if (controller.IsPressed(Inputs.NextDrive))
			{
				if (!_nextDrivePressed)
				{
					_currentDrive++;
					_currentDrive %= _syncSettings.FloppyDrives;
					string name = "";
					if (_driveSlots[_currentDrive] == _driveNullOrEmpty)
					{
						name = "empty";
					}
					else
					{
						name = GetFullName(_roms[_driveSlots[_currentDrive]]);
					}
					CoreComm.Notify($"Selected drive FD{_currentDrive}: {name}", _messageDuration);
				}
			}

			_ejectPressed = controller.IsPressed(Inputs.EjectDisk);
			_insertPressed = controller.IsPressed(Inputs.InsertDisk);
			_nextSlotPressed = controller.IsPressed(Inputs.NextSlot);
			_nextDrivePressed = controller.IsPressed(Inputs.NextDrive);			
			fi.CurrentDrive = _currentDrive;

			foreach (var (name, key) in _keyboardMap)
			{
				if (controller.IsPressed(name))
				{
					unsafe
					{
						fi.Keys.Buffer[(int)key] = 1;
					}
				}
			}

			return fi;
		}

		protected override void FrameAdvancePost()
		{
			UpdateVideoStandard(false);
		}

		protected override void SaveStateBinaryInternal(BinaryWriter writer)
		{
			writer.Write(_ejectPressed);
			writer.Write(_insertPressed);
			writer.Write(_nextSlotPressed);
			writer.Write(_nextDrivePressed);
			writer.Write(_currentDrive);
			writer.Write(_currentSlot);
			writer.Write(_driveSlots[0]);
			writer.Write(_driveSlots[1]);
			writer.Write(_driveSlots[2]);
			writer.Write(_driveSlots[3]);
		}

		protected override void LoadStateBinaryInternal(BinaryReader reader)
		{
			_ejectPressed = reader.ReadBoolean();
			_insertPressed = reader.ReadBoolean();
			_nextSlotPressed = reader.ReadBoolean();
			_nextDrivePressed = reader.ReadBoolean();
			_currentDrive = reader.ReadInt32();
			_currentSlot = reader.ReadInt32();
			_driveSlots[0] = reader.ReadInt32();
			_driveSlots[1] = reader.ReadInt32();
			_driveSlots[2] = reader.ReadInt32();
			_driveSlots[3] = reader.ReadInt32();
		}

		private void UpdateVideoStandard(bool initial)
		{
			var ntsc = initial
				? _syncSettings.Region is VideoStandard.NTSC
				: BufferHeight == LibDSDA.NTSC_HEIGHT;

			if (ntsc)
			{
				_correctedWidth = LibDSDA.PAL_WIDTH * 6 / 7;
				VsyncNumerator = LibDSDA.VIDEO_NUMERATOR_NTSC;
				VsyncDenominator = LibDSDA.VIDEO_DENOMINATOR_NTSC;
			}
			else
			{
				_correctedWidth = LibDSDA.PAL_WIDTH;
				VsyncNumerator = LibDSDA.VIDEO_NUMERATOR_PAL;
				VsyncDenominator = LibDSDA.VIDEO_DENOMINATOR_PAL;
			}
		}

		private static class FileNames
		{
			public const string FD = "FloppyDisk";
			public const string CD = "CompactDisk";
			public const string HD = "HardDrive";
		}
	}
}
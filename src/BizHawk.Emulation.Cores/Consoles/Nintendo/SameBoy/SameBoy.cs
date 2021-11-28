﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C libsameboy
	/// </summary>
	[PortedCore(CoreNames.Sameboy, "LIJI32", "0.14.7", "https://github.com/LIJI32/SameBoy", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class Sameboy : ICycleTiming, IInputPollable, ILinkable, IRomInfo, IBoardInfo, IGameboyCommon
	{
		private readonly BasicServiceProvider _serviceProvider;

		private readonly Gameboy.GBDisassembler _disassembler;

		private IntPtr SameboyState { get; set; } = IntPtr.Zero;

		public bool IsCgb { get; set; }

		public bool IsCGBMode() => IsCgb;

		private readonly LibSameboy.SampleCallback _samplecb;
		private readonly LibSameboy.InputCallback _inputcb;

		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		public Sameboy(CoreComm comm, GameInfo game, byte[] file, SameboySyncSettings syncSettings, bool deterministic)
		{
			_serviceProvider = new BasicServiceProvider(this);

			_syncSettings = syncSettings ?? new SameboySyncSettings();

			LibSameboy.LoadFlags flags = _syncSettings.ConsoleMode switch
			{
				SameboySyncSettings.ConsoleModeType.GB => LibSameboy.LoadFlags.IS_DMG,
				SameboySyncSettings.ConsoleModeType.GBC => LibSameboy.LoadFlags.IS_CGB,
				SameboySyncSettings.ConsoleModeType.GBA => LibSameboy.LoadFlags.IS_CGB | LibSameboy.LoadFlags.IS_AGB,
				_ => game.System == VSystemID.Raw.GBC ? LibSameboy.LoadFlags.IS_CGB : LibSameboy.LoadFlags.IS_DMG
			};

			IsCgb = (flags & LibSameboy.LoadFlags.IS_CGB) == LibSameboy.LoadFlags.IS_CGB;

			byte[] bios;
			if (_syncSettings.EnableBIOS)
			{
				FirmwareID fwid = new(
					IsCgb ? "GBC" : "GB",
					_syncSettings.ConsoleMode is SameboySyncSettings.ConsoleModeType.GBA
					? "AGB"
					: "World");
				bios = comm.CoreFileProvider.GetFirmwareOrThrow(fwid, "BIOS Not Found, Cannot Load.  Change SyncSettings to run without BIOS.");
			}
			else
			{
				bios = Util.DecompressGzipFile(new MemoryStream(IsCgb
					? _syncSettings.ConsoleMode is SameboySyncSettings.ConsoleModeType.GBA ? Resources.SameboyAgbBoot.Value : Resources.SameboyCgbBoot.Value
					: Resources.SameboyDmgBoot.Value));
			}

			DeterministicEmulation = false;

			if (!_syncSettings.UseRealTime || deterministic)
			{
				flags |= LibSameboy.LoadFlags.RTC_ACCURATE;
				DeterministicEmulation = true;
			}

			SameboyState = LibSameboy.sameboy_create(file, file.Length, bios, bios.Length, flags);

			InitMemoryDomains();
			InitMemoryCallbacks();

			_samplecb = QueueSample;
			LibSameboy.sameboy_setsamplecallback(SameboyState, _samplecb);
			_inputcb = InputCallback;
			LibSameboy.sameboy_setinputcallback(SameboyState, _inputcb);
			_tracecb = MakeTrace;
			LibSameboy.sameboy_settracecallback(SameboyState, null);

			LibSameboy.sameboy_setscanlinecallback(SameboyState, null, 0);
			LibSameboy.sameboy_setprintercallback(SameboyState, null);

			const string TRACE_HEADER = "SM83: PC, opcode, registers (A, F, B, C, D, E, H, L, SP, LY, CY)";
			Tracer = new TraceBuffer(TRACE_HEADER);
			_serviceProvider.Register<ITraceable>(Tracer);

			_disassembler = new Gameboy.GBDisassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_stateBuf = new byte[LibSameboy.sameboy_statelen(SameboyState)];

			RomDetails = $"{game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(file)}\r\n{MD5Checksum.ComputePrefixedHex(file)}\r\n";
			BoardName = MapperName(file);

			CycleCount = 0;
		}

		public double ClockRate => 2097152;

		public long CycleCount
		{
			get => LibSameboy.sameboy_getcyclecount(SameboyState);
			private set => LibSameboy.sameboy_setcyclecount(SameboyState, value);
		}

		public int LagCount { get; set; } = 0;

		public bool IsLagFrame { get; set; } = false;

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private void InputCallback()
		{
			IsLagFrame = false;
			_inputCallbacks.Call();
		}

		public bool LinkConnected
		{
			get => _printercb != null;
			set { return; }
		}

		public string RomDetails { get; }

		private static string MapperName(byte[] romdata)
		{
			return (romdata[0x147]) switch
			{
				0x00 => "Plain ROM",
				0x01 => "MBC1 ROM",
				0x02 => "MBC1 ROM+RAM",
				0x03 => "MBC1 ROM+RAM+BATTERY",
				0x05 => "MBC2 ROM",
				0x06 => "MBC2 ROM+BATTERY",
				0x08 => "Plain ROM+RAM",
				0x09 => "Plain ROM+RAM+BATTERY",
				0x0F => "MBC3 ROM+TIMER+BATTERY",
				0x10 => "MBC3 ROM+TIMER+RAM+BATTERY",
				0x11 => "MBC3 ROM",
				0x12 => "MBC3 ROM+RAM",
				0x13 => "MBC3 ROM+RAM+BATTERY",
				0x19 => "MBC5 ROM",
				0x1A => "MBC5 ROM+RAM",
				0x1B => "MBC5 ROM+RAM+BATTERY",
				0x1C => "MBC5 ROM+RUMBLE",
				0x1D => "MBC5 ROM+RUMBLE+RAM",
				0x1E => "MBC5 ROM+RUMBLE+RAM+BATTERY",
				0x22 => "MBC7 ROM+ACCEL+EEPROM",
				0xFC => "Pocket Camera ROM+RAM+BATTERY",
				0xFE => "HuC3 ROM+RAM+BATTERY",
				0xFF => "HuC1 ROM+RAM+BATTERY",
				_ => "UNKNOWN",
			};
		}

		public string BoardName { get; }

		// getmemoryarea will return the raw palette buffer, but we want the rgb32 palette, so convert it
		private unsafe uint[] SynthesizeFrontendPal(IntPtr _pal)
		{
			var buf = new uint[32];
			var pal = (short*)_pal;
			for (int i = 0; i < 32; i++)
			{
				byte r = (byte)(pal[i]       & 0x1F);
				byte g = (byte)(pal[i] >> 5  & 0x1F);
				byte b = (byte)(pal[i] >> 10 & 0x1F);
				buf[i] = (uint)((0xFF << 24) | (r << 19) | (g << 11) | (b << 3));
			}
			return buf;
		}

		public IGPUMemoryAreas LockGPU()
		{
			var _vram = IntPtr.Zero;
			var _bgpal = IntPtr.Zero;
			var _sppal = IntPtr.Zero;
			var _oam = IntPtr.Zero;
			int unused = 0;
			if (!LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.VRAM, ref _vram, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.BGP, ref _bgpal, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.OBP, ref _sppal, ref unused)
				|| !LibSameboy.sameboy_getmemoryarea(SameboyState, LibSameboy.MemoryAreas.OAM, ref _oam, ref unused))
			{
				throw new InvalidOperationException("Unexpected error in sameboy_getmemoryarea");
			}
			return new GPUMemoryAreas(_vram, _oam, SynthesizeFrontendPal(_sppal), SynthesizeFrontendPal(_bgpal));
		}

		private class GPUMemoryAreas : IGPUMemoryAreas
		{
			public IntPtr Vram { get; }

			public IntPtr Oam { get; }

			public IntPtr Sppal { get; }

			public IntPtr Bgpal { get; }

			private readonly List<GCHandle> _handles = new List<GCHandle>();

			public GPUMemoryAreas(IntPtr vram, IntPtr oam, uint[] sppal, uint[] bgpal)
			{
				Vram = vram;
				Oam = oam;
				Sppal = AddHandle(sppal);
				Bgpal = AddHandle(bgpal);
			}

			private IntPtr AddHandle(object target)
			{
				var handle = GCHandle.Alloc(target, GCHandleType.Pinned);
				_handles.Add(handle);
				return handle.AddrOfPinnedObject();
			}

			public void Dispose()
			{
				foreach (var h in _handles)
					h.Free();
				_handles.Clear();
			}
		}

		private ScanlineCallback _scanlinecb;
		private int _scanlinecbline;

		public void SetScanlineCallback(ScanlineCallback callback, int line)
		{
			_scanlinecb = callback;
			_scanlinecbline = line;

			LibSameboy.sameboy_setscanlinecallback(SameboyState, _scanlinecbline >= 0 ? callback : null, line);

			if (_scanlinecbline == -2)
			{
				_scanlinecb(LibSameboy.sameboy_cpuread(SameboyState, 0xFF40));
			}
		}

		private PrinterCallback _printercb;

		public void SetPrinterCallback(PrinterCallback callback)
		{
			_printercb = callback;
			LibSameboy.sameboy_setprintercallback(SameboyState, _printercb);
		}
	}
}

﻿using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	public partial class Sameboy
	{
		private readonly List<MemoryDomain> _memoryDomains = new List<MemoryDomain>();

		private IMemoryDomains MemoryDomains { get; set; }

		private void CreateMemoryDomain(LibSameboy.MemoryAreas which, string name)
		{
			IntPtr data = IntPtr.Zero;
			int length = 0;

			if (!LibSameboy.sameboy_getmemoryarea(SameboyState, which, ref data, ref length))
			{
				throw new Exception($"{nameof(LibSameboy.sameboy_getmemoryarea)}() failed!");
			}

			// if length == 0, it's an empty block; (usually rambank on some carts); that's ok
			if (data != IntPtr.Zero && length > 0)
			{
				_memoryDomains.Add(new MemoryDomainIntPtr(name, MemoryDomain.Endian.Little, data, length, true, 1));
			}
		}

		private void InitMemoryDomains()
		{
			CreateMemoryDomain(LibSameboy.MemoryAreas.ROM, "ROM");
			CreateMemoryDomain(LibSameboy.MemoryAreas.RAM, "WRAM");
			CreateMemoryDomain(LibSameboy.MemoryAreas.CART_RAM, "CartRAM");
			CreateMemoryDomain(LibSameboy.MemoryAreas.VRAM, "VRAM");
			CreateMemoryDomain(LibSameboy.MemoryAreas.HRAM, "HRAM");
			CreateMemoryDomain(LibSameboy.MemoryAreas.IO, "MMIO");
			CreateMemoryDomain(LibSameboy.MemoryAreas.BOOTROM, "BIOS");
			CreateMemoryDomain(LibSameboy.MemoryAreas.OAM, "OAM");
			CreateMemoryDomain(LibSameboy.MemoryAreas.BGP, "BGP");
			CreateMemoryDomain(LibSameboy.MemoryAreas.OBP, "OBP");
			CreateMemoryDomain(LibSameboy.MemoryAreas.IE, "IE");

			// also add a special memory domain for the system bus, where calls get sent directly to the core each time
			_memoryDomains.Add(new MemoryDomainDelegate("System Bus", 65536, MemoryDomain.Endian.Little,
				delegate(long addr)
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					return LibSameboy.sameboy_cpuread(SameboyState, (ushort)addr);
				},
				delegate(long addr, byte val)
				{
					if (addr < 0 || addr >= 65536)
					{
						throw new ArgumentOutOfRangeException();
					}

					LibSameboy.sameboy_cpuwrite(SameboyState, (ushort)addr, val);
				}, 1));

			MemoryDomains = new MemoryDomainList(_memoryDomains);
			_serviceProvider.Register(MemoryDomains);
		}
	}
}

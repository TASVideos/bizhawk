﻿using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII
	{
		private void SetupMemoryDomains()
		{
			List<MemoryDomain> domains = new();

			MemoryDomainDelegate mainRamDomain = new("Main RAM", 0x10000, MemoryDomain.Endian.Little,
				addr =>
				{
					if (addr is < 0 or > 0x10000) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return _machine.Memory.PeekMainRam((int)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0x10000) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					_machine.Memory.PokeMainRam((int)addr, value);
				}, 1);

			domains.Add(mainRamDomain);

			MemoryDomainDelegate auxRamDomain = new("Auxiliary RAM", 0x10000, MemoryDomain.Endian.Little,
				addr =>
				{
					if (addr is < 0 or > 0x10000) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return _machine.Memory.PeekAuxRam((int)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0x10000) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					_machine.Memory.PokeAuxRam((int)addr, value);
				}, 1);

			domains.Add(auxRamDomain);

			MemoryDomainDelegate systemBusDomain = new("System Bus", 0x10000, MemoryDomain.Endian.Little,
				addr =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					return (byte)_machine.Memory.Peek((int)addr);
				},
				(addr, value) =>
				{
					if (addr is < 0 or > 0xFFFF) throw new ArgumentOutOfRangeException(paramName: nameof(addr), addr, message: "address out of range");
					_machine.Memory.Write((int)addr, value);
				}, 1);

			domains.Add(systemBusDomain);

			_memoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}

		private IMemoryDomains _memoryDomains;
	}
}

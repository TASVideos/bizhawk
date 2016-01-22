﻿using System;
using System.Collections.Generic;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public partial class C64
	{
		private IMemoryDomains _memoryDomains;

		private void SetupMemoryDomains()
		{
			// chips must be initialized before this code runs!
		    var domains = new List<MemoryDomain>
		    {
                C64MemoryDomainFactory.Create("System Bus", 0x10000, _board.Cpu.Peek, _board.Cpu.Poke),
                C64MemoryDomainFactory.Create("RAM", 0x10000, _board.Ram.Peek, _board.Ram.Poke),
                C64MemoryDomainFactory.Create("CIA0", 0x10, _board.Cia0.Peek, _board.Cia0.Poke),
                C64MemoryDomainFactory.Create("CIA1", 0x10, _board.Cia1.Peek, _board.Cia1.Poke),
                C64MemoryDomainFactory.Create("VIC", 0x40, _board.Vic.Peek, _board.Vic.Poke),
                C64MemoryDomainFactory.Create("SID", 0x20, _board.Sid.Peek, _board.Sid.Poke)
            };
		    //domains.Add(new MemoryDomain("1541 Bus", 0x10000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.Peek), new Action<int, byte>(disk.Poke)));
			//domains.Add(new MemoryDomain("1541 VIA0", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia0), new Action<int, byte>(disk.PokeVia0)));
			//domains.Add(new MemoryDomain("1541 VIA1", 0x10, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekVia1), new Action<int, byte>(disk.PokeVia1)));
			//domains.Add(new MemoryDomain("1541 RAM", 0x1000, MemoryDomain.Endian.Little, new Func<int, byte>(disk.PeekRam), new Action<int, byte>(disk.PokeRam)));
			_memoryDomains = new MemoryDomainList(domains);
			((BasicServiceProvider) ServiceProvider).Register(_memoryDomains);
		}

        private static class C64MemoryDomainFactory
        {
            public static MemoryDomain Create(string name, int size, Func<int, int> peekByte, Action<int, int> pokeByte)
            {
                return new MemoryDomain(name, size, MemoryDomain.Endian.Little, addr => unchecked((byte)peekByte((int)addr)), (addr, val) => pokeByte(unchecked((int)addr), val));
            }
        }
    }
}

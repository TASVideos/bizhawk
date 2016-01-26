﻿namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	// vic pal
	public static class Chip6569
	{
	    private static readonly int Cycles = 63;
	    private static readonly int ScanWidth = Cycles * 8;
	    private static readonly int Lines = 312;
	    private static readonly int VblankStart = 0x12C % Lines;
	    private static readonly int VblankEnd = 0x00F % Lines;
	    private static readonly int HblankOffset = 20;
	    private static readonly int HblankStart = (0x17C + HblankOffset) % ScanWidth;
	    private static readonly int HblankEnd = (0x1E0 + HblankOffset) % ScanWidth;

	    private static readonly int[] Timing = Vic.TimingBuilder_XRaster(0x194, 0x1F8, ScanWidth, -1, -1);
	    private static readonly int[] Fetch = Vic.TimingBuilder_Fetch(Timing, 0x164);
	    private static readonly int[] Ba = Vic.TimingBuilder_BA(Fetch);
	    private static readonly int[] Act = Vic.TimingBuilder_Act(Timing, 0x004, 0x14C, HblankStart, HblankEnd);

	    private static readonly int[][] Pipeline = {
				Timing,
				Fetch,
				Ba,
				Act
			};

		public static Vic Create()
		{
			return new Vic(
				Cycles, Lines,
				Pipeline,
				17734472 / 18,
				HblankStart, HblankEnd,
				VblankStart, VblankEnd
				);
		}
	}
}

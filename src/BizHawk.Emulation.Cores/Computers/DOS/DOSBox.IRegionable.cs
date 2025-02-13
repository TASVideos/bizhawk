using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.DOS
{
	public partial class DOSBox : IRegionable
	{
		public DisplayType Region => _syncSettings.Region is VideoStandard.NTSC
			? DisplayType.NTSC
			: DisplayType.PAL;
	}
}

using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA
	{
		private IMemoryDomains MemoryDomains;

		private void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>
			{
			};

			MemoryDomains = new MemoryDomainList(domains) { };
			((BasicServiceProvider)ServiceProvider).Register(MemoryDomains);
		}
	}
}

using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : ISoundProvider
	{
		private readonly short[] _samples = new short[6280];
		private int _nsamp;

		public bool CanProvideAsync => false;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			nsamp = _nsamp;
			samples = _samples;
			_nsamp = 0;
		}

		public void DiscardSamples()
			=> _nsamp = 0;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesAsync(short[] samples)
			=> throw new InvalidOperationException("Async mode is not supported.");

		private unsafe void UpdateAudio()
		{
			var src = IntPtr.Zero;
			Core.dsda_get_audio(ref _nsamp, ref src);

			if (src != IntPtr.Zero)
			{
				using (_elf.EnterExit())
				{
					Marshal.Copy(src, _samples, 0, _nsamp * 2);
				}
			}
			else
			{
				_nsamp = 0;
			}
		}
	}
}

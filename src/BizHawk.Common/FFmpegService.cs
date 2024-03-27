using System;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading.Tasks;
using BizHawk.Common.PathExtensions;

namespace BizHawk.Common
{
	public static class FFmpegService
	{
		private const string BIN_HOST_URI_LINUX_X64 = "https://github.com/TASEmulators/ffmpeg-binaries/raw/master/ffmpeg-4.4.1-static-linux-x64.7z";

		private const string BIN_HOST_URI_WIN_X64 = "https://github.com/TASEmulators/ffmpeg-binaries/raw/master/ffmpeg-4.4.1-static-windows-x64.7z";

		private const string VERSION = "ffmpeg version 4.4.1";

		public static string FFmpegPath => Path.Combine(PathUtils.DataDirectoryPath, "dll", OSTailoredCode.IsUnixHost ? "ffmpeg" : "ffmpeg.exe");

		public static readonly string Url = OSTailoredCode.IsUnixHost ? BIN_HOST_URI_LINUX_X64 : BIN_HOST_URI_WIN_X64;

		public class AudioQueryResult
		{
			public bool IsAudio;
		}

		private static string[] Escape(IEnumerable<string> args)
		{
			return args.Select(s => s.Contains(' ') ? $"\"{s}\"" : s).ToArray();
		}

		//note: accepts . or : in the stream stream/substream separator in the stream ID format, since that changed at some point in FFMPEG history
		//if someone has a better idea how to make the determination of whether an audio stream is available, I'm all ears
		private static readonly Regex rxHasAudio = new Regex(@"Stream \#(\d*(\.|\:)\d*)\: Audio", RegexOptions.Compiled);
		public static AudioQueryResult QueryAudio(string path)
		{
			var ret = new AudioQueryResult();
			string stdout = Run("-i", path).Text;
			ret.IsAudio = rxHasAudio.Matches(stdout).Count > 0;
			return ret;
		}

		/// <summary>
		/// queries whether this service is available. if ffmpeg is broken or missing, then you can handle it gracefully
		/// </summary>
		public static bool QueryServiceAvailable()
		{
			try
			{
				return Run("-version").Text.Contains(VERSION);
			}
			catch
			{
				return false;
			}
		}

		public struct RunResults
		{
			public string Text;
			public int ExitCode;
		}

		public static RunResults Run(params string[] args)
		{
			args = Escape(args);
			StringBuilder sbCmdline = new StringBuilder();
			for (int i = 0; i < args.Length; i++)
			{
				sbCmdline.Append(args[i]);
				if (i != args.Length - 1) sbCmdline.Append(' ');
			}

			ProcessStartInfo oInfo = new ProcessStartInfo(FFmpegPath, sbCmdline.ToString())
			{
				UseShellExecute = false,
				CreateNoWindow = true,
				RedirectStandardOutput = true,
				RedirectStandardError = true
			};

			Process proc = new Process();
			proc.StartInfo = oInfo;
			Mutex m = new Mutex();

			var outputBuilder = new StringBuilder();
			var outputCloseEvent = new TaskCompletionSource<bool>();
			var errorCloseEvent = new TaskCompletionSource<bool>();

			proc.OutputDataReceived += (s, e) =>
			{
				if (e.Data == null)
				{
					outputCloseEvent.SetResult(true);
				}
				else
				{
					m.WaitOne();
					outputBuilder.Append(e.Data);
					m.ReleaseMutex();
				}
			};

			proc.ErrorDataReceived += (s, e) =>
			{
				if (e.Data == null)
				{
					errorCloseEvent.SetResult(true);
				}
				else
				{
					m.WaitOne();
					outputBuilder.Append(e.Data);
					m.ReleaseMutex();
				}
			};

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();
			proc.WaitForExit();
			string resultText = "";
			m.WaitOne();
			resultText = outputBuilder.ToString();
			m.ReleaseMutex();

			return new RunResults
			{
				ExitCode = proc.ExitCode,
				Text = resultText
			};
		}

		/// <exception cref="InvalidOperationException">FFmpeg exited with non-zero exit code or produced no output</exception>
		public static byte[] DecodeAudio(string path)
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				var runResults = Run("-i", path, "-xerror", "-f", "wav", "-ar", "44100", "-ac", "2", "-acodec", "pcm_s16le", "-y", tempfile);
				if (runResults.ExitCode != 0)
					throw new InvalidOperationException($"Failure running ffmpeg for audio decode. here was its output:\r\n{runResults.Text}");
				byte[] ret = File.ReadAllBytes(tempfile);
				if (ret.Length == 0)
					throw new InvalidOperationException($"Failure running ffmpeg for audio decode. here was its output:\r\n{runResults.Text}");
				return ret;
			}
			finally
			{
				File.Delete(tempfile);
			}
		}
	}

}

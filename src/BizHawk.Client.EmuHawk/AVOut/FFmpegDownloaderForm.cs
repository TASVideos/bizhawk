using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Downloads FFmpeg
	/// </summary>
	public partial class FFmpegDownloaderForm : Form
	{
		public FFmpegDownloaderForm()
		{
			InitializeComponent();

			txtLocation.Text = FFmpegService.FFmpegPath;
			txtUrl.Text = FFmpegService.Url;

			if (OSTailoredCode.IsUnixHost) textBox1.Text = string.Join("\n", textBox1.Text.Split('\n').Take(3)) + "\n\n(Linux user: If installing manually, you can use a symlink.)";
		}

		private int pct = 0;
		private bool exiting = false;
		private bool succeeded = false;
		private bool failed = false;

		private void ThreadProc()
		{
			Download();
		}

		private void Download()
		{
			//the temp file is owned by this thread
			var fn = TempFileManager.GetTempFilename("ffmpeg_download", ".7z", false);

			try
			{
				DirectoryInfo parentDir = new(Path.GetDirectoryName(FFmpegService.FFmpegPath)!);
				if (!parentDir.Exists) parentDir.Create();
				using var fs = File.Create(FFmpegService.FFmpegPath); // check writable before bothering with the download
				using (var evt = new ManualResetEvent(false))
				{
					using (var client = new System.Net.WebClient())
					{
						System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
						client.DownloadFileAsync(new Uri(FFmpegService.Url), fn);
						client.DownloadProgressChanged += (object sender, System.Net.DownloadProgressChangedEventArgs e) =>
						{
							pct = e.ProgressPercentage;
						};
						client.DownloadFileCompleted += (object sender, System.ComponentModel.AsyncCompletedEventArgs e) =>
						{
							//we don't really need a status. we'll just try to unzip it when it's done
							evt.Set();
						};

						for (; ; )
						{
							if (evt.WaitOne(10))
								break;

							//if the gui thread ordered an exit, cancel the download and wait for it to acknowledge
							if (exiting)
							{
								client.CancelAsync();
								evt.WaitOne();
								break;
							}
						}
					}
				}
				
				//throw new Exception("test of download failure");

				//if we were ordered to exit, bail without wasting any more time
				if (exiting)
					return;

				//try acquiring file
				using (var hf = new HawkFile(fn))
				{
					using (var exe = OSTailoredCode.IsUnixHost ? hf.BindArchiveMember("ffmpeg") : hf.BindFirstOf(".exe"))
					{
						//last chance. exiting, don't dump the new ffmpeg file
						if (exiting)
							return;
						exe!.GetStream().CopyTo(fs);
						fs.Dispose();
						if (OSTailoredCode.IsUnixHost)
						{
							OSTailoredCode.ConstructSubshell("chmod", $"+x {FFmpegService.FFmpegPath}", checkStdout: false).Start();
							Thread.Sleep(50); // Linux I/O flush idk
						}
					}
				}

				//make sure it worked
				if (!FFmpegService.QueryServiceAvailable()) throw new Exception("download failed");

				succeeded = true;
			}
			catch (Exception e)
			{
				failed = true;
				Util.DebugWriteLine($"FFmpeg download failed with:\n{e}");
			}
			finally
			{
				try { File.Delete(fn); }
				catch { }
			}
		}

		private void btnDownload_Click(object sender, EventArgs e)
		{
			btnDownload.Text = "Downloading...";
			btnDownload.Enabled = false;
			failed = false;
			succeeded = false;
			pct = 0;
			var t = new Thread(ThreadProc);
			t.Start();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			Close();
		}

		protected override void OnClosed(EventArgs e)
		{
			//inform the worker thread that it needs to try terminating without doing anything else
			//(it will linger on in background for a bit til it can service this)
			exiting = true;
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			//if it's done, close the window. the user will be smart enough to reopen it
			if (succeeded)
				Close();
			if (failed)
			{
				failed = false;
				pct = 0;
				btnDownload.Text = "FAILED - Download Again";
				btnDownload.Enabled = true;
			}
			progressBar1.Value = pct;
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(FFmpegService.Url);
		}
	}
}

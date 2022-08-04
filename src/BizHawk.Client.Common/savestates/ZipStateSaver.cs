﻿using System;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public class ZipStateSaver : IDisposable
	{
		private readonly IZipWriter _zip;
		private bool _isDisposed;

		private static void WriteZipVersion(Stream s)
		{
			var sw = new StreamWriter(s);
			sw.WriteLine("2"); // version 1.0.2
			sw.Flush();
		}

		private static void WriteEmuVersion(Stream s)
		{
			var sw = new StreamWriter(s);
			sw.WriteLine(VersionInfo.GetEmuVersion());
			sw.Flush();
		}

		public ZipStateSaver(string path, int compressionLevel)
		{
			_zip = new FrameworkZipWriter(path, compressionLevel);

			// we put these in every zip, so we know where they came from
			PutLump(BinaryStateLump.ZipVersion, WriteZipVersion, false);
			PutLump(BinaryStateLump.BizVersion, WriteEmuVersion, false);
		}

		public void PutLump(BinaryStateLump lump, Action<Stream> callback, bool zstdCompress = true)
		{
			_zip.WriteItem(lump.WriteName, callback, zstdCompress);
		}

		public void PutLump(BinaryStateLump lump, Action<BinaryWriter> callback)
		{
			PutLump(lump, s =>
			{
				var bw = new BinaryWriter(s);
				callback(bw);
				bw.Flush();
			});
		}

		public void PutLump(BinaryStateLump lump, Action<TextWriter> callback)
		{
			// don't zstd compress text, as it's annoying for users
			PutLump(lump, s =>
			{
				TextWriter tw = new StreamWriter(s);
				callback(tw);
				tw.Flush();
			}, false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_isDisposed)
			{
				_isDisposed = true;

				if (disposing)
				{
					_zip.Dispose();
				}
			}
		}
	}
}

﻿using System;
using System.IO;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public sealed class Zstd : IDisposable
	{
		private sealed class ZstdCompressionStreamContext : IDisposable
		{
			public readonly IntPtr Zcs;

			public readonly byte[] InputBuffer;
			public readonly GCHandle InputHandle;
			public LibZstd.StreamBuffer Input;

			public readonly byte[] OutputBuffer;
			public readonly GCHandle OutputHandle;
			public LibZstd.StreamBuffer Output;

			// TODO: tweak these sizes

			// 4 MB input buffer
			public const int INPUT_BUFFER_SIZE = 1024 * 1024 * 4;
			// 1 MB output buffer
			public const int OUTPUT_BUFFER_SIZE = 1024 * 1024 * 1;

			public bool InUse;

			public ZstdCompressionStreamContext()
			{
				Zcs = _lib.ZSTD_createCStream();

				InputBuffer = new byte[INPUT_BUFFER_SIZE];
				InputHandle = GCHandle.Alloc(InputBuffer, GCHandleType.Pinned);

				Input = new()
				{
					Ptr = InputHandle.AddrOfPinnedObject(),
					Size = 0,
					Pos = 0,
				};

				OutputBuffer = new byte[OUTPUT_BUFFER_SIZE];
				OutputHandle = GCHandle.Alloc(OutputBuffer, GCHandleType.Pinned);

				Output = new()
				{
					Ptr = OutputHandle.AddrOfPinnedObject(),
					Size = OUTPUT_BUFFER_SIZE,
					Pos = 0,
				};

				InUse = false;
			}

			private bool _disposed = false;

			public void Dispose()
			{
				if (!_disposed)
				{
					_lib.ZSTD_freeCStream(Zcs);
					InputHandle.Free();
					OutputHandle.Free();
					_disposed = true;
				}
			}

			public void InitContext(int compressionLevel)
			{
				if (InUse)
				{
					throw new InvalidOperationException("Cannot init context still in use!");
				}

				_lib.ZSTD_initCStream(Zcs, compressionLevel);
				Input.Size = Input.Pos = Output.Pos = 0;
				InUse = true;
			}
		}

		private sealed class ZstdCompressionStream : Stream
		{
			private readonly Stream _baseStream;
			private readonly ZstdCompressionStreamContext _ctx;

			public ZstdCompressionStream(Stream baseStream, ZstdCompressionStreamContext ctx)
			{
				_baseStream = baseStream;
				_ctx = ctx;
			}

			private bool _disposed = false;

			protected override void Dispose(bool disposing)
			{
				if (disposing && !_disposed)
				{
					Flush();
					while (true)
					{
						ulong n = _lib.ZSTD_endStream(_ctx.Zcs, ref _ctx.Output);
						CheckError(n);
						InternalFlush();
						if (n == 0)
						{
							break;
						}
					}
					_ctx.InUse = false;
					_disposed = true;
				}

				base.Dispose(disposing);
			}

			public override bool CanRead
				=> false;

			public override bool CanSeek
				=> false;

			public override bool CanWrite
				=> true;

			public override long Length
				=> throw new NotImplementedException();

			public override long Position 
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			private void InternalFlush()
			{
				_baseStream.Write(_ctx.OutputBuffer, 0, (int)_ctx.Output.Pos);
				_ctx.Output.Pos = 0;
			}

			public override void Flush()
			{
				while (_ctx.Input.Pos < _ctx.Input.Size)
				{
					CheckError(_lib.ZSTD_compressStream(_ctx.Zcs, ref _ctx.Output, ref _ctx.Input));
					while (true)
					{
						if (_ctx.Output.Pos == ZstdCompressionStreamContext.OUTPUT_BUFFER_SIZE)
						{
							InternalFlush();
						}

						ulong n = _lib.ZSTD_flushStream(_ctx.Zcs, ref _ctx.Output);
						CheckError(n);
						if (n == 0)
						{
							InternalFlush();
							break;
						}
					}
				}

				_ctx.Input.Pos = _ctx.Input.Size = 0;
			}

			public override int Read(byte[] buffer, int offset, int count)
				=> throw new NotImplementedException();

			public override long Seek(long offset, SeekOrigin origin)
				=> throw new NotImplementedException();

			public override void SetLength(long value)
				=> throw new NotImplementedException();

			public override void Write(byte[] buffer, int offset, int count)
			{
				while (count > 0)
				{
					if (_ctx.Input.Size == ZstdCompressionStreamContext.INPUT_BUFFER_SIZE)
					{
						Flush();
					}

					int n = Math.Min(count, (int)(ZstdCompressionStreamContext.INPUT_BUFFER_SIZE - _ctx.Input.Size));
					Marshal.Copy(buffer, offset, _ctx.Input.Ptr + (int)_ctx.Input.Size, n);
					offset += n;
					_ctx.Input.Size += (ulong)n;
					count -= n;
				}
			}
		}

		private sealed class ZstdDecompressionStreamContext : IDisposable
		{
			public readonly IntPtr Zds;

			public readonly byte[] InputBuffer;
			public readonly GCHandle InputHandle;
			public LibZstd.StreamBuffer Input;

			public readonly byte[] OutputBuffer;
			public readonly GCHandle OutputHandle;
			public LibZstd.StreamBuffer Output;

			// TODO: tweak these sizes

			// 1 MB input buffer
			public const int INPUT_BUFFER_SIZE = 1024 * 1024 * 1;
			// 4 MB output buffer
			public const int OUTPUT_BUFFER_SIZE = 1024 * 1024 * 4;

			public bool InUse;

			public ZstdDecompressionStreamContext()
			{
				Zds = _lib.ZSTD_createDStream();

				InputBuffer = new byte[INPUT_BUFFER_SIZE];
				InputHandle = GCHandle.Alloc(InputBuffer, GCHandleType.Pinned);

				Input = new()
				{
					Ptr = InputHandle.AddrOfPinnedObject(),
					Size = 0,
					Pos = 0,
				};

				OutputBuffer = new byte[OUTPUT_BUFFER_SIZE];
				OutputHandle = GCHandle.Alloc(OutputBuffer, GCHandleType.Pinned);

				Output = new()
				{
					Ptr = OutputHandle.AddrOfPinnedObject(),
					Size = OUTPUT_BUFFER_SIZE,
					Pos = 0,
				};

				InUse = false;
			}

			private bool _disposed = false;

			public void Dispose()
			{
				if (!_disposed)
				{
					_lib.ZSTD_freeDStream(Zds);
					InputHandle.Free();
					OutputHandle.Free();
					_disposed = true;
				}
			}

			public void InitContext()
			{
				if (InUse)
				{
					throw new InvalidOperationException("Cannot init context still in use!");
				}

				_lib.ZSTD_initDStream(Zds);
				Input.Size = Input.Pos = Output.Pos = 0;
				InUse = true;
			}
		}

		private sealed class ZstdDecompressionStream : Stream
		{
			private readonly Stream _baseStream;
			private readonly ZstdDecompressionStreamContext _ctx;

			public ZstdDecompressionStream(Stream baseStream, ZstdDecompressionStreamContext ctx)
			{
				_baseStream = baseStream;
				_ctx = ctx;
			}

			private bool _disposed = false;

			protected override void Dispose(bool disposing)
			{
				if (disposing && !_disposed)
				{
					_ctx.InUse = false;
					_disposed = true;
				}

				base.Dispose(disposing);
			}

			public override bool CanRead
				=> true;

			public override bool CanSeek
				=> false;

			public override bool CanWrite
				=> false;

			public override long Length
				=> _baseStream.Length; // FIXME: this wrong but this is only used in a > 0 check so I guess it works?

			public override long Position
			{
				get => throw new NotImplementedException();
				set => throw new NotImplementedException();
			}

			public override void Flush()
				=> throw new NotImplementedException();

			private ulong _outputConsumed = 0;

			public override int Read(byte[] buffer, int offset, int count)
			{
				int n = count;
				while (n > 0)
				{
					int inputConsumed = _baseStream.Read(_ctx.InputBuffer,
						(int)_ctx.Input.Size, (int)(ZstdDecompressionStreamContext.INPUT_BUFFER_SIZE - _ctx.Input.Size));
					_ctx.Input.Size += (ulong)inputConsumed;
					// avoid interop in case compression cannot be done
					if (_ctx.Output.Pos < ZstdDecompressionStreamContext.OUTPUT_BUFFER_SIZE
						&& _ctx.Input.Pos < _ctx.Input.Size)
					{
						CheckError(_lib.ZSTD_decompressStream(_ctx.Zds, ref _ctx.Output, ref _ctx.Input));
					}
					int outputToConsume = Math.Min(n, (int)(_ctx.Output.Pos - _outputConsumed));
					Marshal.Copy(_ctx.Output.Ptr + (int)_outputConsumed, buffer, offset, outputToConsume);
					_outputConsumed += (ulong)outputToConsume;
					offset += outputToConsume;
					n -= outputToConsume;

					if (_outputConsumed == ZstdDecompressionStreamContext.OUTPUT_BUFFER_SIZE)
					{
						// all the buffer is consumed, kick these back to the beginning
						_ctx.Output.Pos = _outputConsumed = 0;
					}

					if (_ctx.Input.Pos == ZstdDecompressionStreamContext.INPUT_BUFFER_SIZE)
					{
						// ditto here
						_ctx.Input.Pos = _ctx.Input.Size = 0;
					}

					// couldn't consume anything, get out
					// (decompression must be complete at this point)
					if (inputConsumed == 0 && outputToConsume == 0)
					{
						break;
					}
				}

				return count - n;
			}

			public override long Seek(long offset, SeekOrigin origin)
				=> throw new NotImplementedException();

			public override void SetLength(long value)
				=> throw new NotImplementedException();

			public override void Write(byte[] buffer, int offset, int count)
				=> throw new NotImplementedException();
		}

		private static readonly LibZstd _lib;

		public static int MinCompressionLevel { get; }

		public static int MaxCompressionLevel { get; }

		static Zstd()
		{
			DynamicLibraryImportResolver resolver = new DynamicLibraryImportResolver(
				OSTailoredCode.IsUnixHost ? "libzstd.so.1" : "libzstd.dll", hasLimitedLifetime: false);
			_lib = BizInvoker.GetInvoker<LibZstd>(resolver, CallingConventionAdapters.Native);

			MinCompressionLevel = _lib.ZSTD_minCLevel();
			MaxCompressionLevel = _lib.ZSTD_maxCLevel();
		}

		private ZstdCompressionStreamContext? _compressionStreamContext;
		private ZstdDecompressionStreamContext? _decompressionStreamContext;

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				_compressionStreamContext?.Dispose();
				_decompressionStreamContext?.Dispose();
				_disposed = true;
			}
		}

		private static void CheckError(ulong code)
		{
			if (_lib.ZSTD_isError(code) != 0)
			{
				throw new Exception($"ZSTD ERROR: {Marshal.PtrToStringAnsi(_lib.ZSTD_getErrorName(code))}");
			}
		}

		/// <summary>
		/// Creates a zstd compression stream.
		/// This stream uses a shared context as to avoid buffer allocation spam.
		/// It is absolutely important to call Dispose() / use using on returned stream.
		/// If this is not done, the shared context will remain in use,
		/// and the proceeding attempt to initialize it will throw.
		/// Also, of course, do not attempt to create multiple streams at once.
		/// Only 1 stream at a time is allowed per Zstd instance.
		/// </summary>
		/// <param name="stream">the stream to write compressed data</param>
		/// <param name="compressionLevel">compression level, bounded by MinCompressionLevel and MaxCompressionLevel</param>
		/// <returns>zstd compression stream</returns>
		/// <exception cref="ArgumentOutOfRangeException">compressionLevel is too small or too big</exception>
		public Stream CreateZstdCompressionStream(Stream stream, int compressionLevel)
		{
			if (compressionLevel < MinCompressionLevel || compressionLevel > MaxCompressionLevel)
			{
				throw new ArgumentOutOfRangeException(nameof(compressionLevel));
			}

			_compressionStreamContext ??= new();
			_compressionStreamContext.InitContext(compressionLevel);
			return new ZstdCompressionStream(stream, _compressionStreamContext);
		}

		/// <summary>
		/// Creates a zstd decompression stream.
		/// This stream uses a shared context as to avoid buffer allocation spam.
		/// It is absolutely important to call Dispose() / use using on returned stream.
		/// If this is not done, the shared context will remain in use,
		/// and the proceeding attempt to initialize it will throw.
		/// Also, of course, do not attempt to create multiple streams at once.
		/// Only 1 stream at a time is allowed per Zstd instance.
		/// </summary>
		/// <param name="stream">a stream with zstd compressed data to decompress</param>
		/// <returns>zstd decompression stream</returns>
		public Stream CreateZstdDecompressionStream(Stream stream)
		{
			_decompressionStreamContext ??= new();
			_decompressionStreamContext.InitContext();
			return new ZstdDecompressionStream(stream, _decompressionStreamContext);
		}

		/// <summary>
		/// Decompresses src stream and returns a memory stream with the decompressed contents.
		/// Context creation and disposing is handled internally in this function, unlike the non-static ones.
		/// This is useful in cases where you are not doing repeated decompressions,
		/// so keeping a Zstd instance around is not as useful.
		/// </summary>
		/// <param name="src">stream with zstd compressed data to decompress</param>
		/// <returns>MemoryStream with the decompressed contents of src</returns>
		/// <exception cref="InvalidOperationException">src does not have a ZSTD header</exception>
		public static MemoryStream DecompressZstdStream(Stream src)
		{
			// check for ZSTD header
			byte[] tmp = new byte[4];
			if (src.Read(tmp, 0, 4) != 4)
			{
				throw new InvalidOperationException("Unexpected end of stream");
			}
			if (tmp[0] != 0x28 || tmp[1] != 0xB5 || tmp[2] != 0x2F || tmp[3] != 0xFD)
			{
				throw new InvalidOperationException("ZSTD header not present");
			}
			src.Seek(0, SeekOrigin.Begin);

			using ZstdDecompressionStreamContext dctx = new ZstdDecompressionStreamContext();
			dctx.InitContext();
			using ZstdDecompressionStream dstream = new ZstdDecompressionStream(src, dctx);
			MemoryStream ret = new MemoryStream();
			dstream.CopyTo(ret);
			return ret;
		}
	}
}

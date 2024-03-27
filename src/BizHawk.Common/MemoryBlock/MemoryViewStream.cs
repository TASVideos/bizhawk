using System;
using System.IO;

namespace BizHawk.Common
{
	/// <summary>
	/// Create a stream that allows read/write over a set of unmanaged memory pointers
	/// The validity and lifetime of those pointers is YOUR responsibility
	/// </summary>
	public unsafe class MemoryViewStream : Stream, ISpanStream
	{
		public MemoryViewStream(bool readable, bool writable, long ptr, long length)
		{
			_readable = readable;
			_writable = writable;
			_ptr = ptr;
			_length = length;
			_pos = 0;
		}

		private readonly long _length;
		private readonly long _ptr;
		private readonly bool _readable;
		private readonly bool _writable;

		private long _pos;
		private bool _closed;

		public override bool CanRead => _readable;
		public override bool CanSeek => true;
		public override bool CanWrite => _writable;
		public override long Length => _length;

		public override long Position
		{
			get => _pos;
			set
			{
				if (value < 0 || value > _length)
				{
					throw new ArgumentOutOfRangeException(paramName: nameof(value), value, message: "index out of range");
				}

				_pos = value;
			}
		}

		private void EnsureNotDisposed()
		{
			if (_closed)
			{
				throw new ObjectDisposedException(nameof(MemoryViewStream));
			}
		}

		public override void Flush()
		{
		}

		private byte* CurrentPointer()
			=> (byte*)Z.SS(_ptr + _pos);

#if NET6_0_OR_GREATER
		public override int Read(Span<byte> buffer)
#else
		public int Read(Span<byte> buffer)
#endif
		{
			if (!_readable)
			{
				throw new IOException();
			}

			EnsureNotDisposed();
			var count = (int)Math.Min(buffer.Length, _length - _pos);
			new ReadOnlySpan<byte>(CurrentPointer(), count).CopyTo(buffer);
			_pos += count;
			return count;
		}

		public override int Read(byte[] buffer, int offset, int count)
			=> Read(new(buffer, offset, count));

		public override int ReadByte()
		{
			if (_pos >= _length)
			{
				return -1;
			}

			var ret = *CurrentPointer();
			_pos++;
			return ret;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			var newpos = origin switch
			{
				SeekOrigin.Begin => offset,
				SeekOrigin.Current => _pos + offset,
				SeekOrigin.End => _length + offset,
				_ => offset
			};

			Position = newpos;
			return newpos;
		}

		public override void SetLength(long value)
			=> throw new IOException();

#if NET6_0_OR_GREATER
		public override void Write(ReadOnlySpan<byte> buffer)
#else
		public void Write(ReadOnlySpan<byte> buffer)
#endif
		{
			if (!_writable)
			{
				throw new IOException();
			}

			EnsureNotDisposed();
			if (_pos + buffer.Length > _length)
			{
				throw new IOException("End of non-resizable stream");
			}

			buffer.CopyTo(new(CurrentPointer(), buffer.Length));
			_pos += buffer.Length;
		}

		public override void Write(byte[] buffer, int offset, int count)
			=> Write(new(buffer, offset, count));

		public override void WriteByte(byte value)
		{
			if (_pos >= _length)
			{
				throw new IOException("End of non-resizable stream");
			}

			*CurrentPointer() = value;
			_pos++;
		}

		protected override void Dispose(bool disposing)
		{
			_closed = true;
			base.Dispose(disposing);
		}
	}
}

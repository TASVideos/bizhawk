using System;
using System.Diagnostics;
using System.Security.Cryptography;

using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="MD5"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="SHA1Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public static class MD5Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 128;

		public const string PREFIX = "MD5";

		public /*static readonly*/const string EmptyFile = "D41D8CD98F00B204E9800998ECF8427E";

#if NET6_0_OR_GREATER
		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> MD5.HashData(data);
#else
		private static MD5? _md5Impl;

		private static MD5 MD5Impl
		{
			get
			{
				if (_md5Impl == null)
				{
					_md5Impl = MD5.Create();
					Debug.Assert(_md5Impl.CanReuseTransform && _md5Impl.HashSize is EXPECTED_LENGTH);
				}
				return _md5Impl;
			}
		}

		public static byte[] Compute(byte[] data)
			=> MD5Impl.ComputeHash(data);

		public static string ComputeDigestHex(byte[] data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(byte[] data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";

		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());
#endif

		public static string ComputeDigestHex(ReadOnlySpan<byte> data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(ReadOnlySpan<byte> data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";
	}
}

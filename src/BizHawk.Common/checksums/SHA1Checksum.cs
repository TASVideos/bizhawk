using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using BizHawk.Common.BufferExtensions;
using BizHawk.Common.CollectionExtensions;

namespace BizHawk.Common
{
	/// <summary>uses <see cref="SHA1"/> implementation from BCL</summary>
	/// <seealso cref="CRC32Checksum"/>
	/// <seealso cref="MD5Checksum"/>
	/// <seealso cref="SHA256Checksum"/>
	public static class SHA1Checksum
	{
		/// <remarks>in bits</remarks>
		internal const int EXPECTED_LENGTH = 160;

		public const string PREFIX = "SHA1";

		public /*static readonly*/const string Dummy = "EEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE";

		public /*static readonly*/const string EmptyFile = "DA39A3EE5E6B4B0D3255BFEF95601890AFD80709";

		public /*static readonly*/const string Zero = "0000000000000000000000000000000000000000";

#if NET6_0_OR_GREATER
		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> SHA1.HashData(data);

		public static byte[] ComputeConcat(ReadOnlySpan<byte> dataA, ReadOnlySpan<byte> dataB)
		{
			using var impl = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			impl.AppendData(dataA);
			impl.AppendData(dataB);
			return impl.GetHashAndReset();
		}
#else
		private static unsafe byte[] UnmanagedImpl(byte[] buffer)
		{
			// Set SHA1 start state
			var state = stackalloc uint[] { 0x67452301, 0xEFCDAB89, 0x98BADCFE, 0x10325476, 0xC3D2E1F0 };
			// This will use dedicated SHA instructions, which perform 4x faster than a generic implementation
			LibBizHash.BizCalcSha1((IntPtr) state, buffer, buffer.Length);
			// The copy seems wasteful, but pinning the state down actually has a bigger performance impact
			var ret = new byte[20];
			Marshal.Copy((IntPtr) state, ret, 0, 20);
			return ret;
		}

		private static SHA1? _sha1Impl;

		private static SHA1 SHA1Impl
		{
			get
			{
				if (_sha1Impl == null)
				{
					_sha1Impl = SHA1.Create();
					Debug.Assert(_sha1Impl.CanReuseTransform && _sha1Impl.HashSize is EXPECTED_LENGTH);
				}
				return _sha1Impl;
			}
		}

		private static readonly bool UseUnmanagedImpl = RuntimeInformation.ProcessArchitecture == Architecture.X64 && LibBizHash.BizSupportsShaInstructions();

		public static byte[] Compute(byte[] data)
			=> UseUnmanagedImpl
				? UnmanagedImpl(data)
				: SHA1Impl.ComputeHash(data);

		public static byte[] ComputeConcat(byte[] dataA, byte[] dataB)
		{
			if (UseUnmanagedImpl) return UnmanagedImpl(dataA.ConcatArray(dataB));
			using var impl = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
			impl.AppendData(dataA);
			impl.AppendData(dataB);
			return impl.GetHashAndReset();
		}

		public static string ComputeDigestHex(byte[] data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(byte[] data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";

		public static byte[] Compute(ReadOnlySpan<byte> data)
			=> Compute(data.ToArray());

		public static byte[] ComputeConcat(ReadOnlySpan<byte> dataA, ReadOnlySpan<byte> dataB)
			=> ComputeConcat(dataA.ToArray(), dataB.ToArray());
#endif

		public static string ComputeDigestHex(ReadOnlySpan<byte> data)
			=> Compute(data).BytesToHexString();

		public static string ComputePrefixedHex(ReadOnlySpan<byte> data)
			=> $"{PREFIX}:{ComputeDigestHex(data)}";
	}
}

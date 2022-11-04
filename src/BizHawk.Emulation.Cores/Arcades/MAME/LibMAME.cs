﻿using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Arcades.MAME
{
	public abstract class LibMAME
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		// enums
		public enum OutputChannel : int
		{
			ERROR, WARNING, INFO, DEBUG, VERBOSE, LOG, COUNT
		}

		// constants
		public const int ROMENTRYTYPE_SYSTEM_BIOS = 9;
		public const int ROMENTRYTYPE_DEFAULT_BIOS = 10;
		public const int ROMENTRY_TYPEMASK = 15;
		public const int BIOS_INDEX = 24;
		public const int BIOS_FIRST = 1;
		public const string BIOS_LUA_CODE = "bios";

		// main launcher
		[BizImport(cc, Compatibility = true)]
		public abstract uint mame_launch(int argc, string[] argv);

		[BizImport(cc)]
		public abstract void mame_coswitch();

		[BizImport(cc)]
		public abstract char mame_read_byte(uint address);

		[BizImport(cc)]
		public abstract int mame_get_sound(short[] samples);

		// execute
		[BizImport(cc)]
		public abstract void mame_lua_execute(string code);

		// get int
		[BizImport(cc)]
		public abstract int mame_lua_get_int(string code);

		// get long
		// nb: this is actually a double cast to long internally
		[BizImport(cc)]
		public abstract long mame_lua_get_long(string code);

		// get bool
		[BizImport(cc)]
		public abstract bool mame_lua_get_bool(string code);

		/// <summary>
		/// MAME's luaengine uses lua strings to return C strings as well as
		/// binary buffers. You're meant to know which you're going to get and
		/// handle that accordingly. When we want to get a C string, we
		/// Marshal.PtrToStringAnsi(). With buffers, we Marshal.Copy()
		/// to our new buffer. MameGetString() only covers the former
		/// because it's the same steps every time, while buffers use to
		/// need aditional logic. In both cases MAME wants us to manually
		/// free the string buffer. It's made that way to make the buffer
		/// persist actoss C API calls.
		/// </summary>

		// get string
		[BizImport(cc)]
		public abstract IntPtr mame_lua_get_string(string code, out int length);

		// free string
		[BizImport(cc)]
		public abstract bool mame_lua_free_string(IntPtr pointer);

		// log
		[UnmanagedFunctionPointer(cc)]
		public delegate void LogCallbackDelegate(OutputChannel channel, int size, string data);

		[BizImport(cc)]
		public abstract void mame_set_log_callback(LogCallbackDelegate cb);
	}
}

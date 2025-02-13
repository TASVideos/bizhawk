/* config.h.  Generated from config.h.in by configure.  */
/* config.h.in.  Generated from configure.ac by autoheader.  */


/*
 *  Copyright (C) 2002-2021  The DOSBox Team
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU Library General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License along
 *  with this program; if not, write to the Free Software Foundation, Inc.,
 *  51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
 */


/* Define if building universal (internal helper macro) */
/* #undef AC_APPLE_UNIVERSAL_BUILD */

/* Compiling on BSD */
/* #undef BSD */

/* Determines if the compilers supports always_inline attribute. */
/* #undef C_ATTRIBUTE_ALWAYS_INLINE */

/* Determines if the compilers supports fastcall attribute. */
/* #undef C_ATTRIBUTE_FASTCALL */

/* Define to 1 to use FFMPEG libavcodec for video capture */
/* #undef C_AVCODEC */

/* Define to 1 to use Direct3D shaders */
/* #undef C_D3DSHADERS */

/* Define to 1 to enable internal debugger, requires libcurses */
/* #undef C_DEBUG */

/* Define to 1 to enable Direct3D 9 display output support */
/* #undef C_DIRECT3D */

/* Define to 1 if you want parallel pass-through support (Win32, Linux and
   BSD). */
#define C_DIRECTLPT 1

/* Define to 1 if you want serial pass-through support (Win32, Posix and
   OS/2). */
#define C_DIRECTSERIAL 1

/* Define to 1 to use x86/x64 dynamic cpu core */
#define C_DYNAMIC_X86 1

/* Define to 1 to use recompiling cpu core */
#define C_DYNREC 1

/* Targeting Emscripten */
/* #undef C_EMSCRIPTEN */

/* Define to 1 to enable libfluidsynth MIDI synthesis */
#define C_FLUIDSYNTH 0

/* Force SDL drawn menus */
/* #undef C_FORCE_MENU_SDLDRAW */

/* Define to 1 to enable floating point emulation */
#define C_FPU 1

/* Define to 1 to use a x86/x64 assembly fpu core */
#define C_FPU_X86 1

/* Define to 1 to enable freetype support */
/* #undef C_FREETYPE */

/* Define to 1 to enable game link headless mode */
#define C_GAMELINK 1

/* Determines if the compilers supports attributes for structures. */
#define C_HAS_ATTRIBUTE 1

/* Determines if the compilers supports __builtin_expect for branch
   prediction. */
/* #undef C_HAS_BUILTIN_EXPECT */

/* Define to 1 if you have linux/kvm.h and KVM virtualization */
#define C_HAVE_LINUX_KVM 1

/* Define to 1 if you have the mach_vm_remap function */
/* #undef C_HAVE_MACH_VM_REMAP */

/* Define to 1 if you have the memfd_create function */
#define C_HAVE_MEMFD_CREATE 1

/* Define to 1 if you have the mmap function */
#define C_HAVE_MMAP 1

/* Define to 1 if you have the mprotect function */
#define C_HAVE_MPROTECT 1

/* Define to 1 if you have the posix_memalign function */
/* #undef C_HAVE_POSIX_MEMALIGN */

/* Define to 1 to enable heavy debugging, also have to enable C_DEBUG */
/* #undef C_HEAVY_DEBUG */

/* Targeting HX DOS extender */
/* #undef C_HX_DOS */

/* Define to 1 to use iconv */
#define C_ICONV 1

/* Define to 1 to use Win32 functions in iconv backend */
/* #undef C_ICONV_WIN32 */

/* Define to 1 to enable IPX over Internet networking, requires SDL_net */
/* #undef C_IPX */

/* Define to 1 if you have libpng */
/* #undef C_LIBPNG  */

/* Define to 1 if you have libz */
/* #undef C_LIBZ */

/* Define to 1 to enable internal modem support, requires SDL_net */
/* #undef C_MODEM */

/* Define to 1 to enable MT32 emulation */
#define C_MT32 1

/* Define to 1 to use opengl display output support */
/* #undef C_OPENGL */

/* Define to 1 to enable ethernet pass-through, requires libpcap */
/* #undef C_PCAP */

/* Define to 1 to enable printer emulation */
/* #undef C_PRINTER */

/* Define to 1 to alter the simpler render scalers to operate only on the full
   scanline instead of detecting differences. This is a performance adjustment
   for slow or embedded systems */
/* #undef C_SCALER_FULL_LINE */

/* Set to 1 to enable SDL 1.x support */
/* #undef C_SDL1 */

/* Set to 1 to enable SDL 2.x support */
#define C_SDL2 1

/* Indicate whether SDL_net is present */
/* #undef C_SDL_NET */

/* Define to 1 if you have setpriority support */
/* #undef C_SET_PRIORITY */

/* Define to 1 to enable userspace TCP/IP emulation, requires libslirp */
/* #undef C_SLIRP */

/* Define to 1 to enable screenshots, requires libpng */
/* #undef C_SSHOT */

/* The type of cpu this target has */
#define C_TARGETCPU X86_64

/* Define to 1 to use a unaligned memory access */
#define C_UNALIGNED_MEMORY 1

/* define to 1 to enable X11 support */
/* #define C_X11 */

/* define to 1 if XKBrules.h is present */
/* #undef C_X11_EXT_XKBRULES */

/* define to 1 if you have XKBlib.h and X11 lib */
/* #undef C_X11_XKB */

/* define to 1 if XKBfile.h is present */
/* #undef C_X11_XKBFILE */

/* define to 1 if you have XRandr.h and X11 lib */
/* #undef C_X11_XRANDR */

/* Define to 1 to enable XBRZ scaler */
/* #undef C_XBRZ */

/* Determines if the function clock_gettime is available. */
#define DB_HAVE_CLOCK_GETTIME 1

/* libm does not include powf */
/* #undef DB_HAVE_NO_POWF */

/* struct dirent has d_type */
#define DIRENT_HAS_D_TYPE 1

/* environ can be included */
/* #undef ENVIRON_INCLUDED */

/* environ can be linked */
#define ENVIRON_LINKED 1

/* Compiling on Haiku */
/* #undef HAIKU */

/* Define to 1 to use ALSA for MIDI */
/* #undef HAVE_ALSA */

/* Define to 1 to use Direct3D 9 display output support */
/* #undef HAVE_D3D9_H */

/* Define to 1 if you have the <inttypes.h> header file. */
#define HAVE_INTTYPES_H 1

/* Define to 1 if you have the `asound' library (-lasound). */
#define HAVE_LIBASOUND 1

/* Define to 1 if you have the <netinet/in.h> header file. */
#define HAVE_NETINET_IN_H 1

/* Define to 1 if you have the <pwd.h> header file. */
#define HAVE_PWD_H 1

/* Define to 1 if you have the `realpath' function. */
#define HAVE_REALPATH 1

/* Define to 1 if you have the <stdint.h> header file. */
#define HAVE_STDINT_H 1

/* Define to 1 if you have the <stdio.h> header file. */
#define HAVE_STDIO_H 1

/* Define to 1 if you have the <stdlib.h> header file. */
#define HAVE_STDLIB_H 1

/* Define to 1 if you have the <strings.h> header file. */
#define HAVE_STRINGS_H 1

/* Define to 1 if you have the <string.h> header file. */
#define HAVE_STRING_H 1

/* Define to 1 if you have the <sys/socket.h> header file. */
#define HAVE_SYS_SOCKET_H 1

/* Define to 1 if you have the <sys/stat.h> header file. */
#define HAVE_SYS_STAT_H 1

/* Define to 1 if you have the <sys/types.h> header file. */
#define HAVE_SYS_TYPES_H 1

/* Define to 1 if you have the <unistd.h> header file. */
#define HAVE_UNISTD_H 1

/* Whether iconv uses const char** */
/* #undef ICONV_CONST_CHAR */

/* Compiling on GNU/Linux */
#define LINUX 1

/* Compiling on Mac OS X */
/* #undef MACOSX */

/* Compiling on OS/2 EMX */
/* #undef OS2 */

/* Name of package */
#define PACKAGE "dosbox-x"

/* Define to the address where bug reports for this package should be sent. */
#define PACKAGE_BUGREPORT "https://github.com/joncampbell123/dosbox-x/issues"

/* Define to the full name of this package. */
#define PACKAGE_NAME "dosbox-x"

/* Define to the full name and version of this package. */
#define PACKAGE_STRING "dosbox-x 2025.02.01"

/* Define to the one symbol short name of this package. */
#define PACKAGE_TARNAME "dosbox-x"

/* Define to the home page for this package. */
#define PACKAGE_URL "https://dosbox-x.com"

/* Define to the version of this package. */
#define PACKAGE_VERSION "2025.02.01"

/* Compiling on RISC OS */
/* #undef RISCOS */

/* Define to 1 if all of the C90 standard headers exist (not just the ones
   required in a freestanding environment). This macro is provided for
   backward compatibility; new code need not use it. */
#define STDC_HEADERS 1

/* Define to 1 if your <sys/time.h> declares `struct tm'. */
/* #undef TM_IN_SYS_TIME */

/* Version number of package */
#define VERSION "2025.02.01"

/* Define WORDS_BIGENDIAN to 1 if your processor stores words with the most
   significant byte first (like Motorola and SPARC, unlike Intel). */
#if defined AC_APPLE_UNIVERSAL_BUILD
# if defined __BIG_ENDIAN__
#  define WORDS_BIGENDIAN 1
# endif
#else
# ifndef WORDS_BIGENDIAN
/* #  undef WORDS_BIGENDIAN */
# endif
#endif

/* Number of bits in a file offset, on hosts where this is settable. */
/* #undef _FILE_OFFSET_BITS */

/* Define for large files, on AIX-style hosts. */
/* #undef _LARGE_FILES */

/* Define to empty if `const' does not conform to ANSI C. */
/* #undef const */

/* Define to `__inline__' or `__inline' if that's what the C compiler
   calls it, or to nothing if 'inline' is not supported under any name.  */
#ifndef __cplusplus
/* #undef inline */
#endif

/* Define to `unsigned int' if <sys/types.h> does not define. */
/* #undef size_t */

#if C_ATTRIBUTE_ALWAYS_INLINE
#define INLINE inline __attribute__((always_inline))
#else
#define INLINE inline
#endif

#if C_ATTRIBUTE_FASTCALL
#define DB_FASTCALL __attribute__((fastcall))
#else
#define DB_FASTCALL
#endif

#if C_HAS_ATTRIBUTE
#define GCC_ATTRIBUTE(x) __attribute__ ((x))
#else
#define GCC_ATTRIBUTE(x) /* attribute not supported */
#endif

#if C_HAS_BUILTIN_EXPECT
#define GCC_UNLIKELY(x) __builtin_expect((x),0)
#define GCC_LIKELY(x) __builtin_expect((x),1)
#else
#define GCC_UNLIKELY(x) (x)
#define GCC_LIKELY(x) (x)
#endif


#if !defined(FLUIDINC)
#include <cstdint>

typedef uintptr_t Bitu;
typedef intptr_t Bits;
#endif


/*
  Define HAS_CDIRECTLPT as 1 if C_DIRECTLPT is defined (as 1) *and* parallel
  pass-through is available on the current platform. It is only available on
  x86{_64} with Windows or BSD, and on Linux.
  We cannot override the value of C_DIRECTLPT, because configure will replace
  "#undef C_DIRECTLPT" or "#define C_DIRECTLPT 0" with "#define C_DIRECTLPT 1".
*/
#undef HAS_CDIRECTLPT
// #ifdef C_DIRECTLPT
// #if ((defined __i386__ || defined __x86_64__ || defined _M_IX86 || defined _M_X64) && \
//      (defined WIN32 || defined BSD || defined __CYGWIN__)) || \
//      /* WIN32 is not defined by default on Cygwin */ \
//     defined LINUX /* Linux, including non-x86 (e.g. Raspberry Pi) */
// #define HAS_CDIRECTLPT 1
// #endif
// #endif // C_DIRECTLPT
// #ifndef HAS_CDIRECTLPT
// #define HAS_CDIRECTLPT 0
// #endif

/*
  libco.amd64 (2016-09-14)
  author: byuu
  license: public domain
*/

#include "libco.h"

#include <stdint.h>
#include <assert.h>
#include <stdlib.h>
#include <string.h>
#include <sys/mman.h>
#include <emulibc.h>

// allocations are 16k larger than asked for, which is all used as guard space
#define GUARD_SIZE 0x4000

typedef struct {
	// used by coswap.s, has to be at the beginning of the struct
	struct {
		uint64_t rsp;
		uint64_t rbp; // we have to save rbp because unless fomit-frame-pointer is set, the compiler regards it as "special" and won't allow clobbers
		uint64_t rip;
	} jmp_buf;
	// points to the lowest address in the stack
	// NB: because of guard space, this is not valid stack
	void* stack;
	// length of the stack that we allocated in bytes
	uint64_t stack_size;
} cothread_impl;

// the cothread that represents the real host thread we started from
static cothread_impl co_host_buffer;
// what cothread are we in right now
static cothread_impl* co_active_handle;

static void free_thread(cothread_impl* co)
{
	if (munmap(co->stack, co->stack_size) != 0)
		abort();
	free(co);
}

static cothread_impl* alloc_thread(uint64_t size)
{
	cothread_impl* co = calloc(1, sizeof(*co));
	if (!co)
		return NULL;

	// align up to 4k
	size = (size + 4095) & ~4095ul;
	size += GUARD_SIZE;

	co->stack = mmap(NULL, size,
		PROT_READ | PROT_WRITE, MAP_PRIVATE | MAP_ANONYMOUS | MAP_STACK, -1, 0);
	
	if (co->stack == (void*)(-1))
	{
		free(co);
		return NULL;
	}

	if (mprotect(co->stack, GUARD_SIZE, PROT_NONE) != 0)
	{
		free_thread(co);
		return NULL;
	}

	co->stack_size = size;
	return co;
}

static void crash(void)
{
	__asm__("int3"); // called only if cothread_t entrypoint returns
}

ECL_EXPORT void co_clean(void)
{
	memset(&co_host_buffer, 0, sizeof(co_host_buffer));
}

cothread_t co_active(void)
{
	if (!co_active_handle)
		co_active_handle = &co_host_buffer;
	return co_active_handle;
}

cothread_t co_create(unsigned int sz, void (*entrypoint)(void))
{
	cothread_impl* co;
	if (!co_active_handle)
		co_active_handle = &co_host_buffer;

	if ((co = alloc_thread(sz)))
	{
		uint64_t* p = (uint64_t*)((char*)co->stack + co->stack_size); // seek to top of stack
		*--p = (uint64_t)crash; // crash if entrypoint returns
		co->jmp_buf.rsp = (uint64_t)p; // stack pointer
		co->jmp_buf.rip = (uint64_t)entrypoint; // start of function
	}

	return co;
}

void co_delete(cothread_t handle)
{
	free_thread(handle);
}

void co_switch(cothread_t handle)
{
	cothread_impl* co = handle;
	cothread_impl* co_previous_handle = co_active_handle;
	co_active_handle = co;

	register uint64_t _rdi __asm__("rdi") = (uint64_t)co_previous_handle;
	register uint64_t _rsi __asm__("rsi") = (uint64_t)co_active_handle;

	/*
		mov [rdi + 0], rsp
		mov [rdi + 8], rbp
		lea rax, [rip + 17]
		mov [rdi + 16], rax
		mov rsp, [rsi + 0]
		mov rbp, [rsi + 8]
		mov rax, [rsi + 16]
		jmp rax
	*/
	__asm__(
		"mov %%rsp, 0(%%rdi)\n"
		"mov %%rbp, 8(%%rdi)\n"
		"lea 17(%%rip), %%rax\n"
		"mov %%rax, 16(%%rdi)\n"
		"mov 0(%%rsi), %%rsp\n"
		"mov 8(%%rsi), %%rbp\n"
		"mov 16(%%rsi), %%rax\n"
		"jmp *%%rax\n"
		::"r"(_rdi), "r"(_rsi)
		:"rax", "rbx", "rcx", "rdx", /*"rbp",*/ /*"rsi", "rdi",*/ "r8", "r9", "r10", "r11", "r12", "r13", "r14", "r15",
			"zmm0", "zmm1", "zmm2", "zmm3", "zmm4", "zmm5", "zmm6", "zmm7", "zmm8", "zmm9",
			"zmm10", "zmm11", "zmm12", "zmm13", "zmm14", "zmm15",
			/*"zmm16", "zmm17", "zmm18", "zmm19",
			"zmm20", "zmm21", "zmm22", "zmm23", "zmm24", "zmm25", "zmm26", "zmm27", "zmm28", "zmm29",
			"zmm30", "zmm31",*/
			"memory"
	);
}

cothread_t co_derive(void* memory, unsigned sz, void (*entrypoint)(void))
{
	return NULL;
}

int co_serializable(void)
{
	return 0;
}

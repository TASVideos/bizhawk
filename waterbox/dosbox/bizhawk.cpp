#include "bizhawk.hpp"
#include "../libco/libco.h"

extern int _main(int argc, char* argv[]);
void runMain() { _main(0, nullptr); }
cothread_t _emuCoroutine;
cothread_t _driverCoroutine;

ECL_EXPORT bool Init(int argc, char **argv)
{
	// FILE* f = fopen("FloppyDisk0", "rb");
	// 
	// if (f == NULL) return false;
	// else 
	// {
	// 	fseek(f, 0L, SEEK_END);
 //        size_t size = ftell(f);
	// 	printf("File Size: %lu\n", size);
	// 	fclose(f);
	// }

	constexpr size_t stackSize = 4 * 1024 * 1024;
	_emuCoroutine = co_create(stackSize, runMain);
	_driverCoroutine = co_active();
	co_switch(_emuCoroutine);

	return true;
}

ECL_EXPORT void FrameAdvance(MyFrameInfo* f)
{
}

uint8_t mainRAM[256];

ECL_EXPORT void GetMemoryAreas(MemoryArea *m)
{
	m[0].Data  = mainRAM;
	m[0].Name  = "Main RAM";
	m[0].Size  = 256;
	m[0].Flags = MEMORYAREA_FLAGS_WORDSIZE1 | MEMORYAREA_FLAGS_WRITABLE | MEMORYAREA_FLAGS_PRIMARY;

}

void (*LEDCallback)();
ECL_EXPORT void SetLEDCallback(void (*callback)())
{
	LEDCallback = callback;
}

void (*InputCallback)();
ECL_EXPORT void SetInputCallback(void (*callback)())
{
	InputCallback = callback;
}

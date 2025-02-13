#include <vector>
#include <string>
#include <cstdio>
#include <cstdint>
#include "BizhawkInterface.hxx"
#include <d_player.h>
#include <w_wad.h>

extern "C"
{
  int headlessMain(int argc, char **argv);
  void headlessRunSingleTick();
  void headlessUpdateSounds(void);
  void headlessClearTickCommand();
  void headlessSetTickCommand(int playerId, int forwardSpeed, int strafingSpeed, int turningSpeed, int fire, int action, int weapon, int altWeapon, int lookfly, int artifact, int jump, int endPlayer);

  // Video-related functions
  void headlessUpdateVideo(void);
  void* headlessGetVideoBuffer();
  int headlessGetVideoPitch();
  int headlessGetVideoWidth();
  int headlessGetVideoHeight();
  void headlessEnableVideoRendering();
  void headlessDisableVideoRendering();
  void headlessEnableAudioRendering();
  void headlessDisableAudioRendering();
  uint32_t* headlessGetPallette();

  void headlessSetSaveStatePointer(void* savePtr, int saveStateSize);
  size_t headlessGetEffectiveSaveSize();
  void dsda_ArchiveAll(void);
  void dsda_UnArchiveAll(void);
  void headlessGetMapName(char* outString);
  
  void D_AddFile (const char *file, wad_source_t source, void* const buffer, const size_t size);
  void AddIWAD(const char *iwad, void* const buffer, const size_t size); 
  unsigned char * I_CaptureAudio (int* nsamples);
  void I_InitSound(void);
  void I_SetSoundCap (void);
}

// Players information
extern "C" int enableOutput;
extern "C" player_t players[MAX_MAXPLAYERS];
extern "C" int preventLevelExit;
extern "C" int preventGameEnd;
extern "C" int reachedLevelExit;
extern "C" int reachedGameEnd;
extern "C" int gamemap;
extern "C" int gametic;
extern "C" dboolean playeringame[MAX_MAXPLAYERS];
extern "C" int consoleplayer;
extern "C" int displayplayer;
extern "C" pclass_t PlayerClass[MAX_MAXPLAYERS];

struct InitSettings
{
	int _Player1Present;
	int _Player2Present;
	int _Player3Present;
	int _Player4Present;
	int _CompatibilityMode;
	int _SkillLevel;
	int _MultiplayerMode;
	int _InitialEpisode;
	int _InitialMap;
	int _Turbo;
	int _FastMonsters;
	int _MonstersRespawn;
	int _NoMonsters;
	int _Player1Class;
	int _Player2Class;
	int _Player3Class;
	int _Player4Class;
	int _ChainEpisodes;
	int _StrictMode;
	int _PreventLevelExit;
	int _PreventGameEnd;
} __attribute__((packed));

struct PackedPlayerInput
{
	int _RunSpeed;
	int _StrafingSpeed;
	int _TurningSpeed;
	int _WeaponSelect;
	int _Fire;
	int _Action;
	int _AltWeapon;
	int _FlyLook;
	int _ArtifactUse;
	int _Jump;
	int _EndPlayer;
} __attribute__((packed));

struct PackedRenderInfo
{
	int _RenderVideo;
	int _RenderAudio;
	int _PlayerPointOfView;
} __attribute__((packed));

ECL_EXPORT void dsda_get_audio(int *n, void **buffer)
{
	int nSamples = 0;
	void* audioBuffer = nullptr;
    audioBuffer = I_CaptureAudio(&nSamples);
	// printf("audioBuffer: %p - nSamples: %d\n", audioBuffer, nSamples);

	if (n)
		*n = nSamples;
	if (buffer)
		*buffer = audioBuffer;
}

#define PALETTE_SIZE 256
uint32_t _convertedPaletteBuffer[PALETTE_SIZE];
ECL_EXPORT void dsda_get_video(int& w, int& h, int& pitch, uint8_t*& buffer, int& paletteSize, uint32_t*& paletteBuffer)
{
	buffer = (uint8_t*)headlessGetVideoBuffer();
	w = headlessGetVideoWidth();
	h = headlessGetVideoHeight();
	pitch = headlessGetVideoPitch();
	paletteSize = PALETTE_SIZE;

	auto palette = headlessGetPallette();
	for (size_t i = 0; i < PALETTE_SIZE; i++)
	{
		uint8_t* srcColor = (uint8_t*)&palette[i];
		uint8_t* dstColor = (uint8_t*)&_convertedPaletteBuffer[i];
		dstColor[0] = srcColor[2];
		dstColor[1] = srcColor[1];
		dstColor[2] = srcColor[0];
		dstColor[3] = srcColor[3];
	} 

	paletteBuffer = _convertedPaletteBuffer;
}

ECL_EXPORT void dsda_frame_advance(struct PackedPlayerInput *player1Inputs, struct PackedPlayerInput *player2Inputs, struct PackedPlayerInput *player3Inputs, struct PackedPlayerInput *player4Inputs, struct PackedRenderInfo *renderInfo)
{
	// Setting inputs
    headlessClearTickCommand();

    // Setting Player 1 inputs
	headlessSetTickCommand
	(
		0,
		player1Inputs->_RunSpeed,
		player1Inputs->_StrafingSpeed,
		player1Inputs->_TurningSpeed,
		player1Inputs->_Fire,
		player1Inputs->_Action,
		player1Inputs->_WeaponSelect,
		player1Inputs->_AltWeapon,
		player1Inputs->_FlyLook,
		player1Inputs->_ArtifactUse,
		player1Inputs->_Jump,
		player1Inputs->_EndPlayer
	);

	// Setting Player 2 inputs
	headlessSetTickCommand
	(
		1,
		player2Inputs->_RunSpeed,
		player2Inputs->_StrafingSpeed,
		player2Inputs->_TurningSpeed,
		player2Inputs->_Fire,
		player2Inputs->_Action,
		player2Inputs->_WeaponSelect,
		player2Inputs->_AltWeapon,
		player2Inputs->_FlyLook,
		player2Inputs->_ArtifactUse,
		player2Inputs->_Jump,
		player2Inputs->_EndPlayer
	);

	// Setting Player 3 inputs
	headlessSetTickCommand
	(
		2,
		player3Inputs->_RunSpeed,
		player3Inputs->_StrafingSpeed,
		player3Inputs->_TurningSpeed,
		player3Inputs->_Fire,
		player3Inputs->_Action,
		player3Inputs->_WeaponSelect,
		player3Inputs->_AltWeapon,
		player3Inputs->_FlyLook,
		player3Inputs->_ArtifactUse,
		player3Inputs->_Jump,
		player3Inputs->_EndPlayer
	);

    // Setting Player 4 inputs
	headlessSetTickCommand
	(
		3,
		player4Inputs->_RunSpeed,
		player4Inputs->_StrafingSpeed,
		player4Inputs->_TurningSpeed,
		player4Inputs->_Fire,
		player4Inputs->_Action,
		player4Inputs->_WeaponSelect,
		player4Inputs->_AltWeapon,
		player4Inputs->_FlyLook,
		player4Inputs->_ArtifactUse,
		player4Inputs->_Jump,
		player4Inputs->_EndPlayer
	);

   // Enabling/Disabling rendering, as required
   if (renderInfo->_RenderVideo == 0) headlessDisableVideoRendering();
   if (renderInfo->_RenderVideo == 1) headlessEnableVideoRendering();
   if (renderInfo->_RenderAudio == 0) headlessDisableAudioRendering();
   if (renderInfo->_RenderAudio == 1) headlessEnableAudioRendering();

	// Running a single tick
	headlessRunSingleTick();

    // Updating video
    if (renderInfo->_RenderVideo == 1)
	{
	  displayplayer = consoleplayer = renderInfo->_PlayerPointOfView;
	  headlessUpdateVideo();
	} 
}

ECL_ENTRY void (*input_callback_cb)(void);

void real_input_callback(void)
{
	if (input_callback_cb)
		input_callback_cb();
}

ECL_EXPORT void dsda_set_input_callback(ECL_ENTRY void (*fecb)(void))
{
	input_callback_cb = fecb;
}

bool foundIWAD = false;

ECL_EXPORT int dsda_init(struct InitSettings *settings)
{
  // Creating arguments
  int argc = 0;
  char** argv = (char**) alloc_invisible (sizeof(char*) * 512);
  
  bool _noMonsters = false;
  bool _monstersRespawn = false;

  // Specifying executable name
  char arg0[] = "dsda";
  argv[argc++] = arg0;

  // Eliminating restrictions to TAS inputs
  if (settings->_StrictMode == 0)
  {
	char arg2[] = "-tas";
	argv[argc++] = arg2;
  }
  
  // Specifying skill level
  char arg3[] = "-skill";
  argv[argc++] = arg3;
  char argSkill[512];
  sprintf(argSkill, "%d", settings->_SkillLevel);
  argv[argc++] = argSkill;
  
  // Specifying episode and map
  char arg4[] = "-warp";
  argv[argc++] = arg4;
  char argEpisode[512];
  {
  	sprintf(argEpisode, "%d", settings->_InitialEpisode);
  	argv[argc++] = argEpisode;
  }
  char argMap[512];
  sprintf(argMap, "%d", settings->_InitialMap);
  argv[argc++] = argMap;
  
  // Specifying comp level
  char arg5[] = "-complevel";
  argv[argc++] = arg5;
  char argCompatibilityLevel[512];
  sprintf(argCompatibilityLevel, "%d", settings->_CompatibilityMode);
  argv[argc++] = argCompatibilityLevel;
  
  // Specifying fast monsters
  char arg6[] = "-fast";
  if (settings->_FastMonsters == 1) argv[argc++] = arg6;
  
  // Specifying monsters respawn
  char arg7[] = "-respawn";
  if (settings->_MonstersRespawn == 1) argv[argc++] = arg7;
  
  // Specifying no monsters
  char arg8[] = "-nomonsters";
  if (settings->_NoMonsters == 1) argv[argc++] = arg8;

  char arg9[] = "-chain_episodes";
  if (settings->_ChainEpisodes == 1) argv[argc++] = arg9;

  // Specifying Turbo
  char arg10[] = "-turbo";
  char argTurbo[512];
  if (settings->_Turbo >= 0)
  {
	sprintf(argTurbo, "%d", settings->_Turbo);
    argv[argc++] = arg10;
	argv[argc++] = argTurbo;
  } 

  printf("Passing arguments: \n");
  for (int i = 0; i < argc; i++) printf("%s ", argv[i]);
  printf("\n");

  // Setting players in game
  playeringame[0] = settings->_Player1Present;
  playeringame[1] = settings->_Player2Present;
  playeringame[2] = settings->_Player3Present;
  playeringame[3] = settings->_Player4Present;

  // Getting player count
  auto playerCount = settings->_Player1Present + settings->_Player2Present + settings->_Player3Present + settings->_Player4Present;
  char arg12[] = "-solo-net";
  if (playerCount > 1) argv[argc++] = arg12;

  // Set multiplayer mode
  char arg13[] = "-deathmatch";
  if (settings->_MultiplayerMode == 1) argv[argc++] = arg13;
  char arg14[] = "-altdeath";
  if (settings->_MultiplayerMode == 2) argv[argc++] = arg14;

  // Handle class
  PlayerClass[0] = (pclass_t)settings->_Player1Class;
  PlayerClass[1] = (pclass_t)settings->_Player2Class;
  PlayerClass[2] = (pclass_t)settings->_Player3Class;
  PlayerClass[3] = (pclass_t)settings->_Player4Class;

  // Initializing DSDA core
  headlessMain(argc, argv);
  printf("DSDA Initialized\n");

  // Initializing audio
  I_SetSoundCap();
  I_InitSound();
  printf("Audio Initialized\n");

  // If, required prevent level exit and game end triggers
  preventLevelExit = settings->_PreventLevelExit;
  preventGameEnd = settings->_PreventGameEnd;

  printf("Prevent Level Exit: %d\n", preventLevelExit);
  printf("Prevent Game End:   %d\n", preventGameEnd);

  // Enabling DSDA output, for debugging
  enableOutput = 1;

  return 1;
}


ECL_EXPORT int dsda_add_wad_file(const char *filename, const int size, ECL_ENTRY int (*feload_archive_cb)(const char *filename, unsigned char *buffer, int maxsize))
{
  printf("Loading WAD '%s' of size %d...\n", filename, size);
  auto wadFileBuffer = (unsigned char*) alloc_invisible(size);

  if (wadFileBuffer == NULL) { fprintf(stderr, "Error creating buffer. Do we have enough memory in the waterbox?\n"); return 0; }
  else printf("Created buffer at address: %p\n", wadFileBuffer);
  
  int loadSize = feload_archive_cb(filename, wadFileBuffer, size);
  if (loadSize != size) { fprintf(stderr, "Error loading '%s': read %d bytes, but expected %d bytes\n", filename, loadSize, size); return 0; }

  // Check size is enough
  if (size < 5) { fprintf(stderr, "Error loading '%s': read %d bytes, which is too small\n", filename, size); return 0; }

  // Getting wad header
  char header[5];
  header[0] = wadFileBuffer[0];
  header[1] = wadFileBuffer[1];
  header[2] = wadFileBuffer[2];
  header[3] = wadFileBuffer[3];
  header[4] = '\0';

  // Getting string
  std::string headerString(header);

  // Safety checks
  bool recognizedFormat = false;

  // Loading PWAD
  if (headerString == "PWAD")
  {
	recognizedFormat = true;

    // Loading PWAD
	D_AddFile(filename, source_pwad, wadFileBuffer, size);
	printf("Loaded PWAD '%s' correctly\n", filename);
  } 

  // Loading IWAD
  if (headerString == "IWAD")
  {
    recognizedFormat = true;

    // Checking for repeated IWAD
	if (foundIWAD == true) { fprintf(stderr, "Error with '%s': an IWAD was already loaded before\n", filename); return 0; }
	foundIWAD = true;

    // Loading IWAD
	printf("Loading IWAD '%s'...\n", filename);
	AddIWAD(filename, wadFileBuffer, size);
    printf("Loaded IWAD '%s' correctly\n", filename);
  } 
 
  // Checking for correct header
  if (recognizedFormat == false) { fprintf(stderr, "Error with '%s': it contains an unrecognized header '%s'\n", filename, header); return 0; }

  // Return 1 for all ok
  return 1;
}
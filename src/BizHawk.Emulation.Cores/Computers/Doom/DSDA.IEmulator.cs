using BizHawk.Emulation.Common;
using static BizHawk.Emulation.Cores.Computers.Doom.CInterface;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool renderVideo, bool renderAudio)
		{
			// Declaring inputs
			PackedPlayerInput player1Inputs = new PackedPlayerInput();
			PackedPlayerInput player2Inputs = new PackedPlayerInput();
			PackedPlayerInput player3Inputs = new PackedPlayerInput();
			PackedPlayerInput player4Inputs = new PackedPlayerInput();

			if (_syncSettings.Player1Present)
			{
				player1Inputs._RunSpeed = _controllerDeck.ReadPot1(controller, 0);
				player1Inputs._StrafingSpeed = _controllerDeck.ReadPot1(controller, 1);
				player1Inputs._TurningSpeed = _controllerDeck.ReadPot1(controller, 2);
				player1Inputs._WeaponSelect = _controllerDeck.ReadPot1(controller, 3);
				var actionsBitfield = _controllerDeck.ReadPort1(controller);
				player1Inputs._Fire = actionsBitfield & 0b00001;
				player1Inputs._Action = (actionsBitfield & 0b00010) >> 1;
				player1Inputs._AltWeapon = (actionsBitfield & 0b00100) >> 2;

				// Handling mouse-driven running
				int mouseRunningSpeed = _controllerDeck.ReadPot1(controller, 4);
				if (_player1LastMouseRunningValue > MOUSE_NO_INPUT)
				{
					int mouseRunningDelta = _player1LastMouseRunningValue - mouseRunningSpeed;
					player1Inputs._RunSpeed += mouseRunningDelta * _syncSettings.MouseRunSensitivity;
					if (player1Inputs._RunSpeed > 50) player1Inputs._RunSpeed = 50;
					if (player1Inputs._RunSpeed < -50) player1Inputs._RunSpeed = -50;
				}
				_player1LastMouseRunningValue = mouseRunningSpeed;

				// Handling mouse-driven turning
				int mouseTurningSpeed = _controllerDeck.ReadPot1(controller, 5);
				if (_player1LastMouseTurningValue > MOUSE_NO_INPUT)
				{
					int mouseTurningDelta = _player1LastMouseTurningValue - mouseTurningSpeed;
					player1Inputs._TurningSpeed += mouseTurningDelta * _syncSettings.MouseTurnSensitivity;
				}
				_player1LastMouseTurningValue = mouseTurningSpeed;

				// Raven Games
				if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
				{
					player1Inputs._FlyLook = _controllerDeck.ReadPot1(controller, 6);
					player1Inputs._ArtifactUse = _controllerDeck.ReadPot1(controller, 7);
					if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
					{
						player1Inputs._Jump = (actionsBitfield & 0b01000) >> 3;
						player1Inputs._EndPlayer = (actionsBitfield & 0b10000) >> 4;
					}
				}
			}

			if (_syncSettings.Player2Present)
			{
				player2Inputs._RunSpeed = _controllerDeck.ReadPot2(controller, 0);
				player2Inputs._StrafingSpeed = _controllerDeck.ReadPot2(controller, 1);
				player2Inputs._TurningSpeed = _controllerDeck.ReadPot2(controller, 2);
				player2Inputs._WeaponSelect = _controllerDeck.ReadPot2(controller, 3);
				var actionsBitfield = _controllerDeck.ReadPort2(controller);
				player2Inputs._Fire = actionsBitfield & 0b00001;
				player2Inputs._Action = (actionsBitfield & 0b00010) >> 1;
				player2Inputs._AltWeapon = (actionsBitfield & 0b00100) >> 2;

				// Handling mouse-driven running
				int mouseRunningSpeed = _controllerDeck.ReadPot2(controller, 4);
				if (_player2LastMouseRunningValue > MOUSE_NO_INPUT)
				{
					int mouseRunningDelta = _player2LastMouseRunningValue - mouseRunningSpeed;
					player2Inputs._RunSpeed += mouseRunningDelta * _syncSettings.MouseRunSensitivity;
					if (player2Inputs._RunSpeed > 50) player2Inputs._RunSpeed = 50;
					if (player2Inputs._RunSpeed < -50) player2Inputs._RunSpeed = -50;
				}
				_player2LastMouseRunningValue = mouseRunningSpeed;

				// Handling mouse-driven turning
				int mouseTurningSpeed = _controllerDeck.ReadPot2(controller, 5);
				if (_player2LastMouseTurningValue > MOUSE_NO_INPUT)
				{
					int mouseTurningDelta = _player2LastMouseTurningValue - mouseTurningSpeed;
					player2Inputs._TurningSpeed += mouseTurningDelta * _syncSettings.MouseTurnSensitivity;
				}
				_player2LastMouseTurningValue = mouseTurningSpeed;

				// Raven Games
				if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
				{
					player2Inputs._FlyLook = _controllerDeck.ReadPot2(controller, 4);
					player2Inputs._ArtifactUse = _controllerDeck.ReadPot2(controller, 5);
					if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
					{
						player2Inputs._Jump = (actionsBitfield & 0b01000) >> 3;
						player2Inputs._EndPlayer = (actionsBitfield & 0b10000) >> 4;
					}
				}
			}

			if (_syncSettings.Player3Present)
			{
				player3Inputs._RunSpeed = _controllerDeck.ReadPot3(controller, 0);
				player3Inputs._StrafingSpeed = _controllerDeck.ReadPot3(controller, 1);
				player3Inputs._TurningSpeed = _controllerDeck.ReadPot3(controller, 2);
				player3Inputs._WeaponSelect = _controllerDeck.ReadPot3(controller, 3);
				var actionsBitfield = _controllerDeck.ReadPort3(controller);
				player3Inputs._Fire = actionsBitfield & 0b00001;
				player3Inputs._Action = (actionsBitfield & 0b00010) >> 1;
				player3Inputs._AltWeapon = (actionsBitfield & 0b00100) >> 2;

				// Handling mouse-driven running
				int mouseRunningSpeed = _controllerDeck.ReadPot3(controller, 4);
				if (_player3LastMouseRunningValue > MOUSE_NO_INPUT)
				{
					int mouseRunningDelta = _player3LastMouseRunningValue - mouseRunningSpeed;
					player3Inputs._RunSpeed += mouseRunningDelta * _syncSettings.MouseRunSensitivity;
					if (player3Inputs._RunSpeed > 50) player3Inputs._RunSpeed = 50;
					if (player3Inputs._RunSpeed < -50) player3Inputs._RunSpeed = -50;
				}
				_player3LastMouseRunningValue = mouseRunningSpeed;

				// Handling mouse-driven turning
				int mouseTurningSpeed = _controllerDeck.ReadPot3(controller, 5);
				if (_player3LastMouseTurningValue > MOUSE_NO_INPUT)
				{
					int mouseTurningDelta = _player3LastMouseTurningValue - mouseTurningSpeed;
					player3Inputs._TurningSpeed += mouseTurningDelta * _syncSettings.MouseTurnSensitivity;
				}
				_player3LastMouseTurningValue = mouseTurningSpeed;

				// Raven Games
				if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
				{
					player3Inputs._FlyLook = _controllerDeck.ReadPot3(controller, 6);
					player3Inputs._ArtifactUse = _controllerDeck.ReadPot3(controller, 7);
					if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
					{
						player3Inputs._Jump = (actionsBitfield & 0b01000) >> 3;
						player3Inputs._EndPlayer = (actionsBitfield & 0b10000) >> 4;
					}
				}
			}

			if (_syncSettings.Player4Present)
			{
				player4Inputs._RunSpeed = _controllerDeck.ReadPot4(controller, 0);
				player4Inputs._StrafingSpeed = _controllerDeck.ReadPot4(controller, 1);
				player4Inputs._TurningSpeed = _controllerDeck.ReadPot4(controller, 2);
				player4Inputs._WeaponSelect = _controllerDeck.ReadPot4(controller, 3);
				var actionsBitfield = _controllerDeck.ReadPort4(controller);
				player4Inputs._Fire = actionsBitfield & 0b00001;
				player4Inputs._Action = (actionsBitfield & 0b00010) >> 1;
				player4Inputs._AltWeapon = (actionsBitfield & 0b00100) >> 2;

				// Handling mouse-driven running
				int mouseRunningSpeed = _controllerDeck.ReadPot4(controller, 4);
				if (_player4LastMouseRunningValue > MOUSE_NO_INPUT)
				{
					int mouseRunningDelta = _player4LastMouseRunningValue - mouseRunningSpeed;
					player4Inputs._RunSpeed += mouseRunningDelta * _syncSettings.MouseRunSensitivity;
					if (player4Inputs._RunSpeed > 50) player4Inputs._RunSpeed = 50;
					if (player4Inputs._RunSpeed < -50) player4Inputs._RunSpeed = -50;
				}
				_player4LastMouseRunningValue = mouseRunningSpeed;

				// Handling mouse-driven turning
				int mouseTurningSpeed = _controllerDeck.ReadPot4(controller, 5);
				if (_player4LastMouseTurningValue > MOUSE_NO_INPUT)
				{
					int mouseTurningDelta = _player4LastMouseTurningValue - mouseTurningSpeed;
					player4Inputs._TurningSpeed += mouseTurningDelta * _syncSettings.MouseTurnSensitivity;
				}
				_player4LastMouseTurningValue = mouseTurningSpeed;

				// Raven Games
				if (_syncSettings.InputFormat is DoomControllerTypes.Heretic or DoomControllerTypes.Hexen)
				{
					player4Inputs._FlyLook = _controllerDeck.ReadPot4(controller, 4);
					player4Inputs._ArtifactUse = _controllerDeck.ReadPot4(controller, 5);
					if (_syncSettings.InputFormat is DoomControllerTypes.Hexen)
					{
						player4Inputs._Jump = (actionsBitfield & 0b01000) >> 3;
						player4Inputs._EndPlayer = (actionsBitfield & 0b10000) >> 4;
					}
				}
			}

			PackedRenderInfo renderInfo = new PackedRenderInfo();
			renderInfo._RenderVideo = renderVideo ? 1 : 0;
			renderInfo._RenderAudio = renderAudio ? 1 : 0;
			renderInfo._PlayerPointOfView = _settings.DisplayPlayer - 1;

			Core.dsda_frame_advance(
				ref player1Inputs,
				ref player2Inputs,
				ref player3Inputs,
				ref player4Inputs,
				ref renderInfo);

			if (renderVideo)
				UpdateVideo();

			if (renderAudio)
				UpdateAudio();

			Frame++;

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId => VSystemID.Raw.Doom;

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			Frame = 0;
		}

		public void Dispose()
		{
			_elf.Dispose();
		}
	}
}

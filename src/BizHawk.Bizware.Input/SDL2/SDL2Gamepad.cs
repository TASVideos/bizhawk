using System;
using System.Collections.Generic;

using static SDL2.SDL;

namespace BizHawk.Bizware.Input
{
	/// <summary>
	/// SDL2 Gamepad Handler
	/// </summary>
	internal class SDL2Gamepad : IDisposable
	{
		// indexed by instance id
		private static readonly Dictionary<int, SDL2Gamepad> Gamepads = new();

		private readonly IntPtr Opaque;

		/// <summary>Is an SDL_GameController rather than an SDL_Joystick</summary>
		public readonly bool IsGameController;

		/// <summary>Has rumble</summary>
		public readonly bool HasRumble;

		/// <summary>Contains name and delegate function for all buttons, hats and axis</summary>
		public readonly IReadOnlyCollection<(string ButtonName, Func<bool> GetIsPressed)> ButtonGetters;
		
		/// <summary>For use in keybind boxes</summary>
		public string InputNamePrefix { get; private set; }

		/// <summary>Device index in SDL</summary>
		public int DeviceIndex { get; private set; }

		/// <summary>Instance ID in SDL</summary>
		public int InstanceID { get; }

		/// <summary>Device name in SDL</summary>
		public string DeviceName { get; }

		public static void Deinitialize()
		{
			foreach (var gamepad in Gamepads.Values)
			{
				gamepad.Dispose();
			}

			Gamepads.Clear();
		}

		public void Dispose()
		{
			Console.WriteLine($"Disconnecting SDL gamepad, device index {DeviceIndex}, instance ID {InstanceID}, name {DeviceName}");

			if (IsGameController)
			{
				SDL_GameControllerClose(Opaque);
			}
			else
			{
				SDL_JoystickClose(Opaque);
			}
		}

		private static void RefreshIndexes()
		{
			var njoysticks = SDL_NumJoysticks();
			for (var i = 0; i < njoysticks; i++)
			{
				var joystickId = SDL_JoystickGetDeviceInstanceID(i);
				if (Gamepads.TryGetValue(joystickId, out var gamepad))
				{
					gamepad.UpdateIndex(i);
				}
			}
		}

		public static void AddDevice(int deviceIndex)
		{
			var instanceId = SDL_JoystickGetDeviceInstanceID(deviceIndex);
			if (!Gamepads.ContainsKey(instanceId))
			{
				var gamepad = new SDL2Gamepad(deviceIndex);
				Gamepads.Add(SDL_JoystickGetDeviceInstanceID(deviceIndex), gamepad);
			}
			else
			{
				Console.WriteLine($"Gamepads contained a joystick with instance ID {instanceId}, ignoring add device event");
			}

			RefreshIndexes();
		}

		public static void RemoveDevice(int deviceInstanceId)
		{
			if (Gamepads.TryGetValue(deviceInstanceId, out var gamepad))
			{
				gamepad.Dispose();
				Gamepads.Remove(deviceInstanceId);
			}
			else
			{
				Console.WriteLine($"Gamepads did not contain a joystick with instance ID {deviceInstanceId}, ignoring remove device event");
			}

			RefreshIndexes();
		}

		public static IEnumerable<SDL2Gamepad> EnumerateDevices()
			=> Gamepads.Values;

		private List<(string ButtonName, Func<bool> GetIsPressed)> CreateGameControllerButtonGetters()
		{
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = new();

			const int dzp = 20000;
			const int dzn = -20000;
			const int dzt = 5000;

			bool GetSDLButton(SDL_GameControllerButton button)
			{
				var ret = SDL_GameControllerGetButton(Opaque, button);
				if (ret == 0xFF)
				{
					Console.WriteLine($"SDL error when reading {button}, SDL error {SDL_GetError()}");
				}

				return ret == 1;
			}

			// buttons
			buttonGetters.Add(("A", () =>GetSDLButton(SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_A)));
			buttonGetters.Add(("B", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_B)));
			buttonGetters.Add(("X", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_X)));
			buttonGetters.Add(("Y", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_Y)));
			buttonGetters.Add(("Back", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_BACK)));
			buttonGetters.Add(("Guide", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_GUIDE)));
			buttonGetters.Add(("Start", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_START)));
			buttonGetters.Add(("LeftThumb", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSTICK)));
			buttonGetters.Add(("RightThumb", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSTICK)));
			buttonGetters.Add(("LeftShoulder", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_LEFTSHOULDER)));
			buttonGetters.Add(("RightShoulder", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_RIGHTSHOULDER)));
			buttonGetters.Add(("DpadUp", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_UP)));
			buttonGetters.Add(("DpadDown", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_DOWN)));
			buttonGetters.Add(("DpadLeft", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_LEFT)));
			buttonGetters.Add(("DpadRight", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_DPAD_RIGHT)));
			buttonGetters.Add(("Misc", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_MISC1)));
			buttonGetters.Add(("Paddle1", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE1)));
			buttonGetters.Add(("Paddle2", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE2)));
			buttonGetters.Add(("Paddle3", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE3)));
			buttonGetters.Add(("Paddle4", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_PADDLE4)));
			buttonGetters.Add(("Touchpad", () => GetSDLButton( SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_TOUCHPAD)));

			// sticks
			buttonGetters.Add(("LStickUp", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) >= dzp));
			buttonGetters.Add(("LStickDown", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY) <= dzn));
			buttonGetters.Add(("LStickLeft", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) <= dzn));
			buttonGetters.Add(("LStickRight", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX) >= dzp));
			buttonGetters.Add(("RStickUp", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) >= dzp));
			buttonGetters.Add(("RStickDown", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY) <= dzn));
			buttonGetters.Add(("RStickLeft", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) <= dzn));
			buttonGetters.Add(("RStickRight", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX) >= dzp));

			// triggers
			buttonGetters.Add(("LeftTrigger", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT) > dzt));
			buttonGetters.Add(("RightTrigger", () => SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT) > dzt));

			return buttonGetters;
		}

		private List<(string ButtonName, Func<bool> GetIsPressed)> CreateJoystickButtonGetters()
		{
			List<(string ButtonName, Func<bool> GetIsPressed)> buttonGetters = new();

			const float dzp = 20000;
			const float dzn = -20000;

			// axes
			buttonGetters.Add(("X+", () => SDL_JoystickGetAxis(Opaque, 0) >= dzp));
			buttonGetters.Add(("X-", () => SDL_JoystickGetAxis(Opaque, 0) <= dzn));
			buttonGetters.Add(("Y+", () => SDL_JoystickGetAxis(Opaque, 1) >= dzp));
			buttonGetters.Add(("Y-", () => SDL_JoystickGetAxis(Opaque, 1) <= dzn));
			buttonGetters.Add(("Z+", () => SDL_JoystickGetAxis(Opaque, 2) >= dzp));
			buttonGetters.Add(("Z-", () => SDL_JoystickGetAxis(Opaque, 2) <= dzn));
			buttonGetters.Add(("W+", () => SDL_JoystickGetAxis(Opaque, 3) >= dzp));
			buttonGetters.Add(("W-", () => SDL_JoystickGetAxis(Opaque, 3) <= dzn));
			buttonGetters.Add(("V+", () => SDL_JoystickGetAxis(Opaque, 4) >= dzp));
			buttonGetters.Add(("V-", () => SDL_JoystickGetAxis(Opaque, 4) <= dzn));
			buttonGetters.Add(("S+", () => SDL_JoystickGetAxis(Opaque, 5) >= dzp));
			buttonGetters.Add(("S-", () => SDL_JoystickGetAxis(Opaque, 5) <= dzn));
			buttonGetters.Add(("Q+", () => SDL_JoystickGetAxis(Opaque, 6) >= dzp));
			buttonGetters.Add(("Q-", () => SDL_JoystickGetAxis(Opaque, 6) <= dzn));
			buttonGetters.Add(("P+", () => SDL_JoystickGetAxis(Opaque, 7) >= dzp));
			buttonGetters.Add(("P-", () => SDL_JoystickGetAxis(Opaque, 7) <= dzn));
			buttonGetters.Add(("N+", () => SDL_JoystickGetAxis(Opaque, 8) >= dzp));
			buttonGetters.Add(("N-", () => SDL_JoystickGetAxis(Opaque, 8) <= dzn));
			var naxes = SDL_JoystickNumAxes(Opaque);
			for (var i = 9; i < naxes; i++)
			{
				var j = i;
				buttonGetters.Add(($"Axis{j.ToString()}+", () => SDL_JoystickGetAxis(Opaque, j) >= dzp));
				buttonGetters.Add(($"Axis{j.ToString()}-", () => SDL_JoystickGetAxis(Opaque, j) <= dzn));
			}

			// buttons
			var nbuttons = SDL_JoystickNumButtons(Opaque);
			for (var i = 0; i < nbuttons; i++)
			{
				var j = i;
				buttonGetters.Add(($"B{i + 1}", () => SDL_JoystickGetButton(Opaque, j) == 1));
			}

			// hats
			var nhats = SDL_JoystickNumHats(Opaque);
			for (var i = 0; i < nhats; i++)
			{
				var j = i;
				buttonGetters.Add(($"POV{j.ToString()}U", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_UP) == SDL_HAT_UP));
				buttonGetters.Add(($"POV{j.ToString()}D", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_DOWN) == SDL_HAT_DOWN));
				buttonGetters.Add(($"POV{j.ToString()}L", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_LEFT) == SDL_HAT_LEFT));
				buttonGetters.Add(($"POV{j.ToString()}R", () => (SDL_JoystickGetHat(Opaque, j) & SDL_HAT_RIGHT) == SDL_HAT_RIGHT));
			}

			return buttonGetters;
		}

		public void UpdateIndex(int index)
		{
			InputNamePrefix = IsGameController
				? $"X{index + 1} "
				: $"J{index + 1} ";
			DeviceIndex = index;
		}

		private SDL2Gamepad(int index)
		{
			if (SDL_IsGameController(index) == SDL_bool.SDL_TRUE)
			{
				Opaque = SDL_GameControllerOpen(index);
				HasRumble = SDL_GameControllerHasRumble(Opaque) == SDL_bool.SDL_TRUE;
				ButtonGetters = CreateGameControllerButtonGetters();
				IsGameController = true;
				InputNamePrefix = $"X{index + 1} ";
				DeviceName = SDL_GameControllerName(Opaque);
			}
			else
			{
				Opaque = SDL_JoystickOpen(index);
				HasRumble = SDL_JoystickHasRumble(Opaque) == SDL_bool.SDL_TRUE;
				ButtonGetters = CreateJoystickButtonGetters();
				IsGameController = false;
				InputNamePrefix = $"J{index + 1} ";
				DeviceName = SDL_JoystickName(Opaque);
			}

			DeviceIndex = index;
			InstanceID = SDL_JoystickGetDeviceInstanceID(index);

			Console.WriteLine($"Connected SDL gamepad, device index {index}, instance ID {InstanceID}, name {DeviceName}");
		}

		public IEnumerable<(string AxisID, int Value)> GetAxes()
		{
			//constant for adapting a +/- 32768 range to a +/-10000-based range
			const float f = 32768 / 10000.0f;
			static int Conv(short num) => (int)(num / f);

			if (IsGameController)
			{
				return new[]
				{
					("LeftThumbX", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTX))),
					("LeftThumbY", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_LEFTY))),
					("RightThumbX", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTX))),
					("RightThumbY", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_RIGHTY))),
					("LeftTrigger", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERLEFT))),
					("RightTrigger", Conv(SDL_GameControllerGetAxis(Opaque, SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_TRIGGERRIGHT))),
				};
			}

			List<(string AxisID, int Value)> values = new()
			{
				("X", Conv(SDL_JoystickGetAxis(Opaque, 0))),
				("Y", Conv(SDL_JoystickGetAxis(Opaque, 1))),
				("Z", Conv(SDL_JoystickGetAxis(Opaque, 2))),
				("W", Conv(SDL_JoystickGetAxis(Opaque, 3))),
				("V", Conv(SDL_JoystickGetAxis(Opaque, 4))),
				("S", Conv(SDL_JoystickGetAxis(Opaque, 5))),
				("Q", Conv(SDL_JoystickGetAxis(Opaque, 6))),
				("P", Conv(SDL_JoystickGetAxis(Opaque, 7))),
				("N", Conv(SDL_JoystickGetAxis(Opaque, 8))),
			};

			var naxes = SDL_JoystickNumAxes(Opaque);
			for (var i = 9; i < naxes; i++)
			{
				var j = i;
				values.Add(($"Axis{j.ToString()}", Conv(SDL_JoystickGetAxis(Opaque, j))));
			}

			return values;
		}

		/// <remarks><paramref name="left"/> and <paramref name="right"/> are in 0..<see cref="int.MaxValue"/></remarks>
		public void SetVibration(int left, int right)
		{
			static ushort Conv(int i) => unchecked((ushort) ((i >> 15) & 0xFFFF));
			_ = IsGameController
				? SDL_GameControllerRumble(Opaque, Conv(left), Conv(right), uint.MaxValue)
				: SDL_JoystickRumble(Opaque, Conv(left), Conv(right), uint.MaxValue);
		}
	}
}


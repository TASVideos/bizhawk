using System;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	public readonly struct FirmwareID
	{
		public static bool operator ==(FirmwareID a, FirmwareID b) => a.Firmware == b.Firmware && a.System == b.System;

		public static bool operator !=(FirmwareID a, FirmwareID b) => a.Firmware != b.Firmware || a.System != b.System;

		public string ConfigKey => $"{System}+{Firmware}";

		public readonly string Firmware;

		public string MovieHeaderKey => $"{System}_Firmware_{Firmware}";

		public readonly string System;

		public FirmwareID(string system, string firmware)
		{
			static bool IsAllowedCharacter(char c)
				=> c is '-' or (>= '0' and <= '9') or (>= 'A' and <= 'Z') or '_' or (>= 'a' and <= 'z');
			const string ERR_MSG_INVALID_CHAR = "FWIDs must match /[-0-9A-Z_a-z]+/";
			if (!system.All(IsAllowedCharacter)) throw new ArgumentOutOfRangeException(paramName: system, actualValue: system, message: ERR_MSG_INVALID_CHAR);
			if (!firmware.All(IsAllowedCharacter)) throw new ArgumentOutOfRangeException(paramName: firmware, actualValue: firmware, message: ERR_MSG_INVALID_CHAR);
			System = system;
			Firmware = firmware;
		}

		public override bool Equals(object? obj) => obj is FirmwareID other
			&& other.Firmware == Firmware && other.System == System;

		public override int GetHashCode() => (System, Firmware).GetHashCode();

		public override string ToString() => ConfigKey;
	}
}

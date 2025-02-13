using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : IStatable
	{
		public bool AvoidRewind => false;

		public void LoadStateBinary(BinaryReader reader)
		{
			_elf.LoadStateBinary(reader);
			
			// Getting last mouse positions
			_player1LastMouseRunningValue = reader.ReadInt32();
			_player1LastMouseTurningValue = reader.ReadInt32();
			_player2LastMouseRunningValue = reader.ReadInt32();
			_player2LastMouseTurningValue = reader.ReadInt32();
			_player3LastMouseRunningValue = reader.ReadInt32();
			_player3LastMouseTurningValue = reader.ReadInt32();
			_player4LastMouseRunningValue = reader.ReadInt32();
			_player4LastMouseTurningValue = reader.ReadInt32();

			Frame = reader.ReadInt32();
			// any managed pointers that we sent to the core need to be resent now!
			//Core.stella_set_input_callback(_inputCallback);
			UpdateVideo();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			_elf.SaveStateBinary(writer);

			// Writing last mouse positions
			writer.Write(_player1LastMouseRunningValue);
			writer.Write(_player1LastMouseTurningValue);
			writer.Write(_player2LastMouseRunningValue);
			writer.Write(_player2LastMouseTurningValue);
			writer.Write(_player3LastMouseRunningValue);
			writer.Write(_player3LastMouseTurningValue);
			writer.Write(_player4LastMouseRunningValue);
			writer.Write(_player4LastMouseTurningValue);

			writer.Write(Frame);
		}
	}
}

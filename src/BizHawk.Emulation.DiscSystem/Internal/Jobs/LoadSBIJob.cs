using System.IO;
using System.Collections.Generic;

using BizHawk.Common.IOExtensions;

namespace BizHawk.Emulation.DiscSystem.SBI
{
	/// <summary>
	/// Loads SBI files into an internal representation.
	/// </summary>
	internal class LoadSBIJob : DiscJob
	{
		private readonly string IN_Path;

		/// <param name="path">The file to be loaded</param>
		public LoadSBIJob(string path) => IN_Path = path;

		/// <summary>
		/// The resulting interpreted data
		/// </summary>
		public SubQPatchData OUT_Data { get; private set; }

		/// <exception cref="SBIParseException">file at <see cref="IN_Path"/> does not contain valid header or contains misformatted record</exception>
		public override void Run()
		{
			using var fs = File.OpenRead(IN_Path);
			BinaryReader br = new(fs);
			string sig = br.ReadStringFixedUtf8(4);
			if (sig != "SBI\0")
				throw new SBIParseException("Missing magic number");

			SubQPatchData ret = new();
			List<short> bytes = new();

			//read records until done
			for (; ; )
			{
				//graceful end
				if (fs.Position == fs.Length)
					break;

				if (fs.Position + 4 > fs.Length) throw new SBIParseException("Broken record");
				int m = BCD2.BCDToInt(br.ReadByte());
				int s = BCD2.BCDToInt(br.ReadByte());
				int f = BCD2.BCDToInt(br.ReadByte());
				Timestamp ts = new(m, s, f);
				ret.ABAs.Add(ts.Sector);
				int type = br.ReadByte();
				switch (type)
				{
					case 1: //Q0..Q9
						if (fs.Position + 10 > fs.Length) throw new SBIParseException("Broken record");
						for (int i = 0; i <= 9; i++) bytes.Add(br.ReadByte());
						for (int i = 10; i <= 11; i++) bytes.Add(-1);
						break;
					case 2: //Q3..Q5
						if (fs.Position + 3 > fs.Length) throw new SBIParseException("Broken record");
						for (int i = 0; i <= 2; i++) bytes.Add(-1);
						for (int i = 3; i <= 5; i++) bytes.Add(br.ReadByte());
						for (int i = 6; i <= 11; i++) bytes.Add(-1);
						break;
					case 3: //Q7..Q9
						if (fs.Position + 3 > fs.Length) throw new SBIParseException("Broken record");
						for (int i = 0; i <= 6; i++) bytes.Add(-1);
						for (int i = 7; i <= 9; i++) bytes.Add(br.ReadByte());
						for (int i = 10; i <= 11; i++) bytes.Add(-1);
						break;
					default:
						throw new SBIParseException("Broken record");
				}
			}

			ret.subq = bytes.ToArray();

			OUT_Data = ret;
		}
	}
}
#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using BizHawk.Common.IOExtensions;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	public unsafe class Serializer
	{
		public Serializer() { }

		public bool IsReader => _isReader;

		public bool IsWriter => !IsReader;

		public bool IsText => _isText;

		public BinaryReader BinaryReader => _br;

		public BinaryWriter BinaryWriter => _bw;

		public TextReader TextReader => _tr;

		public TextWriter TextWriter => _tw;

		public Serializer(BinaryWriter bw)
		{
			StartWrite(bw);
		}

		public Serializer(BinaryReader br)
		{
			StartRead(br);
		}

		public Serializer(TextWriter tw)
		{
			StartWrite(tw);
		}

		public Serializer(TextReader tr)
		{
			StartRead(tr);
		}

		public static Serializer CreateBinaryWriter(BinaryWriter bw)
		{
			return new Serializer(bw);
		}

		public static Serializer CreateBinaryReader(BinaryReader br)
		{
			return new Serializer(br);
		}

		public static Serializer CreateTextWriter(TextWriter tw)
		{
			return new Serializer(tw);
		}

		public static Serializer CreateTextReader(TextReader tr)
		{
			return new Serializer(tr);
		}

		public void StartWrite(BinaryWriter bw)
		{
			_bw = bw;
			_isReader = false;
		}

		public void StartRead(BinaryReader br)
		{
			_br = br;
			_isReader = true;
		}

		public void StartWrite(TextWriter tw)
		{
			_tw = tw;
			_isReader = false;
			_isText = true;
		}

		public void StartRead(TextReader tr)
		{
			_tr = tr;
			_isReader = true;
			_isText = true;
			BeginTextBlock();
		}

		public void BeginSection(string name)
		{
			_sections.Push(name);
			if (IsText)
			{
				if (IsWriter)
				{
					_tw.WriteLine("[{0}]", name);
				}
				else
				{
					_sectionStack.Push(_currSection);
					_currSection = _currSection[name];
				}
			}
		}

		public void EndSection()
		{
			var name = _sections.Pop();
			if (IsText)
			{
				if (IsWriter)
				{
					_tw.WriteLine("[/{0}]", name);
				}
				else
				{
					_currSection = _sectionStack.Pop();
				}
			}
		}

		/// <exception cref="InvalidOperationException"><typeparamref name="T"/> does not inherit <see cref="Enum"/></exception>
		public void SyncEnum<T>(string name, ref T val) where T : struct
		{
			if (typeof(T).BaseType != typeof(Enum))
			{
				throw new InvalidOperationException();
			}
			
			if (_isText)
			{
				SyncEnumText(name, ref val);
			}
			else if (IsReader)
			{
				val = (T)Enum.ToObject(typeof(T), _br.ReadInt32());
			}
			else
			{
				_bw.Write(Convert.ToInt32(val));
			}
		}

		public void SyncEnumText<T>(string name, ref T val) where T : struct
		{
			if (IsReader)
			{
				if (Present(name))
				{
					val = (T)Enum.Parse(typeof(T), Item(name));
				}
			}
			else
			{
				_tw.WriteLine("{0} {1}", name, val);
			}
		}

		public void Sync(string name, ref byte[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(useNull);
			}
			else
			{
				_bw.WriteByteBuffer(val);
			}
		}

		public void SyncText(string name, ref byte[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					val = Item(name).HexStringToBytes();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<byte>();
				_tw.WriteLine("{0} {1}", name, temp.BytesToHexString());
			}
		}

		public void Sync(string name, ref bool[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToBoolBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<bool>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref bool[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToBoolBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<bool>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}
		public void Sync(string name, ref short[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToShortBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<short>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void Sync(string name, ref ushort[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToUShortBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<ushort>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref short[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToShortBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<short>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void SyncText(string name, ref ushort[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToUShortBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<ushort>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref int[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToIntBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<int>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref int[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToIntBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<int>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref uint[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToUIntBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<uint>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref uint[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToUIntBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<uint>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref float[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToFloatBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<float>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref float[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToFloatBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<float>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref double[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = _br.ReadByteBuffer(false).ToDoubleBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<double>();
				}
			}
			else
			{
				_bw.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref double[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Item(name).HexStringToBytes();
					val = bytes.ToDoubleBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? Array.Empty<double>();
				_tw.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref Bit val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void SyncText(string name, ref Bit val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		public void Sync(string name, ref byte val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref ushort val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref uint val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref sbyte val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref short val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref int val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref long val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref ulong val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref float val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref double val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref bool val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		/// <exception cref="InvalidOperationException"><see cref="IsReader"/> is <see langword="false"/> and <paramref name="name"/> is longer than <paramref name="length"/> chars</exception>
		public void SyncFixedString(string name, ref string val, int length)
		{
			// TODO - this could be made more efficient perhaps just by writing values right out of the string..
			if (IsReader)
			{
				var buf = new char[length];
				if (_isText)
				{
					_tr.Read(buf, 0, length);
				}
				else
				{
					_br.Read(buf, 0, length);
				}

				var len = 0;
				for (; len < length; len++)
				{
					if (buf[len] == 0)
					{
						break;
					}
				}

				val = new string(buf, 0, len);
			}
			else
			{
				if (name.Length > length)
				{
					throw new InvalidOperationException($"{nameof(SyncFixedString)} too long");
				}

				var buf = val.ToCharArray();
				var remainder = new char[length - buf.Length];
				if (IsText)
				{
					_tw.Write(buf);
					_tw.Write(remainder);
				}
				else
				{
					_bw.Write(buf);
					_bw.Write(remainder);
				}
			}
		}

		public void SyncDelta<T>(string name, T[] original, T[] current)
			where T : unmanaged
		{
			if (IsReader)
			{
				var delta = Array.Empty<byte>();
				Sync(name, ref delta, useNull: false);
				DeltaSerializer.ApplyDelta<T>(original, current, delta);
			}
			else
			{
				var delta = DeltaSerializer.GetDelta<T>(original, current).ToArray(); // TODO: don't create array here (need .net update to write span to binary writer)
				Sync(name, ref delta, useNull: false);
			}
		}

		private BinaryReader _br;
		private BinaryWriter _bw;
		private TextReader _tr;
		private TextWriter _tw;

		private bool _isText;
		private bool _isReader;
		private readonly Stack<string> _sections = new Stack<string>();
		private Section _readerSection, _currSection;
		private readonly Stack<Section> _sectionStack = new Stack<Section>();

		private void BeginTextBlock()
		{
			if (!IsText || IsWriter)
			{
				return;
			}

			_readerSection = new Section();
			var ss = new Stack<Section>();
			ss.Push(_readerSection);
			var curs = _readerSection;

			var rxEnd = new System.Text.RegularExpressions.Regex(@"\[/(.*?)\]", System.Text.RegularExpressions.RegexOptions.Compiled);
			var rxBegin = new System.Text.RegularExpressions.Regex(@"\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Compiled);

			// read the entire file into a data structure for flexi-parsing
			string str;
			while ((str = _tr.ReadLine()) != null)
			{
				var end = rxEnd.Match(str);
				var begin = rxBegin.Match(str);
				if (end.Success)
				{
					var name = end.Groups[1].Value;
					if (name != curs.Name)
					{
						throw new InvalidOperationException("Mis-formed savestate blob");
					}

					curs = ss.Pop();
					
					// consume no data past the end of the last proper section
					if (curs == _readerSection)
					{
						_currSection = curs;
						return;
					}
				}
				else if (begin.Success)
				{
					var name = begin.Groups[1].Value;
					ss.Push(curs);
					var news = new Section { Name = name };
					curs.Add(name, news);
					curs = news;
				}
				else
				{
					// add to current section
					if (str.Trim().Length == 0)
					{
						continue;
					}

					var parts = str.Split(' ');
					var key = parts[0];

					// UGLY: adds whole string instead of splitting the key. later, split the key, and have the individual Sync methods give up that responsibility
					curs.Items.Add(key, parts[1]);
				}
			}

			_currSection = _readerSection;
		}

		private string Item(string key)
		{
			return _currSection.Items[key];
		}

		private bool Present(string key)
		{
			return _currSection.Items.ContainsKey(key);
		}

		private void SyncBuffer(string name, int elemsize, int len, void* ptr)
		{
			if (IsReader)
			{
				byte[] temp = null;
				Sync(name, ref temp, false);
				int todo = Math.Min(temp.Length, len * elemsize);
				System.Runtime.InteropServices.Marshal.Copy(temp, 0, new IntPtr(ptr), todo);
			}
			else
			{
				int todo = len * elemsize;
				var temp = new byte[todo];
				System.Runtime.InteropServices.Marshal.Copy(new IntPtr(ptr), temp, 0, todo);
				Sync(name, ref temp, false);
			}
		}

		private void SyncText(string name, ref byte val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref ushort val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref uint val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref sbyte val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref short val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref int val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref long val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref ulong val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref float val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref double val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref bool val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void Read(ref Bit val)
		{
			val = _br.ReadBit();
		}

		private void Write(ref Bit val)
		{
			_bw.WriteBit(val);
		}

		private void ReadText(string name, ref Bit val)
		{
			if (Present(name))
			{
				val = int.Parse(Item(name));
			}
		}

		private void WriteText(string name, ref Bit val)
		{
			_tw.WriteLine("{0} {1}", name, (int)val);
		}

		private void Read(ref byte val)
		{
			val = _br.ReadByte();
		}

		private void Write(ref byte val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref byte val)
		{
			if (Present(name))
			{
				val = byte.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}
		private void WriteText(string name, ref byte val)
		{
			_tw.WriteLine("{0} 0x{1:X2}", name, val);
		}

		private void Read(ref ushort val)
		{
			val = _br.ReadUInt16();
		}

		private void Write(ref ushort val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref ushort val)
		{
			if (Present(name))
			{
				val = ushort.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref ushort val)
		{
			_tw.WriteLine("{0} 0x{1:X4}", name, val);
		}

		private void Read(ref uint val)
		{
			{ val = _br.ReadUInt32(); }
		}

		private void Write(ref uint val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref uint val)
		{
			if (Present(name))
			{
				val = uint.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref uint val)
		{
			_tw.WriteLine("{0} 0x{1:X8}", name, val);
		}

		private void Read(ref sbyte val)
		{
			val = _br.ReadSByte();
		}

		private void Write(ref sbyte val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref sbyte val)
		{
			if (Present(name))
			{
				val = sbyte.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref sbyte val)
		{
			_tw.WriteLine("{0} 0x{1:X2}", name, val);
		}

		private void Read(ref short val)
		{
			val = _br.ReadInt16();
		}

		private void Write(ref short val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref short val)
		{
			if (Present(name))
			{
				val = short.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref short val)
		{
			_tw.WriteLine("{0} 0x{1:X4}", name, val);
		}

		private void Read(ref int val)
		{
			val = _br.ReadInt32();
		}

		private void Write(ref int val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref int val)
		{
			if (Present(name))
			{
				val = int.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref int val)
		{
			_tw.WriteLine("{0} 0x{1:X8}", name, val);
		}

		private void Read(ref long val)
		{
			val = _br.ReadInt64();
		}

		private void Write(ref long val)
		{
			_bw.Write(val);
		}

		private void Read(ref ulong val)
		{
			val = _br.ReadUInt64();
		}

		private void Write(ref ulong val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref long val)
		{
			if (Present(name))
			{
				val = long.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref long val)
		{
			_tw.WriteLine("{0} 0x{1:X16}", name, val);
		}

		private void ReadText(string name, ref ulong val)
		{
			if (Present(name))
			{
				val = ulong.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref ulong val)
		{
			_tw.WriteLine("{0} 0x{1:X16}", name, val);
		}

		private void Read(ref float val)
		{
			val = _br.ReadSingle();
		}

		private void Write(ref float val)
		{
			_bw.Write(val);
		}

		private void Read(ref double val)
		{
			val = _br.ReadDouble();
		}

		private void Write(ref double val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref float val)
		{
			if (Present(name))
			{
				val = float.Parse(Item(name), NumberFormatInfo.InvariantInfo);
			}
		}

		private void WriteText(string name, ref float val)
		{
			_tw.WriteLine("{0} {1}", name, val);
		}

		private void ReadText(string name, ref double val)
		{
			if (Present(name))
			{
				val = double.Parse(Item(name), NumberFormatInfo.InvariantInfo);
			}
		}

		private void WriteText(string name, ref double val)
		{
			_tw.WriteLine("{0} {1}", name, val);
		}

		private void Read(ref bool val)
		{
			val = _br.ReadBoolean();
		}

		private void Write(ref bool val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref bool val)
		{
			if (Present(name))
			{
				val = bool.Parse(Item(name));
			}
		}
		private void WriteText(string name, ref bool val)
		{
			_tw.WriteLine("{0} {1}", name, val);
		}

		private sealed class Section : Dictionary<string, Section>
		{
			public string Name = "";
			public readonly Dictionary<string, string> Items = new Dictionary<string, string>();
		}
	}
}

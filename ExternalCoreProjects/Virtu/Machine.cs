﻿using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Virtu.Library;

namespace Jellyfish.Virtu
{
	public sealed class Machine
	{
		/// <summary>
		/// for deserialization only!!
		/// </summary>
		public Machine() { }

		public Machine(byte[] appleIIe, byte[] diskIIRom)
		{
			Events = new MachineEvents();

			Cpu = new Cpu(this);
			Memory = new Memory(this, appleIIe);
			Keyboard = new Keyboard();
			GamePort = new GamePort();
			Speaker = new Speaker(this);
			Video = new Video(this);
			NoSlotClock = new NoSlotClock(this);

			var emptySlot = new EmptyPeripheralCard(Video);
			Slot1 = emptySlot;
			Slot2 = emptySlot;
			Slot3 = emptySlot;
			Slot4 = emptySlot;
			Slot5 = emptySlot;
			Slot6 = new DiskIIController(this, diskIIRom);
			Slot7 = emptySlot;

			Slots = new List<IPeripheralCard> { null, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7 };

			BootDiskII = Slots.OfType<DiskIIController>().Last();
		}

		#region API

		public void BizInitialize()
		{
			Cpu.Initialize();
			Memory.Initialize();
			Speaker.Initialize();
			Video.Initialize();
			NoSlotClock.Initialize();

			Cpu.Reset();
			Memory.Reset();
			Speaker.Reset();
			Video.Reset();
			Slot6.Reset();
		}

		public void BizFrameAdvance(IEnumerable<string> buttons)
		{
			Lagged = true;
			DriveLight = false;

			Keyboard.SetKeys(buttons);

			// frame begins at vsync.. beginning of vblank
			while (Video.IsVBlank)
			{
				Events.HandleEvents(Cpu.Execute());
			}

			// now, while not vblank, we're in a frame
			while (!Video.IsVBlank)
			{
				Events.HandleEvents(Cpu.Execute());
			}
		}

		public void Serialize(JsonWriter w)
		{
			CreateSerializer().Serialize(w, this);
		}

		public static Machine Deserialize(JsonReader r)
		{
			return CreateSerializer().Deserialize<Machine>(r);
		}

		public void CpuExecute()
		{
			Events.HandleEvents(Cpu.Execute());
		}

		public void InsertDisk1(byte[] disk1)
		{
			// make a writable memory stream cloned from the rom.
			// for junk.dsk the .dsk is important because it determines the format from that
			BootDiskII.Drives[0].InsertDisk("junk.dsk", (byte[])disk1.Clone(), false);
		}

		public Cpu Cpu { get; private set; }
		public Memory Memory { get; private set; }
		public Speaker Speaker { get; private set; }
		public Video Video { get; private set; }
		public bool Lagged { get; set; }
		public bool DriveLight { get; set; }

		#endregion

		private static JsonSerializer CreateSerializer()
		{
			// TODO: converters could be cached for speedup

			var ser = new JsonSerializer
			{
				TypeNameHandling = TypeNameHandling.Auto,
				PreserveReferencesHandling = PreserveReferencesHandling.All, // leaving out Array is a very important problem, and means that we can't rely on a directly shared array to work.
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
			};

			ser.Converters.Add(new TypeTypeConverter(new[]
			{
				// all expected Types to convert are either in this assembly or mscorlib
				typeof(Machine).Assembly,
				typeof(object).Assembly
			}));

			ser.Converters.Add(new DelegateConverter());
			ser.Converters.Add(new ArrayConverter());

			var cr = new DefaultContractResolver();
			cr.DefaultMembersSearchFlags |= System.Reflection.BindingFlags.NonPublic;
			ser.ContractResolver = cr;

			return ser;
		}

		private const string Version = "0.9.4.0";

		internal MachineEvents Events { get; set; }

		internal Keyboard Keyboard { get; private set; }
		internal GamePort GamePort { get; private set; }
		internal Cassette Cassette { get; private set; }
		internal DiskIIController BootDiskII { get; private set; }
		internal NoSlotClock NoSlotClock { get; private set; }

		internal IPeripheralCard Slot1 { get; private set; }
		internal IPeripheralCard Slot2 { get; private set; }
		internal IPeripheralCard Slot3 { get; private set; }
		internal IPeripheralCard Slot4 { get; private set; }
		internal IPeripheralCard Slot5 { get; private set; }
		internal DiskIIController Slot6 { get; private set; }
		internal IPeripheralCard Slot7 { get; private set; }

		internal IList<IPeripheralCard> Slots { get; private set; }
	}
}

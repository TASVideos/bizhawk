using System.Collections.Generic;
using System.Linq;
using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public class DoomControllerDeck
	{
		public DoomControllerDeck(DoomControllerTypes controllerType, bool player1Present, bool player2Present, bool player3Present, bool player4Present)
		{
			Definition = new("Doom Demo LMP 1.9 Input Format") { };

			if (player1Present) Port1 = ControllerCtors[controllerType](1);
			if (player2Present) Port2 = ControllerCtors[controllerType](2);
			if (player3Present) Port3 = ControllerCtors[controllerType](3);
			if (player4Present) Port4 = ControllerCtors[controllerType](4);

			if (player1Present) Definition.BoolButtons.AddRange(Port1.Definition.BoolButtons.ToList());
			if (player2Present) Definition.BoolButtons.AddRange(Port2.Definition.BoolButtons.ToList());
			if (player3Present) Definition.BoolButtons.AddRange(Port3.Definition.BoolButtons.ToList());
			if (player4Present) Definition.BoolButtons.AddRange(Port4.Definition.BoolButtons.ToList());

			if (player1Present) foreach (var kvp in Port1.Definition.Axes) Definition.Axes.Add(kvp);
			if (player2Present) foreach (var kvp in Port2.Definition.Axes) Definition.Axes.Add(kvp);
			if (player3Present) foreach (var kvp in Port3.Definition.Axes) Definition.Axes.Add(kvp);
			if (player4Present) foreach (var kvp in Port4.Definition.Axes) Definition.Axes.Add(kvp);

			Definition.MakeImmutable();
		}

		public byte ReadPort1(IController c)
			=> Port1.Read(c);

		public byte ReadPort2(IController c)
			=> Port2.Read(c);

		public byte ReadPort3(IController c)
			=> Port3.Read(c);

		public byte ReadPort4(IController c)
			=> Port4.Read(c);

		public int ReadPot1(IController c, int pot)
			=> Port1.Read_Pot(c, pot);

		public int ReadPot2(IController c, int pot)
			=> Port2.Read_Pot(c, pot);

		public int ReadPot3(IController c, int pot)
			=> Port3.Read_Pot(c, pot);

		public int ReadPot4(IController c, int pot)
			=> Port4.Read_Pot(c, pot);

		public ControllerDefinition Definition { get; }

		private readonly IPort Port1;
		private readonly IPort Port2;
		private readonly IPort Port3;
		private readonly IPort Port4;

		private static IReadOnlyDictionary<DoomControllerTypes, Func<int, IPort>> _controllerCtors;

		public static IReadOnlyDictionary<DoomControllerTypes, Func<int, IPort>> ControllerCtors => _controllerCtors
			??= new Dictionary<DoomControllerTypes, Func<int, IPort>>
			{
				[DoomControllerTypes.Doom] = portNum => new DoomController(portNum),
				[DoomControllerTypes.Heretic] = portNum => new HereticController(portNum),
				[DoomControllerTypes.Hexen] = portNum => new HexenController(portNum),
			};
	}
}

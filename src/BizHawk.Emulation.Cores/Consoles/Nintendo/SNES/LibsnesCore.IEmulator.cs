﻿using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

#if BSNES_LAGFIX_B
		private bool? _inputNotifyCBIsNotLag = null;
#else // BSNES_LAGFIX_A
		private bool? _inputNotifyCBIsLag = null;
#endif

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

#if BSNES_LAGFIX_B
			_inputNotifyCBIsNotLag = null;
#else // BSNES_LAGFIX_A
			_inputNotifyCBIsLag = null;
#endif

			if (_tracer.Enabled)
			{
				//Api.QUERY_set_trace_callback(1<<(int)LibsnesApi.eTRACE.SMP, _tracecb); //TEST -- it works but theres no way to control it from the frontend now

				if(IsSGB)
					Api.QUERY_set_trace_callback(1<<(int)LibsnesApi.eTRACE.GB, _tracecb);
				else
					Api.QUERY_set_trace_callback(1<<(int)LibsnesApi.eTRACE.CPU, _tracecb);
			}
			else
			{
				Api.QUERY_set_trace_callback(0,null);
			}

			// speedup when sound rendering is not needed
			Api.QUERY_set_audio_sample(renderSound ? _soundcb : null);

			bool resetSignal = controller.IsPressed("Reset");
			if (resetSignal)
			{
				Api.CMD_reset();
			}

			bool powerSignal = controller.IsPressed("Power");
			if (powerSignal)
			{
				Api.CMD_power();
			}

			var enables = new LibsnesApi.LayerEnables
			{
				BG1_Prio0 = _settings.ShowBG1_0,
				BG1_Prio1 = _settings.ShowBG1_1,
				BG2_Prio0 = _settings.ShowBG2_0,
				BG2_Prio1 = _settings.ShowBG2_1,
				BG3_Prio0 = _settings.ShowBG3_0,
				BG3_Prio1 = _settings.ShowBG3_1,
				BG4_Prio0 = _settings.ShowBG4_0,
				BG4_Prio1 = _settings.ShowBG4_1,
				Obj_Prio0 = _settings.ShowOBJ_0,
				Obj_Prio1 = _settings.ShowOBJ_1,
				Obj_Prio2 = _settings.ShowOBJ_2,
				Obj_Prio3 = _settings.ShowOBJ_3
			};

			Api.SetLayerEnables(ref enables);

			RefreshMemoryCallbacks(false);

			// apparently this is one frame?
			_timeFrameCounter++;
			Api.CMD_run();

#if BSNES_LAGFIX_B
			if (_inputNotifyCBIsNotLag == true)
#else // BSNES_LAGFIX_A
			if (_inputNotifyCBIsLag != true)
#endif
			{
				IsLagFrame = false;
			}
			else
			{
				IsLagFrame = true;
				LagCount++;
			}

			return true;
		}

		public int Frame
		{
			get => _timeFrameCounter;
			private set => _timeFrameCounter = value;
		}

		public string SystemId { get; }
		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_timeFrameCounter = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			Api.Dispose();
			_resampler.Dispose();

			_disposed = true;
		}
	}
}

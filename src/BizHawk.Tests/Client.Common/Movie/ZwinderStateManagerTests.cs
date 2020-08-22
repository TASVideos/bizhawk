using System.IO;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class ZwinderStateManagerTests
	{
		private ZwinderStateManager CreateSmallZwinder(IStatable ss)
		{
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentBufferSize = 1,
				CurrentTargetFrameLength = 10000,

				RecentBufferSize = 1,
				RecentTargetFrameLength = 100000,

				AncientStateInterval = 50000
			});

			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());
			return zw;
		}

		private IStatable CreateStateSource() => new StateSource {PaddingData = new byte[1000]};

		[TestMethod]
		public void SaveCreateRoundTrip()
		{
			var ms = new MemoryStream();
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentBufferSize = 16,
				CurrentTargetFrameLength = 10000,

				RecentBufferSize = 16,
				RecentTargetFrameLength = 100000,

				AncientStateInterval = 50000
			});
			zw.SaveStateHistory(new BinaryWriter(ms));
			var buff = ms.ToArray();
			var rms = new MemoryStream(buff, false);

			var zw2 = ZwinderStateManager.Create(new BinaryReader(rms), zw.Settings, f => false);

			// TODO: we could assert more things here to be thorough
			Assert.IsNotNull(zw2);
			Assert.AreEqual(zw.Settings.CurrentBufferSize, zw2.Settings.CurrentBufferSize);
			Assert.AreEqual(zw.Settings.RecentBufferSize, zw2.Settings.RecentBufferSize);
		}

		[TestMethod]
		public void CountEvictWorks()
		{
			using var zb = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});
			var ss = new StateSource
			{
				PaddingData = new byte[10]
			};
			var stateCount = 0;
			for (int i = 0; i < 1000000; i++)
			{
				zb.Capture(i, s => ss.SaveStateBinary(new BinaryWriter(s)), j => stateCount--, true);
				stateCount++;
			}
			Assert.AreEqual(zb.Count, stateCount);
		}

		[TestMethod]
		public void SaveCreateBufferRoundTrip()
		{
			var buff = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 10
			});
			var ss = new StateSource { PaddingData = new byte[500] };
			for (var frame = 0; frame < 2090; frame++)
			{
				ss.Frame = frame;
				buff.Capture(frame, (s) => ss.SaveStateBinary(new BinaryWriter(s)));
			}
			// states are 504 bytes large, buffer is 1048576 bytes large
			Assert.AreEqual(buff.Count, 2080);
			Assert.AreEqual(buff.GetState(0).Frame, 10);
			Assert.AreEqual(buff.GetState(2079).Frame, 2089);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff.GetState(0).GetReadStream()), 10);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff.GetState(2079).GetReadStream()), 2089);

			var ms = new MemoryStream();
			buff.SaveStateBinary(new BinaryWriter(ms));
			ms.Position = 0;
			var buff2 = ZwinderBuffer.Create(new BinaryReader(ms));

			Assert.AreEqual(buff.Size, buff2.Size);
			Assert.AreEqual(buff.Used, buff2.Used);
			Assert.AreEqual(buff2.Count, 2080);
			Assert.AreEqual(buff2.GetState(0).Frame, 10);
			Assert.AreEqual(buff2.GetState(2079).Frame, 2089);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff2.GetState(0).GetReadStream()), 10);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff2.GetState(2079).GetReadStream()), 2089);
		}

		[TestMethod]
		public void StateBeforeFrame()
		{
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentBufferSize = 1,
				CurrentTargetFrameLength = 10000,

				RecentBufferSize = 1,
				RecentTargetFrameLength = 100000,

				AncientStateInterval = 50000
			});
			{
				var ms = new MemoryStream();
				ss.SaveStateBinary(new BinaryWriter(ms));
				zw.Engage(ms.ToArray());
			}
			for (int frame = 0; frame <= 10440; frame++)
			{
				ss.Frame = frame;
				zw.Capture(frame, ss);
			}
			var kvp = zw.GetStateClosestToFrame(10440);
			var actual = StateSource.GetFrameNumberInState(kvp.Value);
			Assert.AreEqual(kvp.Key, actual);
			Assert.IsTrue(actual <= 10440);
		}

		[TestMethod]
		public void Last_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);
			
			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 20; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Last;

			// Assert
			Assert.AreEqual(futureReservedFrame, actual);
		}

		[TestMethod]
		public void Last_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 9;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Last;

			// Assert
			Assert.AreEqual(totalCurrentFrames - expectedFrameGap, actual);
		}

		[TestMethod]
		public void HasState_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 20; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.HasState(futureReservedFrame);

			// Assert
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void HasState_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 9;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.HasState(totalCurrentFrames - expectedFrameGap);

			// Assert
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.GetStateClosestToFrame(futureReservedFrame + 1);

			// Assert
			Assert.IsNotNull(actual);
			Assert.AreEqual(futureReservedFrame, actual.Key);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 9;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.GetStateClosestToFrame(totalCurrentFrames);

			// Assert
			Assert.AreEqual(totalCurrentFrames - expectedFrameGap, actual.Key);
		}

		[TestMethod]
		public void InvalidateAfter_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			zw.InvalidateAfter(futureReservedFrame - 1);

			// Assert
			Assert.IsFalse(zw.HasState(futureReservedFrame));
		}

		[TestMethod]
		public void InvalidateAfter_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 10;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			zw.InvalidateAfter(totalCurrentFrames - 1);

			// Assert
			Assert.IsFalse(zw.HasState(totalCurrentFrames));
		}

		[TestMethod]
		public void Count_NoReserved()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 10;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Count;

			// Assert
			var expected = (totalCurrentFrames / expectedFrameGap) + 1;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void Count_WithReserved()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 10;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(1000, ss);
			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Count;

			// Assert
			var expected = (totalCurrentFrames / expectedFrameGap) + 2;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void DeleteMe()
		{
			var ss = CreateStateSource();
			var zw = new ZwinderStateManager(f => false);

			for (int i = 0; i < 10000; i += 200)
			{
				zw.CaptureReserved(i, ss);
			}

			for (int i = 400; i < 10000; i += 400)
			{
				zw.EvictReserved(i);
			}

			for (int i = 0; i < 100000; i++)
			{
				zw.Capture(i, ss);
			}

			for (int i = 0; i < 100000; i++)
			{
				var hasState = zw.HasState(i);
				var hasCache = zw.StateCache.Contains(i);

				if (hasState != hasCache)
				{
					int zzz = 0;
				}

				Assert.AreEqual(hasState, hasCache);
			}
		}

		private class StateSource : IStatable
		{
			public int Frame { get; set; }
			public byte[] PaddingData { get; set; } = new byte[0];
			public void LoadStateBinary(BinaryReader reader)
			{
				Frame = reader.ReadInt32();
				reader.Read(PaddingData, 0, PaddingData.Length);
			}

			public void SaveStateBinary(BinaryWriter writer)
			{
				writer.Write(Frame);
				writer.Write(PaddingData);
			}

			public static int GetFrameNumberInState(Stream stream)
			{
				var ss = new StateSource();
				ss.LoadStateBinary(new BinaryReader(stream));
				return ss.Frame;
			}
		}
	}
}

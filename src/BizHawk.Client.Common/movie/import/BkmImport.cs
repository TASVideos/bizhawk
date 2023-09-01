﻿using BizHawk.Common;

namespace BizHawk.Client.Common.movie.import
{
	// ReSharper disable once UnusedMember.Global
	[ImporterFor("BizHawk", ".bkm")]
	internal class BkmImport : MovieImporter
	{
		protected override void RunImport()
		{
			BkmMovie bkm = new BkmMovie { Filename = SourceFile.FullName };
			bkm.Load();

			for (int i = 0; i < bkm.InputLogLength; i++)
			{
				var input = bkm.GetInputState(i, Result.Movie.Emulator.ControllerDefinition, bkm.Header[HeaderKeys.Platform]);
				Result.Movie.AppendFrame(input);
			}

			Result.Movie.HeaderEntries.Clear();
			foreach (var (k, v) in bkm.Header) Result.Movie.HeaderEntries[k] = v;

			Result.Movie.SyncSettingsJson = bkm.SyncSettingsJson;

			Result.Movie.Comments.Clear();
			foreach (string comment in bkm.Comments)
			{
				Result.Movie.Comments.Add(comment);
			}

			Result.Movie.Subtitles.Clear();
			foreach (var sub in bkm.Subtitles)
			{
				Result.Movie.Subtitles.Add(sub);
			}

			Result.Movie.BinarySavestate = bkm.BinarySavestate;
		}
	}
}

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovieImport
	{
		ImportResult Import(
			IDialogParent dialogParent,
			IMovieSession session,
			IEmulator emulator,
			string path,
			Config config);
	}

	internal abstract class MovieImporter : IMovieImport
	{
		protected const string EmulationOrigin = "emuOrigin";
		protected const string MovieOrigin = "MovieOrigin";

		protected IDialogParent _dialogParent;
		private IEmulator _emulator;
		private delegate bool MatchesMovieHash(ReadOnlySpan<byte> romData);

		public ImportResult Import(
			IDialogParent dialogParent,
			IMovieSession session,
			IEmulator emulator,
			string path,
			Config config)
		{
			_dialogParent = dialogParent;
			_emulator = emulator;
			SourceFile = new FileInfo(path);
			Config = config;

			if (!SourceFile.Exists)
			{
				Result.Errors.Add($"Could not find the file {path}");
				return Result;
			}

			var newFileName = $"{SourceFile.FullName}.{Bk2Movie.Extension}";
			Result.Movie = session.Get(newFileName);
			Result.Movie.Attach(emulator);
			RunImport();

			if (!Result.Errors.Any())
			{
				if (string.IsNullOrEmpty(Result.Movie.Hash))
				{
					string hash = null;
					// try to generate a matching hash from the original ROM
					if (Result.Movie.HeaderEntries.TryGetValue("CRC32", out string crcHash))
					{
						hash = PromptForRom(data => string.Equals(CRC32Checksum.ComputeDigestHex(data), crcHash, StringComparison.OrdinalIgnoreCase));
					}
					else if (Result.Movie.HeaderEntries.TryGetValue("MD5", out string md5Hash))
					{
						hash = PromptForRom(data => string.Equals(MD5Checksum.ComputeDigestHex(data), md5Hash, StringComparison.OrdinalIgnoreCase));
					}
					else if (Result.Movie.HeaderEntries.TryGetValue("SHA256", out string sha256Hash))
					{
						hash = PromptForRom(data => string.Equals(SHA256Checksum.ComputeDigestHex(data), sha256Hash, StringComparison.OrdinalIgnoreCase));
					}

					if (hash is not null)
						Result.Movie.Hash = hash;
				}

				Result.Movie.Save();
			}

			return Result;
		}

		/// <summary>
		/// Prompts the user for a ROM file that matches the original movie file's hash
		/// and returns a SHA1 hash of that ROM file.
		/// </summary>
		/// <param name="matchesMovieHash">Function that checks whether the ROM data matches the original hash</param>
		/// <returns>SHA1 hash of the selected ROM file</returns>
		private string PromptForRom(MatchesMovieHash matchesMovieHash)
		{
			string messageBoxText = "Please select the original ROM to finalize the import process.";
			while (true)
			{
				if (!_dialogParent.ModalMessageBox2(messageBoxText, "ROM required to populate hash", useOKCancel: true))
					return "";

				var result = _dialogParent.ShowFileOpenDialog(
					filter: RomLoader.RomFilter,
					initDir: Config.PathEntries.RomAbsolutePath(_emulator.SystemId));
				if (result is null)
					return "";

				using var rom = new HawkFile(result);
				if (rom.IsArchive) rom.BindFirst();
				ReadOnlySpan<byte> romData = rom.ReadAllBytes();
				if (romData.Length % 1024 == 512)
					romData = romData.Slice(512, romData.Length - 512);
				if (matchesMovieHash(romData))
					return SHA1Checksum.ComputeDigestHex(romData);

				messageBoxText = "The selected ROM does not match the movie's hash. Please try again.";
			}
		}

		protected Config Config { get; private set; }

		protected ImportResult Result { get; } = new ImportResult();

		protected FileInfo SourceFile { get; private set; }

		protected abstract void RunImport();

		// Get the content for a particular header.
		protected static string ParseHeader(string line, string headerName)
		{
			// Case-insensitive search.
			int x = line.ToLower().LastIndexOf(
				headerName.ToLower()) + headerName.Length;
			string str = line.Substring(x + 1, line.Length - x - 1);
			return str.Trim();
		}

		// Reduce all whitespace to single spaces.
		protected static string SingleSpaces(string line)
		{
			line = line.Replace("\t", " ");
			line = line.Replace("\n", " ");
			line = line.Replace("\r", " ");
			line = line.Replace("\r\n", " ");
			string prev;
			do
			{
				prev = line;
				line = line.Replace("  ", " ");
			}
			while (prev != line);
			return line;
		}

		// Ends the string where a NULL character is found.
		protected static string NullTerminated(string str)
		{
			int pos = str.IndexOf('\0');
			if (pos != -1)
			{
				str = str.Substring(0, pos);
			}

			return str;
		}
	}

	public class ImportResult
	{
		public IList<string> Warnings { get; } = new List<string>();
		public IList<string> Errors { get; } = new List<string>();

		public IMovie Movie { get; set; }

		public static ImportResult Error(string errorMsg)
		{
			var result = new ImportResult();
			result.Errors.Add(errorMsg);
			return result;
		}
	}

	[AttributeUsage(AttributeTargets.Class)]
	public sealed class ImporterForAttribute : Attribute
	{
		public ImporterForAttribute(string emulator, string extension)
		{
			Emulator = emulator;
			Extension = extension;
		}

		public string Emulator { get; }
		public string Extension { get; }
	}
}

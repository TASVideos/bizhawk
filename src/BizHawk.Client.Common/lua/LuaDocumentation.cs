﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Newtonsoft.Json;

namespace BizHawk.Client.Common
{
	public class LuaDocumentation : List<LibraryFunction>
	{
		public string ToTASVideosWikiMarkup()
		{
			var sb = new StringBuilder();
			
			sb.AppendLine("__This is an autogenerated page, do not edit.__ (To update, open the function list dialog on a Debug build and click the wiki button.)")
				.AppendLine()
				.AppendLine("This page documents the the behavior and parameters of Lua functions available for the [BizHawk] emulator. The function list is also available from within EmuHawk. From the Lua Console, click Help > Lua Functions List or press F1.")
				.AppendLine()
				.AppendLine("As you might expect, Lua scripts execute top to bottom once, so to run something every frame you'll need to end the script with an infinite loop (or you could set a callback on one of the events instead). Make sure to call {{emu.frameadvance}} or {{emu.yield}} in the loop or your script will hang EmuHawk and then crash!")
				.AppendLine()
				.AppendLine("Lua supports integer arithmetic starting with BizHawk 2.9. Note {{~}} is both bitwise NOT and XOR. Some of the {{bit}} helper functions remain, but you should try to avoid them if you need performance (or [https://github.com/TASEmulators/BizHawk-ExternalTools/wiki|switch to .NET]). If you're getting overwhelmed with deprecation warnings while trying to migrate a script, add this one-liner at the top: {{bit = (require \"migration_helpers\").EmuHawk_pre_2_9_bit();}}")
				.AppendLine()
				.AppendLine("FCEUX users: While this API surface may look similar, even functions with the same name may take different arguments, or behave differently in a way that isn't immediately obvious. (TODO: create a migration helper function for FCEUX)")
				.AppendLine()
				.AppendLine(@"__Types and notation__
* [[]] (brackets)
** Brackets around a parameter indicate that the parameter is optional. Optional parameters have an equals sign followed by the value that will be used if no value is supplied.
** Brackets after a parameter type indicate it is an array
* ? (question mark)
** A question mark next to a value indicates that it is nullable i.e. null/nil may be passed instead of a real value. (Confusingly, many .NET types are nullable but omit the '?'. These aren't common in our Lua APIs.)
* null/nil
** null is equivalent to Lua's nil.
** Omitting the last parameter is equivalent to passing nil, same with the second-last parameter and so on. However, if you want to pass the last parameter but not one in the middle, you will need to explicitly pass nil.
* object
** A System.Object; any table or value may be passed, including nil.
** Usually you can infer what kinds of values would be useful to pass from the descriptions and examples.
* luacolor
** Any of:
** a 32-bit number in the format 0xAARRGGBB;
** a string in the format ""#RRGGBB"" or ""#AARRGGBB"";
** a string containing a CSS3/X11 color name e.g. ""blue"", ""palegoldenrod""; or
** a Color created with forms.createcolor.
** As noted above, luacolor? indicates nil may also be passed.
* nluafunc
** A Lua function. Note that these are always parameters, and never return values of a call.
** Some callbacks will be called with arguments, if the function you register has the right number of parameters. This will be noted in the registration function's docs.
* table
** A standard Lua table
* something else
** check the .NET documentation on MSDN")
				.AppendLine()
				.AppendLine(@"__Common parameter meanings__
* {{ulong addr}}/{{long addr}}/{{uint addr}}/{{int addr}} in memory functions
** Relative to the specified domain (or the ""current"" domain, see below).
** Some memory events allow {{nil}} for the address. That will hook on all addresses.
* {{string domain}} in memory functions
** The name of a memory domain. The list of valid domains is returned by {{memory.getmemorydomainlist}}, or can be seen in various tools such as the Hex Editor.
** If {{domain}} is optional (i.e. the function has {{nil}} as the default argument) and you pass {{nil}} or omit the argument, the ""current"" domain is used. This can be set/gotten with {{memory.usememorydomain}}/{{memory.getcurrentmemorydomain}}.
* {{string scope}} in memory functions
** The name of a scope. The list of valid scopes is returned by {{event.availableScopes}}.
* {{int frame}} in {{movie}}/{{tastudio}} functions and others
** Frame index i.e. the number of VBlanks that have been seen. While many EmuHawk features use this concept, note that most take effect ''after'' a certain frame, but some, such as memory freezes and polling/inputs, take effect ''during'' a frame.
* {{int? controller}}/{{int? player}} in (virtual) input functions
** Player index, as seen in {{Config}} > {{Controllers...}} (starts at 1).
** If you pass {{nil}} or omit the argument, this will either indicate all players, or ""no player"" i.e. buttons on the Console. See the relevant function's documentation.
* {{luacolor line}}/{{luacolor background}} in drawing functions
** {{line}} is the ""stroke"" or ""outline"" color, {{background}} is the ""fill"" color. Both can have transparency.
* {{string surfacename}} in {{gui}} drawing functions
** Either {{""emucore""}} or {{""client""}}.
** If you pass {{nil}} or omit the argument, the ""current"" surface is used. This can be set with {{gui.use_surface}}.
* other {{string}} parameters in drawing functions
** See the relevant function's documentation.
* {{long handle}} in forms functions
** These handles are returned by {{forms.newform}} and the various control creation functions such as {{forms.checkbox}}. Do not attempt to re-use them across script restarts or do arithmetic with them.
* {{string guid}}/{{string name}} in event functions
** The ''name'' of a callback is an optional parameter in all the event subscription functions. The ''ID'' of a callback is what's returned by the subscription function. Multiple callbacks can share the same name, but IDs are unique.
");

			foreach (var library in this.Select(lf => (Name: lf.Library, Description: lf.LibraryDescription))
				.Distinct()
				.OrderBy(library => library.Name))
			{
				sb
					.AppendFormat("%%TAB {0}%%", library.Name)
					.AppendLine()
					.AppendLine();
				if (!string.IsNullOrWhiteSpace(library.Description))
				{
					sb
						.Append(library.Description)
						.AppendLine()
						.AppendLine();
				}

				foreach (var func in this.Where(lf => lf.Library == library.Name).OrderBy(lf => lf.Name))
				{
					string deprecated = func.IsDeprecated ? "__[[deprecated]]__ " : "";
					sb
						.AppendFormat("__{0}.{1}__%%%", func.Library, func.Name)
						.AppendLine().AppendLine()
						.AppendFormat("* {4}{0} {1}.{2}{3}", func.ReturnType, func.Library, func.Name, func.ParameterList.Replace("[", "[[").Replace("]", "]]"), deprecated)
						.AppendLine().AppendLine()
						.AppendFormat("* {0}", func.Description)
						.AppendLine().AppendLine();
				}
			}

			sb.Append("%%TAB_END%%");

			return sb.ToString();
		}

		private class SublimeCompletions
		{
			public SublimeCompletions()
			{
				Scope = "source.lua - string";
			}

			[JsonProperty(PropertyName = "scope")]
			public string Scope { get; set; }

			[JsonProperty(PropertyName = "completions")]
			public List<Completion> Completions { get; set; } = new List<Completion>();

			public class Completion
			{
				[JsonProperty(PropertyName = "trigger")]
				public string Trigger { get; set; }

				[JsonProperty(PropertyName = "contents")]
				public string Contents { get; set; }
			}
		}

		public string ToSublime2CompletionList()
		{
			var sc = new SublimeCompletions();

			foreach (var f in this.OrderBy(lf => lf.Library).ThenBy(lf => lf.Name))
			{
				var completion = new SublimeCompletions.Completion
				{
					Trigger = $"{f.Library}.{f.Name}"
				};

				var sb = new StringBuilder();

				if (f.ParameterList.Length is not 0)
				{
					sb
						.Append($"{f.Library}.{f.Name}(");

					var parameters = f.Method.GetParameters()
						.ToList();

					for (int i = 0; i < parameters.Count; i++)
					{
						sb
							.Append("${")
							.Append(i + 1)
							.Append(':');

						sb.Append(parameters[i].IsOptional
							? $"[{parameters[i].Name}]"
							: parameters[i].Name);

						sb.Append('}');

						if (i < parameters.Count - 1)
						{
							sb.Append(',');
						}
					}

					sb.Append(')');
				}
				else
				{
					sb.Append($"{f.Library}.{f.Name}()");
				}

				completion.Contents = sb.ToString();
				sc.Completions.Add(completion);
			}

			return JsonConvert.SerializeObject(sc);
		}

		public string ToNotepadPlusPlusAutoComplete()
		{
			return ""; // TODO
		}

		public string ToLuaLanguageServerDefinitions()
		{
			var generator = new LuaCatsGenerator();
			return generator.Generate(this);
		}
	}

	public class LibraryFunction
	{
		private readonly LuaMethodAttribute _luaAttributes;
		private readonly LuaMethodExampleAttribute _luaExampleAttribute;

		public readonly bool SuggestInREPL;

		public LibraryFunction(string library, string libraryDescription, MethodInfo method, bool suggestInREPL = true)
		{
			_luaAttributes = method.GetCustomAttribute<LuaMethodAttribute>(false);
			_luaExampleAttribute = method.GetCustomAttribute<LuaMethodExampleAttribute>(false);
			Method = method;
			SuggestInREPL = suggestInREPL;

			IsDeprecated = method.GetCustomAttribute<LuaDeprecatedMethodAttribute>(false) != null;
			Library = library;
			LibraryDescription = libraryDescription;
		}

		public string Library { get; }
		public string LibraryDescription { get; }

		public readonly bool IsDeprecated;

		public MethodInfo Method { get; }

		public string Name => _luaAttributes.Name;

		public string Description => _luaAttributes.Description;

		public string Example => _luaExampleAttribute?.Example;

		private string _parameterList;

		public string ParameterList
		{
			get
			{
				if (_parameterList == null)
				{
					var parameters = Method.GetParameters();

					var list = new StringBuilder();
					list.Append('(');
					for (var i = 0; i < parameters.Length; i++)
					{
						var p = TypeCleanup(parameters[i].ToString());
						if (parameters[i].GetCustomAttribute<LuaColorParamAttribute>() != null) p = p.Replace("object", "luacolor");
						if (parameters[i].IsOptional)
						{
							list.Append($"[{p} = {parameters[i].DefaultValue?.ToString() ?? "nil"}]");
						}
						else
						{
							list.Append(p);
						}

						if (i < parameters.Length - 1)
						{
							list.Append(", ");
						}
					}

					list.Append(')');
					_parameterList = list.ToString();
				}

				return _parameterList;
			}
		}

		private static string TypeCleanup(string str)
		{
			return str
				.Replace("System", "")
				.Replace(" ", "")
				.Replace(".", "")
				.Replace("LuaInterface", "")
				.Replace("Object[]", "object[] ")
				.Replace("Object", "object ")
				.Replace("Nullable`1[Boolean]", "bool? ")
				.Replace("Boolean[]", "bool[] ")
				.Replace("Boolean", "bool ")
				.Replace("String", "string ")
				.Replace("LuaTable", "table ")
				.Replace("LuaFunction", "func ")
				.Replace("Nullable`1[Int32]", "int? ")
				.Replace("Nullable`1[UInt32]", "uint? ")
				.Replace("Byte", "byte ")
				.Replace("Int16", "short ")
				.Replace("Int32", "int ")
				.Replace("Int64", "long ")
				.Replace("Ushort", "ushort ")
				.Replace("Ulong", "ulong ")
				.Replace("UInt32", "uint ")
				.Replace("UInt64", "ulong ")
				.Replace("Double", "double ")
				.Replace("Uint", "uint ")
				.Replace("Nullable`1[DrawingColor]", "Color? ")
				.Replace("DrawingColor", "Color ")
				.ToLowerInvariant();
		}

		public string ReturnType
		{
			get
			{
				var returnType = Method.ReturnType.ToString();
				return TypeCleanup(returnType).Trim();
			}
		}
	}
}

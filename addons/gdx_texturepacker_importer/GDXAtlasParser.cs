#if TOOLS
using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class GDXAtlasParser
{
	private static Regex whitespace_regex = new Regex(@"\s");

	public static Error Parse(string atlasFile, out List<GDXAtlasEntry> data)
	{
		data = new();
		using var file = FileAccess.Open(atlasFile, FileAccess.ModeFlags.Read);
		if (file == null) return Error.Failed;
		if (file.GetError() != Error.Ok) return file.GetError();

		var entry = new GDXAtlasEntry();
		var fileLength = file.GetLength();
		while (file.GetPosition() < fileLength)
		{
			var line = file.GetLine();
			line = whitespace_regex.Replace(line, "");

			// property of texture if contains ":" in line
			if (line.Contains(":"))
			{
				var ss = line.Split(':', StringSplitOptions.RemoveEmptyEntries);
				if (ss.Length == 2)
				{
					switch (ss[0])
					{
						case "bounds":
						{
							if (TryParseRect2I(ss[1], out Rect2I r)) entry.bounds = r;
							else GD.PrintErr($"Unexpected line encountered: {line}");
							break;
						}
						case "index":
						{
							if (int.TryParse(ss[1], out int i)) entry.index = i;
							else GD.PrintErr($"Unexpected line encountered: {line}");
							break;
						}
						case "split":
						{
							entry.is9Slice = true;
							if (TryParseRect2I(ss[1], out Rect2I r)) entry.split = r;
							else GD.PrintErr($"Unexpected line encountered: {line}");
							break;
						}
						case "size":
						{
							entry.isSource = true;
							break;
						}
					}
				}
				else
				{
					GD.PrintErr($"Unexpected line encountered: {line}");
				}
			}
			// else, a new entry
			else
			{
				// could be an empty line when reached point where altas split between 2 or more image files
				if (!string.IsNullOrEmpty(line))
				{
					entry = new() { name = line, index = -1 };
					data.Add(entry);
				}
			}
		}

		return Error.Ok;
	}

	private static bool TryParseRect2I(string input, out Rect2I rect)
	{
		var ss = input.Split(',', StringSplitOptions.RemoveEmptyEntries);
		if (ss.Length == 4)
		{
			int.TryParse(ss[0], out int x);
			int.TryParse(ss[1], out int y);
			int.TryParse(ss[2], out int w);
			int.TryParse(ss[3], out int h);
			rect = new Rect2I(x, y, w, h);
			return true;
		}
		else
		{
			GD.PrintErr($"Unexpected Rect encountered: {input}");
			rect = new Rect2I();
			return false;
		}
	}

	// ----------------------------------------------------------------------------------------------------------------
}
#endif

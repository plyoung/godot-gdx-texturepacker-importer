#if TOOLS
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

[Tool]
public partial class GDXSprite2DImporter : EditorImportPlugin
{
	public override string _GetImporterName()
	{
		return "plyoung.gdx_sprite2d_importer";
	}

	public override string _GetVisibleName()
	{
		return "Sprite2D";
	}

	public override float _GetPriority()
	{
		return 1f;
	}

	public override string[] _GetRecognizedExtensions()
	{
		return new string[] { "atlas" };
	}

	public override string _GetSaveExtension()
	{
		return "tres";
	}

	public override string _GetResourceType()
	{
		return "GDXAtlas";
	}

	public override int _GetPresetCount()
	{
		return 1;
	}

	public override string _GetPresetName(int presetIndex)
	{
		return "Default";
	}

	public override int _GetImportOrder()
	{
		return 0;
	}

	public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetImportOptions(string path, int presetIndex)
	{
		var options = string.Empty;
		for (int i = 0; i < (int)CanvasItem.TextureFilterEnum.Max; i++)
		{
			if (i > 0) options += ",";
			options += ((CanvasItem.TextureFilterEnum)i).ToString();
		}

		return new Godot.Collections.Array<Godot.Collections.Dictionary>
		{
			new Godot.Collections.Dictionary
			{
				{ "name", "filter" },
				{ "default_value", (int)CanvasItem.TextureFilterEnum.Linear },
				{ "property_hint", (int)PropertyHint.Enum },
				{ "hint_string", options },
				//{ "usage", ... },
			}
		};
	}

	public override bool _GetOptionVisibility(string path, StringName optionName, Godot.Collections.Dictionary options)
	{
		return true;
	}

	public override Error _Import(string sourceFile, string savePath, Godot.Collections.Dictionary options, Godot.Collections.Array<string> platformVariants, Godot.Collections.Array<string> genFiles)
	{
		var res = GDXAtlasParser.Parse(sourceFile, out List<GDXAtlasEntry> data);
		if (res != Error.Ok) return res;

		var createdFiles = new List<string>();
		res = CreateResources(sourceFile, data, options, createdFiles);
		if (res != Error.Ok) return res;

		genFiles = new Godot.Collections.Array<string>();
		genFiles.AddRange(createdFiles);

		// override atlas res with empty resource to indicate it was processed
		ResourceSaver.Save(new Resource(), $"{savePath}.tres");

		return Error.Ok;
	}

	private Error CreateResources(string atlasFile, List<GDXAtlasEntry> data, Godot.Collections.Dictionary options, List<string> createdFiles)
	{
		// create the directory where resources will be stored
		var folderName = atlasFile.GetFile() + "_sprites";
		var basePath = atlasFile.GetBaseDir();
		var dir = DirAccess.Open(basePath);
		var res = Error.Ok;
		if (!dir.DirExists(folderName))
		{
			res = dir.MakeDir(folderName);
			if (res != Error.Ok)
			{
				GD.PrintErr($"Failed to create dir: {basePath}/{folderName}");
				return res;
			}
		}

		// create resources
		var filter = (CanvasItem.TextureFilterEnum)options["filter"].AsInt32();
		CompressedTexture2D sourceTexture = null;
		Vector2 textureSize;

		for (int i = 0; i < data.Count; i++)
		{
			var entry = data[i];
			if (entry.isSource)
			{
				var path = basePath.PathJoin(entry.name);
				sourceTexture = ResourceLoader.Load<CompressedTexture2D>(path, "Texture2D", ResourceLoader.CacheMode.Replace);
				if (!GodotObject.IsInstanceValid(sourceTexture))
				{
					sourceTexture = null;
					GD.PrintErr($"Could not load: {path}");
				}
				textureSize = sourceTexture.GetSize();
				continue;
			}

			if (sourceTexture == null)
			{   // sanity check, but should not happen
				continue;
			}

			// create relative paths if needed
			var textureName = entry.name;
			var texturePath = folderName.PathJoin(textureName);
			if (entry.name.Contains('/'))
			{
				var ss = entry.name.Split('/', StringSplitOptions.RemoveEmptyEntries);
				texturePath = folderName;
				for (int j = 0; j < ss.Length - 1; j++)
				{
					texturePath = texturePath.PathJoin(ss[j]);
				}

				res = dir.MakeDirRecursive(texturePath);
				if (res != Error.Ok)
				{
					GD.PrintErr($"Failed to create dir: {basePath}/{texturePath}");
					return res;
				}

				textureName = ss[^1];
				texturePath = texturePath.PathJoin(textureName);
			}

			if (entry.index >= 0)
			{
				texturePath += $"_{entry.index}";
			}

			var spritePath = basePath.PathJoin(texturePath) + ".tscn";

			// create/update sprite
			PackedScene spriteScene = null;
			if (dir.FileExists(spritePath))
			{
				spriteScene = ResourceLoader.Load<PackedScene>(spritePath, "PackedScene", ResourceLoader.CacheMode.Replace);
			}

			spriteScene = spriteScene ?? new PackedScene();

			var sprite = new Sprite2D();
			sprite.Name = textureName;
			sprite.Texture = sourceTexture;
			sprite.TextureFilter = filter;
			sprite.RegionEnabled = true;
			sprite.RegionRect = entry.bounds;

			res = spriteScene.Pack(sprite);
			if (res == Error.Ok)
			{
				res = ResourceSaver.Save(spriteScene, spritePath);
				if (res == Error.Ok)
				{
					createdFiles.Add(spritePath);
				}
				else
				{
					GD.Print($"Failed to save: {spritePath} => {res}");
				}
			}
			else
			{
				GD.Print($"Failed to pack Sprite2D in: {spritePath} => {res}");
			}

			sprite.Free();
		}

		return Error.Ok;
	}

	// ----------------------------------------------------------------------------------------------------------------
}
#endif

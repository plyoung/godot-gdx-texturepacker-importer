#if TOOLS
using Godot;
using System;
using System.Collections.Generic;

[Tool]
public partial class GDXAtlasTextureImporter : EditorImportPlugin
{
	public override string _GetImporterName()
	{
		return "plyoung.gdx_atlas_texture_importer";
	}

	public override string _GetVisibleName()
	{
		return "AtlasTexture";
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
		return new Godot.Collections.Array<Godot.Collections.Dictionary>();
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
		res = CreateResources(sourceFile, data, createdFiles);
		if (res != Error.Ok) return res;

		genFiles = new Godot.Collections.Array<string>();
		genFiles.AddRange(createdFiles);

		// override atlas res with empty resource to indicate it was processed
		ResourceSaver.Save(new Resource(), $"{savePath}.tres");

		return Error.Ok;
	}

	private Error CreateResources(string atlasFile, List<GDXAtlasEntry> data, List<string> createdFiles)
	{
		// create the directory where resources will be stored
		var folderName = atlasFile.GetFile() + "_textures";
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

			var atlastTexturePath = basePath.PathJoin(texturePath) + ".tres";
			var ninePatchPath = basePath.PathJoin(texturePath) + "_9p.tscn";

			// create/update atlas texture
			AtlasTexture atlasTexture = null;
			if (dir.FileExists(atlastTexturePath)) 
			{
				atlasTexture = ResourceLoader.Load<AtlasTexture>(atlastTexturePath, "AtlasTexture", ResourceLoader.CacheMode.Replace);
			}

			atlasTexture = atlasTexture ?? new AtlasTexture();
			atlasTexture.Atlas = sourceTexture;
			atlasTexture.Region = entry.bounds;
			res = ResourceSaver.Save(atlasTexture, atlastTexturePath);
			if (res != Error.Ok)
			{
				GD.Print($"Failed to save: {atlastTexturePath} => {res}");
				continue;
			}

			createdFiles.Add(atlastTexturePath);

			// create nine-slice/nine-patch texture scene if needed
			if (entry.is9Slice)
			{
				PackedScene ninePatchScene = null;
				if (dir.FileExists(ninePatchPath))
				{
					ninePatchScene = ResourceLoader.Load<PackedScene>(ninePatchPath, "PackedScene", ResourceLoader.CacheMode.Replace);
				}

				ninePatchScene = ninePatchScene ?? new PackedScene();

				var ninePatch = new NinePatchRect();
				ninePatch.Name = textureName;
				ninePatch.Texture = atlasTexture;
				ninePatch.Size = entry.bounds.Size;
				ninePatch.RegionRect = new Rect2(Vector2.Zero, entry.bounds.Size);
				ninePatch.PatchMarginLeft = entry.split.Position.X;
				ninePatch.PatchMarginRight = entry.split.Position.Y;
				ninePatch.PatchMarginTop = entry.split.Size.X;
				ninePatch.PatchMarginBottom = entry.split.Size.Y;

				res = ninePatchScene.Pack(ninePatch);
				if (res == Error.Ok)
				{
					res = ResourceSaver.Save(ninePatchScene, ninePatchPath);
					if (res == Error.Ok)
					{
						createdFiles.Add(ninePatchPath);
					}
					else
					{
						GD.Print($"Failed to save: {ninePatchPath} => {res}");
					}
				}
				else
				{
					GD.Print($"Failed to pack NinePatchRect in: {ninePatchPath} => {res}");
				}

				ninePatch.Free();
			}
		}

		return Error.Ok;
	}

	// ----------------------------------------------------------------------------------------------------------------
}
#endif

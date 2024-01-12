#if TOOLS
using Godot;
using System;

[Tool]
public partial class GDXTexturePackerImporterPlugin : EditorPlugin
{
	private GDXAtlasTextureImporter atlasImporter;
	private GDXSprite2DImporter spriteImporter;

	// ----------------------------------------------------------------------------------------------------------------
	#region system

	public override void _EnterTree()
	{
		atlasImporter = new GDXAtlasTextureImporter();
		spriteImporter = new GDXSprite2DImporter();
		AddImportPlugin(atlasImporter);
		AddImportPlugin(spriteImporter);
	}

	public override void _ExitTree()
	{
		RemoveImportPlugin(atlasImporter);
		RemoveImportPlugin(spriteImporter);
		atlasImporter = null;
		spriteImporter = null;
	}

	#endregion
	// ----------------------------------------------------------------------------------------------------------------
}
#endif

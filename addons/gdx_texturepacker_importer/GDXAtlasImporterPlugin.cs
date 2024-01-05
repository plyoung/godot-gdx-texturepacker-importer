#if TOOLS
using Godot;
using System;

[Tool]
public partial class GDXAtlasImporterPlugin : EditorPlugin
{
	private GDXAtlasImporter importer;
	
	// ----------------------------------------------------------------------------------------------------------------
	#region system

	public override void _EnterTree()
	{
		importer = new GDXAtlasImporter();
		AddImportPlugin(importer);
	}

	public override void _ExitTree()
	{
		RemoveImportPlugin(importer);
		importer = null;
	}

	#endregion
	// ----------------------------------------------------------------------------------------------------------------
}
#endif

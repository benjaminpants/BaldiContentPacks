using System;
using System.Collections.Generic;
using System.Text;
using PlusStudioLevelLoader;
using PlusLevelStudio;
using UnityEngine;
using MTM101BaldAPI.AssetTools;
using PlusLevelStudio.Editor;
using PlusStudioLevelFormat;
using PlusLevelStudio.Editor.Tools;
using MTM101BaldAPI;

namespace PiratePack
{
    public static class PiratePackEditorSupport
    {
        public static void AddEditorContent()
        {
            AssetManager assetMan = PiratePlugin.Instance.assetMan;
            EditorInterface.AddNPCVisual("cann", assetMan.Get<NPC>("Cann"));
            EditorInterfaceModes.AddModeCallback(AddContentToMode);
            LevelStudioPlugin.Instance.selectableShopItems.AddRange(new string[] { "pshield3", "doubloon" });
        }

        public static void AddContentToMode(EditorMode mode, bool vanillaCompliant)
        {
            AssetManager assetMan = PiratePlugin.Instance.assetMan;
            // by default, AddToolToCategory doesnt create the category if it doesn't exist, so if any of these dont exist in the mode we are editing, this will do nothing.
            EditorInterfaceModes.AddToolToCategory(mode, "npcs", new NPCTool("cann", assetMan.Get<Sprite>("Editor_Cann")));
            EditorInterfaceModes.AddToolsToCategory(mode, "items", new ItemTool[]
            {
                new ItemTool("pshield3", assetMan.Get<Sprite>("Editor_Shield3")),
                new ItemTool("pshield5", assetMan.Get<Sprite>("Editor_Shield5")),
                new ItemTool("doubloon")
            });
            EditorInterfaceModes.AddToolToCategory(mode, "posters", new PosterTool(assetMan.Get<NPC>("Cann").Poster.baseTexture.name));
        }
    }

    public static class PiratePackLoaderSupport
    {
        public static void AddLoaderContent()
        {
            AssetManager assetMan = PiratePlugin.Instance.assetMan;
            LevelLoaderPlugin.Instance.npcAliases.Add("cann", assetMan.Get<NPC>("Cann"));
            LevelLoaderPlugin.Instance.itemObjects.Add("pshield3", assetMan.Get<ItemObject>("Shield3"));
            LevelLoaderPlugin.Instance.itemObjects.Add("pshield5", assetMan.Get<ItemObject>("Shield5"));
            LevelLoaderPlugin.Instance.itemObjects.Add("doubloon", assetMan.Get<ItemObject>("Doubloon"));
            LevelLoaderPlugin.Instance.posterAliases.Add(assetMan.Get<NPC>("Cann").Poster.baseTexture.name, assetMan.Get<NPC>("Cann").Poster);
        }
    }
}

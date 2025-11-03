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

namespace CarnivalPack
{
    public static class CarnivalPackEditorSupport
    {
        public static void AddEditorStuff()
        {
            AssetManager assetMan = CarnivalPackBasePlugin.Instance.assetMan;
            BalloonMayhamManagerEditor mayhemEditor = GameObject.Instantiate<BalloonMayhamManager>(assetMan.Get<BalloonMayhamManager>("BalloonMayhem"), MTM101BaldiDevAPI.prefabTransform).gameObject.SwapComponent<BalloonMayhamManager, BalloonMayhamManagerEditor>();
            mayhemEditor.name = "BalloonMayhemEditor";
            LevelStudioPlugin.Instance.gameModeAliases.Add("balloonmayhem", new EditorGameMode()
            {
                nameKey = "Ed_GameMode_BalloonMayhem",
                descKey = "Ed_GameMode_BalloonMayhem_Desc",
                hasSettingsPage = false,
                prefab = mayhemEditor,
            });
            LevelStudioPlugin.Instance.eventSprites.Add("balloonfrenzy", assetMan.Get<Sprite>("Editor_Frenzy_Icon"));
            LevelStudioPlugin.Instance.selectableTextures.Add("ZorpFloor");
            LevelStudioPlugin.Instance.selectableTextures.Add("ZorpWall");
            LevelStudioPlugin.Instance.selectableTextures.Add("ZorpCeil");
            EditorInterface.AddNPCVisual("zorpster", assetMan.Get<NPC>("Zorpster"));
            EditorInterface.AddObjectVisualWithCustomCapsuleCollider("zorp_lavalamp", LevelLoaderPlugin.Instance.basicObjects["zorp_lavalamp"], 1f, 7f, 1, Vector3.up * 3.5f);
            LevelStudioPlugin.Instance.defaultRoomTextures.Add("zorp", new TextureContainer("ZorpFloor", "ZorpWall", "ZorpCeil"));
            EditorInterfaceModes.AddModeCallback(AddContent);
        }

        public static void AddContent(EditorMode mode, bool vanillaCompliant)
        {
            AssetManager assetMan = CarnivalPackBasePlugin.Instance.assetMan;
            // by default, AddToolToCategory doesnt create the category if it doesn't exist, so if any of these dont exist in the mode we are editing, this will do nothing.
            EditorInterfaceModes.AddToolToCategory(mode, "rooms", new RoomTool("zorp", assetMan.Get<Sprite>("Editor_Zorpster_Room")));
            EditorInterfaceModes.AddToolToCategory(mode, "npcs", new NPCTool("zorpster", assetMan.Get<Sprite>("Editor_Zorpster_NPC")));
            EditorInterfaceModes.AddToolToCategory(mode, "objects", new ObjectToolNoRotation("zorp_lavalamp", assetMan.Get<Sprite>("Editor_LavaLamp")));
            EditorInterfaceModes.AddToolToCategory(mode, "items", new ItemTool("cottoncandy"));
            EditorInterfaceModes.AddToolToCategory(mode, "posters", new PosterTool(assetMan.Get<NPC>("Zorpster").Poster.baseTexture.name));
            if (mode.id == "full")
            {
                mode.availableGameModes.Add("balloonmayhem");
            }
            mode.availableRandomEvents.Add("balloonfrenzy");
        }
    }

    public static class CarnivalPackLoaderSupport
    {
        public static void AddLoaderStuff()
        {
            AssetManager assetMan = CarnivalPackBasePlugin.Instance.assetMan;
            LevelLoaderPlugin.Instance.npcAliases.Add("zorpster", assetMan.Get<NPC>("Zorpster"));
            LevelLoaderPlugin.Instance.itemObjects.Add("cottoncandy", assetMan.Get<ItemObject>("CottonCandy"));
            LevelLoaderPlugin.Instance.randomEventAliases.Add("balloonfrenzy", assetMan.Get<RandomEvent>("BalloonFrenzy"));
            LevelLoaderPlugin.Instance.posterAliases.Add(assetMan.Get<NPC>("Zorpster").Poster.baseTexture.name, assetMan.Get<NPC>("Zorpster").Poster);
        }
    }

    public class BalloonMayhamManagerEditor : BalloonMayhamManager
    {
        public override void LoadNextLevel()
        {
            EditorPlayModeManager.Instance.Win();
        }
    }
}

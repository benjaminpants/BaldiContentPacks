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
using CriminalPack.Editor;
using Rewired.Utils.Classes.Data;

namespace CriminalPack
{
    public static class CriminalPackEditorSupport
    {
        public static void AddEditorContent()
        {
            AssetManager assetMan = CriminalPackPlugin.Instance.assetMan;
            EditorInterface.AddNPCVisual("dealer", assetMan.Get<NPC>("Dealer"));
            Structure_KeycardDoors keyDoor = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors");
            EditorInterface.AddStructureGenericVisual("keycarddoor_green", keyDoor.doorPrefabs[0].gameObject);
            EditorInterface.AddStructureGenericVisual("keycarddoor_blue", keyDoor.doorPrefabs[1].gameObject);
            EditorInterface.AddStructureGenericVisual("keycarddoor_red", keyDoor.doorPrefabs[2].gameObject);
            EditorInterface.AddStructureGenericVisual("scanner", assetMan.Get<Structure_Scanner>("scanner").prefab.gameObject);
            EditorInterface.AddWindow("cellbars", assetMan.Get<WindowObject>("CellWindow"));
            LevelStudioPlugin.Instance.structureTypes.Add("keycard_door", typeof(HallDoorStructureLocation));
            LevelStudioPlugin.Instance.structureTypes.Add("scanner", typeof(HallDoorStructureLocation));
            LevelStudioPlugin.Instance.selectableTextures.Add("PrisonFloor");
            LevelStudioPlugin.Instance.selectableTextures.Add("PrisonWall");
            EditorInterfaceModes.AddModeCallback(AddContentToMode);
            EditorLevelData.AddDefaultTextureAction((Dictionary<string, TextureContainer> dict) =>
            {
                dict.Add("jailcell", new TextureContainer("PrisonFloor", "PrisonWall", "PrisonFloor"));
            });
            EditorInterface.AddRoomVisualManager<CellRoomVisualManager>("jailcell");
        }

        public static void AddContentToMode(EditorMode mode, bool vanillaCompliant)
        {
            AssetManager assetMan = CriminalPackPlugin.Instance.assetMan;
            // by default, AddToolToCategory doesnt create the category if it doesn't exist, so if any of these dont exist in the mode we are editing, this will do nothing.
            EditorInterfaceModes.AddToolToCategory(mode, "rooms", new RoomTool("jailcell", assetMan.Get<Sprite>("Editor_JailCell")));
            EditorInterfaceModes.AddToolToCategory(mode, "posters", new PosterTool(assetMan.Get<NPC>("Dealer").Poster.baseTexture.name));
            EditorInterfaceModes.AddToolToCategory(mode, "npcs", new NPCTool("dealer", assetMan.Get<Sprite>("Editor_Dealer")));
            EditorInterfaceModes.AddToolsToCategory(mode, "items", new ItemTool[]
            {
                new ItemTool("crowbar"),
                new ItemTool("thief_mask"),
                new ItemTool("empty_dealer_pouch"),
                new ItemTool("iou"),
                new ItemTool("iou_decoy"),
                new ItemTool("keycard_green"),
                new ItemTool("keycard_blue"),
                new ItemTool("keycard_red")
            });
            EditorInterfaceModes.AddToolsToCategory(mode, "structures", new EditorTool[]
            {
                new HallDoorStructureTool("scanner", assetMan.Get<Sprite>("Editor_Scanner")),
                new HallDoorStructureTool("keycard_door", "keycarddoor_green", assetMan.Get<Sprite>("Editor_KeycardDoorGreen")),
                new HallDoorStructureTool("keycard_door", "keycarddoor_blue", assetMan.Get<Sprite>("Editor_KeycardDoorBlue")),
                new HallDoorStructureTool("keycard_door", "keycarddoor_red", assetMan.Get<Sprite>("Editor_KeycardDoorRed")),
            });
            EditorInterfaceModes.InsertToolInCategory(mode, "doors", "window_standard", new WindowTool("cellbars", assetMan.Get<Sprite>("Editor_Cellbars")));
        }

        public static void AddScannerPosterToEditor(PosterObject obj, string itemName)
        {
            EditorInterfaceModes.AddModeCallback((EditorMode mode, bool vanillaCompliant) =>
            {
                EditorInterfaceModes.AddToolToCategory(mode, "posters", new ScannerPosterTool(obj.name, itemName));
            });
        }
    }

    public static class CriminalPackLoaderSupport
    {
        public static void AddLoaderContent()
        {
            AssetManager assetMan = CriminalPackPlugin.Instance.assetMan;
            LevelLoaderPlugin.Instance.npcAliases.Add("dealer", assetMan.Get<NPC>("Dealer"));
            LevelLoaderPlugin.Instance.posterAliases.Add(assetMan.Get<NPC>("Dealer").Poster.baseTexture.name, assetMan.Get<NPC>("Dealer").Poster);

            // items
            LevelLoaderPlugin.Instance.itemObjects.Add("empty_dealer_pouch", assetMan.Get<ItemObject>("PouchEmpty"));
            LevelLoaderPlugin.Instance.itemObjects.Add("crowbar", assetMan.Get<ItemObject>("Crowbar"));
            LevelLoaderPlugin.Instance.itemObjects.Add("thief_mask", assetMan.Get<ItemObject>("Mask"));
            LevelLoaderPlugin.Instance.itemObjects.Add("iou", assetMan.Get<ItemObject>("IOU"));
            LevelLoaderPlugin.Instance.itemObjects.Add("iou_decoy", assetMan.Get<ItemObject>("IOUDecoy"));

            Structure_KeycardDoors keyDoor = assetMan.Get<Structure_KeycardDoors>("Structure_KeycardDoors");
            LevelLoaderPlugin.Instance.structureAliases.Add("keycard_door", new LoaderStructureData(keyDoor, new Dictionary<string, GameObject>()
            {
                { "keycarddoor_green", keyDoor.doorPrefabs[0].gameObject },
                { "keycarddoor_blue", keyDoor.doorPrefabs[1].gameObject },
                { "keycarddoor_red", keyDoor.doorPrefabs[2].gameObject }
            }));
            LevelLoaderPlugin.Instance.structureAliases.Add("scanner", new LoaderStructureData(assetMan.Get<Structure_Scanner>("scanner"), new Dictionary<string, GameObject>()));
            // add keycard items
            LevelLoaderPlugin.Instance.itemObjects.Add("keycard_green", keyDoor.keycardItems[0]);
            LevelLoaderPlugin.Instance.itemObjects.Add("keycard_blue", keyDoor.keycardItems[1]);
            LevelLoaderPlugin.Instance.itemObjects.Add("keycard_red", keyDoor.keycardItems[2]);
            LevelLoaderPlugin.Instance.windowObjects.Add("cellbars", assetMan.Get<WindowObject>("CellWindow"));
            LevelLoaderPlugin.Instance.roomTextureAliases.Add("PrisonFloor", assetMan.Get<Texture2D>("PrisonFloor"));
            LevelLoaderPlugin.Instance.roomTextureAliases.Add("PrisonWall", assetMan.Get<Texture2D>("PrisonWall"));
            RoomAsset cellRoomAsset = assetMan.Get<RoomAsset>("CellBlock");
            LevelLoaderPlugin.Instance.roomSettings.Add("jailcell", new RoomSettings(cellRoomAsset.category, cellRoomAsset.type, cellRoomAsset.color, cellRoomAsset.doorMats, cellRoomAsset.mapMaterial)
            {
                container = cellRoomAsset.roomFunctionContainer
            });
        }

        public static void AddScannerPosterToLoader(PosterObject obj)
        {
            LevelLoaderPlugin.Instance.posterAliases.Add(obj.name, obj);
        }
    }
}

namespace CriminalPack.Editor
{
    public class ScannerPosterTool : PosterTool
    {
        public string name;
        public override string titleKey
        {
            get
            {
                return string.Format(LocalizationManager.Instance.GetLocalizedText("Ed_ScannerPoster_Title"), LocalizationManager.Instance.GetLocalizedText(name));
            }
        }
        public override string descKey
        {
            get
            {
                return string.Format(name.EndsWith("s") ? LocalizationManager.Instance.GetLocalizedText("Ed_ScannerPoster_Desc") : LocalizationManager.Instance.GetLocalizedText("Ed_ScannerPoster_S_Desc"), LocalizationManager.Instance.GetLocalizedText(name));
            }
        }
        public ScannerPosterTool(string type, string name) : base(type)
        {
            this.name = name;
        }
    }

    public class CellRoomVisualManager : EditorRoomVisualManager
    {
        public override void ModifyLightsForEditor(EnvironmentController workerEc)
        {
            UpdateDoors();
        }

        public override void RoomUpdated()
        {
            base.RoomUpdated();
            UpdateDoors();
        }

        public void UpdateDoors()
        {
            return; // PLACEHOLDER, TO AVOID A BUG UNTIL I HAVE TIME TO FIX IT
            for (int i = 0; i < myLevelData.doors.Count; i++)
            {
                if (myLevelData.doors[i].DoorConnectedToRoom(myLevelData, myRoom, true))
                {
                    if (myLevelData.doors[i].GetVisualPrefab().TryGetComponent<StandardDoorDisplay>(out _))
                    {
                        StandardDoorDisplay doorDisp = EditorController.Instance.GetVisual(myLevelData.doors[i]).GetComponent<StandardDoorDisplay>();
                        StandardDoorMats doorMat = LevelLoaderPlugin.Instance.roomSettings[myRoom.roomType].doorMat;
                        StandardDoorMats doorMat2 = LevelLoaderPlugin.Instance.roomSettings[myRoom.roomType].doorMat;
                        MaterialModifier.ChangeOverlay(doorDisp.sideB, doorMat.shut);
                        MaterialModifier.ChangeOverlay(doorDisp.sideA, doorMat2.shut);
                    }
                }
            }
        }
    }
}
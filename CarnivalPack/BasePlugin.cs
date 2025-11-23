using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.AssetTools.SpriteSheets;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.OptionsAPI;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using MTM101BaldAPI.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PlusStudioLevelLoader;
using PlusStudioLevelFormat;
using MTM101BaldAPI.Components.Animation;

namespace CarnivalPack
{
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudio", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("mtm101.rulerp.baldiplus.levelstudioloader", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("mtm101.rulerp.bbplus.baldidevapi")]
    [BepInPlugin("mtm101.rulerp.bbplus.carnivalpackroot", "Carnival Pack", "3.0.1.0")]
    public class CarnivalPackBasePlugin : BaseUnityPlugin
    {
        public static CarnivalPackBasePlugin Instance;

        public Dictionary<string, SpriteAnimation> zorpsterAnimations;

        public ConfigEntry<bool> youtuberModeEnabled;
        public ConfigEntry<bool> balloonFrenzyEnabled;
        public ConfigEntry<bool> balloonMayhamTestEnabled;

        public AssetManager assetMan = new AssetManager();
        
        public static RoomCategory zorpsterRoomCat = EnumExtensions.ExtendEnum<RoomCategory>("ZorpRoom");


        void AddAudioFolderToAssetMan(Color subColor, params string[] path)
        {
            string[] paths = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < paths.Length; i++)
            {
                assetMan.Add<SoundObject>("Aud_" + Path.GetFileNameWithoutExtension(paths[i]), ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromFile(paths[i]), Path.GetFileNameWithoutExtension(paths[i]), SoundType.Voice, subColor));
            }
        }

        void AddSpriteFolderToAssetMan(string prefix = "", float pixelsPerUnit = 40f, params string[] path)
        {
            string[] paths = Directory.GetFiles(Path.Combine(path));
            for (int i = 0; i < paths.Length; i++)
            {
                assetMan.Add<Sprite>(prefix + Path.GetFileNameWithoutExtension(paths[i]), AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromFile(paths[i]), pixelsPerUnit));
            }
        }

        public static string balloonMayhamMidi;
        public static RandomEventType balloonFrenzyEventEnum;
        void RegisterImportant()
        {
            assetMan.Add<Texture2D>("Texture_Zorpster_Idle", AssetLoader.TextureFromMod(this, "ZorpPlaceholder.png"));
            assetMan.Add<Sprite>("Zorpster_Idle", AssetLoader.SpriteFromTexture2D(assetMan.Get<Texture2D>("Texture_Zorpster_Idle"), 40));
            assetMan.Add<SoundObject>("Zorpster_Sound_Idle", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "weirdwahah.wav"), "Sfx_WeirdWahah", SoundType.Effect, Color.white));
            assetMan.Add<SoundObject>("Inflate", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Inflate.wav"), "Sfx_InflateFrenzy", SoundType.Effect, Color.white));
            assetMan.Add<Texture2D>("ZorpWall", AssetLoader.TextureFromMod(this, "Map", "ZorpWall.png"));
            assetMan.Add<Texture2D>("ZorpCeil", AssetLoader.TextureFromMod(this, "Map", "ZorpCeil.png"));
            assetMan.Add<Texture2D>("ZorpFloor", AssetLoader.TextureFromMod(this, "Map", "ZorpFloor.png"));
            assetMan.Add<Sprite>("Tractor1", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "tractor1.png"), 30));
            assetMan.Add<Sprite>("Tractor2", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "tractor2.png"), 30));
            assetMan.Add<Sprite>("Tractor3", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "tractor3.png"), 30));
            assetMan.Add<Sprite>("Tractor4", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "tractor4.png"), 30));
            assetMan.Add<Sprite>("CottonCandySmall", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "CottonCandySmall.png"), 25f));
            assetMan.Add<Sprite>("CottonCandyBig", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "CottonCandyBig.png"), 50f));
            assetMan.Add<Sprite>("Staminometer_Cotton", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Staminometer_Cotton.png"), 50f));
            assetMan.Add<Sprite>("Editor_Zorpster_Room", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Editor", "room_zorp.png"), 1f));
            assetMan.Add<Sprite>("Editor_Zorpster_NPC", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Editor", "npc_zorpster.png"), 1f));
            assetMan.Add<Sprite>("Editor_LavaLamp", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Editor", "object_zorp_lavalamp.png"), 1f));
            assetMan.Add<Sprite>("Editor_Frenzy_Icon", AssetLoader.SpriteFromTexture2D(AssetLoader.TextureFromMod(this, "Editor", "event_balloonfrenzy.png"), 1f));
            AssetLoader.LocalizationFromMod(this);
            if (balloonMayhamTestEnabled.Value)
            {
                AssetLoader.LocalizationFromFile(Path.Combine(AssetLoader.GetModPath(this), "BalloonMayham.json"), Language.English);
            }
            balloonMayhamMidi = AssetLoader.MidiFromMod("balloonMayham", this, "Midi", "BalloonMayham.mid");


            GameObject lavaLampBase = GameObject.Instantiate<GameObject>(Resources.FindObjectsOfTypeAll<EnvironmentObject>().First(x => x.name == "Plant" && x.GetInstanceID() >= 0 && x.transform.parent == null).gameObject, MTM101BaldiDevAPI.prefabTransform);
            lavaLampBase.GetComponentInChildren<SpriteRenderer>().sprite = AssetLoader.SpriteFromMod(this, new Vector2(0.5f, 0f), 15f, "LavaLamp.png");
            lavaLampBase.name = "LavaLamp";
            assetMan.Add<GameObject>("LavaLamp", lavaLampBase);

            assetMan.Add<StandardDoorMats>("ZorpDoorMats", ObjectCreators.CreateDoorDataObject("ZorpDoor", AssetLoader.TextureFromMod(this, "Map", "ZorpDoor_Open.png"), AssetLoader.TextureFromMod(this, "Map", "ZorpDoor_Closed.png")));
            LevelLoaderPlugin.Instance.roomSettings.Add("zorp", new RoomSettings(CarnivalPackBasePlugin.zorpsterRoomCat, RoomType.Room, new Color(172f / 255f, 0f, 252f / 255f), assetMan.Get<StandardDoorMats>("ZorpDoorMats"), null));
            LevelLoaderPlugin.Instance.basicObjects.Add("zorp_lavalamp", assetMan.Get<GameObject>("LavaLamp"));
            LevelLoaderPlugin.Instance.roomTextureAliases.Add("ZorpFloor", assetMan.Get<Texture2D>("ZorpFloor"));
            LevelLoaderPlugin.Instance.roomTextureAliases.Add("ZorpWall", assetMan.Get<Texture2D>("ZorpWall"));
            LevelLoaderPlugin.Instance.roomTextureAliases.Add("ZorpCeil", assetMan.Get<Texture2D>("ZorpCeil"));

            string[] roomPaths = Directory.GetFiles(Path.Combine(AssetLoader.GetModPath(this), "Rooms"), "*.rbpl");
            List<WeightedRoomAsset> potentialZorpsterRooms = new List<WeightedRoomAsset>();
            for (int i = 0; i < roomPaths.Length; i++)
            {
                BinaryReader reader = new BinaryReader(File.OpenRead(roomPaths[i]));
                BaldiRoomAsset formatAsset = BaldiRoomAsset.Read(reader);
                reader.Close();
                ExtendedRoomAsset asset = LevelImporter.CreateRoomAsset(formatAsset);

                if (formatAsset.basicObjects.Find(x => x.prefab == "locker") != null)
                {
                    asset.basicSwaps = new List<BasicObjectSwapData>()
                    {
                        new BasicObjectSwapData()
                        {
                            chance = 0.05f,
                            potentialReplacements = new WeightedTransform[]
                            {
                                new WeightedTransform()
                                {
                                    weight = 100,
                                    selection = LevelLoaderPlugin.Instance.basicObjects["bluelocker"].transform
                                }
                            },
                            prefabToSwap = LevelLoaderPlugin.Instance.basicObjects["locker"].transform
                        }
                    };
                }
                asset.lightPre = LevelLoaderPlugin.Instance.lightTransforms["standardhanging"];
                potentialZorpsterRooms.Add(new WeightedRoomAsset()
                {
                    selection = asset,
                    weight = 100
                });
            }


            Zorpster Zorp = new NPCBuilder<Zorpster>(Info)
                .SetName("Zorpster")
                .SetEnum("Zorp")
                .SetAirborne()
                .IgnorePlayerOnSpawn()
                .AddLooker()
                .AddTrigger()
                .AddSpawnableRoomCategories(zorpsterRoomCat)
                .AddPotentialRoomAssets(potentialZorpsterRooms.ToArray())
                .SetPoster(AssetLoader.TextureFromMod(this, "zorpster_poster.png"), "PST_PRI_Zorpster1", "PST_PRI_Zorpster2")
                .SetMetaTags(new string[] { "adv_ev_cold_school_immunity" })
                .Build();

            Zorp.spriteRenderer[0].gameObject.transform.localPosition += Vector3.up;
            Zorp.audMan = Zorp.GetComponent<AudioManager>();
            Zorp.wahahAudMan = Zorp.gameObject.AddComponent<PropagatedAudioManager>();
            Zorp.wahahAudMan.ReflectionSetVariable("soundOnStart", new SoundObject[] { assetMan.Get<SoundObject>("Zorpster_Sound_Idle") });
            Zorp.wahahAudMan.ReflectionSetVariable("loopOnStart", true);
            Zorp.spriteRenderer[0].sprite = assetMan.Get<Sprite>("Zorpster_Idle");
            Zorp.discoverSounds.AddRange(new SoundObject[] { assetMan.Get<SoundObject>("Aud_Zorp_Discover1"), assetMan.Get<SoundObject>("Aud_Zorp_Discover2") });
            Zorp.lostSounds.AddRange(new SoundObject[] { assetMan.Get<SoundObject>("Aud_Zorp_Lost1"), assetMan.Get<SoundObject>("Aud_Zorp_Lost2"), assetMan.Get<SoundObject>("Aud_Zorp_Lost3") });
            Zorp.goodSubjectSounds.AddRange(new SoundObject[] { assetMan.Get<SoundObject>("Aud_Zorp_Correct1"), assetMan.Get<SoundObject>("Aud_Zorp_Correct2"), assetMan.Get<SoundObject>("Aud_Zorp_Correct3") });
            Zorp.badSubjectSounds.AddRange(new SoundObject[] { assetMan.Get<SoundObject>("Aud_Zorp_Wrong1"), assetMan.Get<SoundObject>("Aud_Zorp_Wrong2")});
            Zorp.jammedSounds.AddRange(new SoundObject[] { assetMan.Get<SoundObject>("Aud_Zorp_Jammed1"), assetMan.Get<SoundObject>("Aud_Zorp_Jammed2"), assetMan.Get<SoundObject>("Aud_Zorp_Jammed3"), assetMan.Get<SoundObject>("Aud_Zorp_Jammed4") });
            Zorp.doneSound = assetMan.Get<SoundObject>("Aud_Zorp_Done1");
            Zorp.escapeSound = assetMan.Get<SoundObject>("Aud_Zorp_Escape");

            // ANIMATOR!
            CustomSpriteRendererAnimator animator = Zorp.gameObject.AddComponent<CustomSpriteRendererAnimator>();
            animator.renderer = Zorp.spriteRenderer[0];
            animator.LoadAnimations(CarnivalPackBasePlugin.Instance.zorpsterAnimations);
            animator.timeScale = TimeScaleType.Npc;
            Zorp.animator = animator;

            assetMan.Add<Zorpster>("Zorpster", Zorp);

            ItemObject cottonCandy = new ItemBuilder(Info)
                .SetNameAndDescription("Itm_CottonCandy", "Desc_CottonCandy")
                .SetSprites(assetMan.Get<Sprite>("CottonCandySmall"), assetMan.Get<Sprite>("CottonCandyBig"))
                .SetEnum("CottonCandy")
                .SetShopPrice(480)
                .SetGeneratorCost(40)
                .SetItemComponent<ITM_CottonCandy>()
                .SetMeta(ItemFlags.Persists, new string[] { "food" })
                .Build();
            ((ITM_CottonCandy)cottonCandy.item).eatSound = (SoundObject)((ITM_ZestyBar)ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).value.item).ReflectionGetVariable("audEat");
            assetMan.Add<ItemObject>("CottonCandy", cottonCandy);


            // setup the balloon
            Balloon balloonTemplate = Resources.FindObjectsOfTypeAll<Balloon>().First(x => x.GetInstanceID() >= 0 && x.name == "Balloon_1");
            FrenzyBalloon balloonFrenzyBalloon = GameObject.Instantiate<Balloon>(balloonTemplate, MTM101BaldiDevAPI.prefabTransform).gameObject.AddComponent<FrenzyBalloon>();
            balloonFrenzyBalloon.name = "BalloonFrenzyBalloonStandard";
            balloonFrenzyBalloon.gameObject.layer = LayerMask.NameToLayer("ClickableCollidableEntities");
            balloonFrenzyBalloon.audMan = balloonFrenzyBalloon.gameObject.AddComponent<PropagatedAudioManager>();
            balloonFrenzyBalloon.popSound = Resources.FindObjectsOfTypeAll<SoundObject>().First(x => x.name == "Gen_Pop");
            balloonFrenzyBalloon.inflateSound = assetMan.Get<SoundObject>("Inflate");
            balloonFrenzyBalloon.potentialSprites = new Sprite[]
            {
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonRegularRed.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonRegularGreen.png"),
                AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonRegularBlue.png")
            };
            assetMan.Add<FrenzyBalloon>("FrenzyBalloon", balloonFrenzyBalloon);

            SoundObject frenzyEventSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "BaldiAnnouncementBalloonFrenzy.wav"), "Vfx_BAL_Event_BalloonFrenzy_1", SoundType.Voice, Color.green);

            frenzyEventSound.additionalKeys = new SubtitleTimedKey[]
            {
                new SubtitleTimedKey()
                {
                    encrypted=false,
                    key="Vfx_BAL_Event_BalloonFrenzy_2",
                    time=5.5f
                }
            };


            balloonFrenzyEventEnum = EnumExtensions.ExtendEnum<RandomEventType>("BalloonFrenzy");
            BalloonFrenzy frenzyEvent = new RandomEventBuilder<BalloonFrenzy>(Info)
                .SetEnum(balloonFrenzyEventEnum)
                .SetMinMaxTime(90f, 120f)
                .SetName("Balloon_Frenzy")
                .SetSound(frenzyEventSound)
                .Build();
            frenzyEvent.standardBalloons.Add(new WeightedSelection<FrenzyBalloon>()
            {
                selection = balloonFrenzyBalloon,
                weight = 300
            });

            assetMan.Add<BalloonFrenzy>("FrenzyEvent", frenzyEvent);

            FrenzyBalloonPoints bpBalloon = CreateBalloonVariant<FrenzyBalloonPoints>(AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonPoints.png"));
            bpBalloon.popSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Gen_PopPoints.wav"), "Sfx_Effects_Pop", SoundType.Effect, Color.white);
            frenzyEvent.standardBalloons.Add(new WeightedSelection<FrenzyBalloon>()
            {
                selection= bpBalloon,
                weight = 40
            });
            FrenzyBalloonExplosion explodeBalloon = CreateBalloonVariant<FrenzyBalloonExplosion>(AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonBoom.png"));
            explodeBalloon.popSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Gen_PopExplosion.wav"), "Sfx_Effects_Pop", SoundType.Effect, Color.white);
            frenzyEvent.standardBalloons.Add(new WeightedSelection<FrenzyBalloon>()
            {
                selection = explodeBalloon,
                weight = 30
            });

            FrenzyBalloonSquish squishBalloon = CreateBalloonVariant<FrenzyBalloonSquish>(AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonSquish.png"));
            squishBalloon.popSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Gen_PopSquish.wav"), "Sfx_Effects_Pop", SoundType.Effect, Color.white);
            frenzyEvent.standardBalloons.Add(new WeightedSelection<FrenzyBalloon>()
            {
                selection = squishBalloon,
                weight = 25
            });

            FrenzyBalloonSpeedboost speedBalloon = CreateBalloonVariant<FrenzyBalloonSpeedboost>(AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 25f, "Balloons", "BalloonSpeedboost.png"));
            speedBalloon.popSound = ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "Gen_PopSpeed.wav"), "Sfx_Effects_Pop", SoundType.Effect, Color.white);
            frenzyEvent.standardBalloons.Add(new WeightedSelection<FrenzyBalloon>()
            {
                selection = speedBalloon,
                weight = 50
            });

            assetMan.Add<RandomEvent>("BalloonFrenzy", frenzyEvent);

            assetMan.Add<SoundObject>("PrincipalNotPopBalloon", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "PRI_NoNotPoppingBalloons.wav"), "Vfx_PRI_NoNotBalloonPop", SoundType.Voice, Color.white));


            NPCMetaStorage.Instance.Get(Character.Baldi).tags.Add("lower_balloon_frenzy_priority");
            NPCMetaStorage.Instance.Get(Character.Principal).tags.Add("no_balloon_frenzy");
            NPCMetaStorage.Instance.Get(Character.Crafters).tags.Add("no_balloon_frenzy");
            NPCMetaStorage.Instance.Get(Character.Chalkles).tags.Add("no_balloon_frenzy");
            NPCMetaStorage.Instance.Get(Character.LookAt).tags.Add("no_balloon_frenzy");
            NPCMetaStorage.Instance.Get(Character.Bully).tags.Add("no_balloon_frenzy");
            NPCMetaStorage.Instance.Get(Character.Sweep).tags.Add("no_balloon_frenzy");

            HudManager hudM = Resources.FindObjectsOfTypeAll<HudManager>().First(x => x.GetInstanceID() >= 0 && x.name == "MainHud");
            Image balloonFrenzyUIBalloon = UIHelpers.CreateImage(AssetLoader.SpriteFromMod(this, Vector2.zero, 30f, "BalloonUI.png"), hudM.transform, Vector3.zero, false);
            balloonFrenzyUIBalloon.name = "BalloonFrenzyBalloon";
            balloonFrenzyUIBalloon.rectTransform.anchorMax = Vector2.one;
            balloonFrenzyUIBalloon.rectTransform.anchorMin = Vector2.one;
            balloonFrenzyUIBalloon.rectTransform.anchoredPosition = new Vector2(50f, -225f);

            TextMeshProUGUI text = UIHelpers.CreateText<TextMeshProUGUI>(BaldiFonts.ComicSans18, "0:00", balloonFrenzyUIBalloon.transform, Vector3.zero);
            text.color = Color.white;
            BalloonFrenzyUI frUI = hudM.gameObject.AddComponent<BalloonFrenzyUI>();
            frUI.balloonImage = balloonFrenzyUIBalloon;
            frUI.text = text;
            text.rectTransform.anchorMin = Vector2.one / 2f;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.anchoredPosition = new Vector2(85f, -75f);

            BalloonMayhamEvent frenzyEventDedicated = new RandomEventBuilder<BalloonMayhamEvent>(Info)
                .SetEnum("BalloonFrenzyDedicated")
                .SetMinMaxTime(90f, 120f)
                .SetName("Balloon_Frenzy_Dedicated")
                .SetSound(frenzyEventSound)
                .SetMeta(RandomEventFlags.Special)
                .Build();

            frenzyEventDedicated.npcsNeedBalloons = false;
            frenzyEventDedicated.standardBalloons = new List<WeightedSelection<FrenzyBalloon>>()
            {
                new WeightedSelection<FrenzyBalloon>()
                {
                    selection = balloonFrenzyBalloon,
                    weight = 100
                },
                new WeightedSelection<FrenzyBalloon>()
                {
                    selection = explodeBalloon,
                    weight = 5
                }
            };

            // setup balloon frenzy game manager
            MainGameManager managerTemplate = GameObject.Instantiate<MainGameManager>(Resources.FindObjectsOfTypeAll<MainGameManager>().First(x => x.GetInstanceID() >= 0 && x.name == "Lvl3_MainGameManager 1"), MTM101BaldiDevAPI.prefabTransform);
            BalloonMayhamManager frenzyManager = managerTemplate.gameObject.AddComponent<BalloonMayhamManager>();
            frenzyManager.name = "BallooonFrenzyGameManager";
            frenzyManager.ReflectionSetVariable("ambience", managerTemplate.ReflectionGetVariable("ambience"));
            frenzyManager.ReflectionSetVariable("happyBaldiPre", managerTemplate.ReflectionGetVariable("happyBaldiPre"));
            frenzyManager.ReflectionSetVariable("elevatorScreenPre", managerTemplate.ReflectionGetVariable("elevatorScreenPre"));
            frenzyManager.ReflectionSetVariable("pitstop", managerTemplate.ReflectionGetVariable("pitstop"));
            frenzyManager.ReflectionSetVariable("destroyOnLoad", true);
            frenzyManager.timeUpSound = Resources.FindObjectsOfTypeAll<SoundObject>().First(x => x.GetInstanceID() >= 0 && x.name == "TimeLimitBell");
            frenzyManager.eventPrefab = frenzyEventDedicated;
            assetMan.Add<BaseGameManager>("BalloonMayhem", frenzyManager);
            GameObject.Destroy(managerTemplate);

            ModdedHighscoreManager.AddModToList(Info, CarnivalPackBasePlugin.Instance.balloonFrenzyEnabled.Value ? null : new string[1] { "FrenzyDisabled" });
            // below are the hacks used to playtest balloon mayham

            if (balloonMayhamTestEnabled.Value)
            {
                // hacky thing for testing
                Resources.FindObjectsOfTypeAll<SceneObject>().Where(x => x.manager is MainGameManager).Do(x =>
                {
                    x.manager = frenzyManager;
                });
            }

            CarnivalPackLoaderSupport.AddLoaderStuff();
            if (Chainloader.PluginInfos.ContainsKey("mtm101.rulerp.baldiplus.levelstudio"))
            {
                CarnivalPackEditorSupport.AddEditorStuff();
            }

        }

        public T CreateBalloonVariant<T>(Sprite sprite) where T : FrenzyBalloon
        {
            FrenzyBalloon baseBalloon = GameObject.Instantiate<FrenzyBalloon>(assetMan.Get<FrenzyBalloon>("FrenzyBalloon"), MTM101BaldiDevAPI.prefabTransform);
            T newBalloon = baseBalloon.gameObject.AddComponent<T>();
            newBalloon.audMan = baseBalloon.audMan;
            newBalloon.inflateSound = baseBalloon.inflateSound;
            newBalloon.popSound = baseBalloon.popSound;
            newBalloon.potentialSprites = new Sprite[] { sprite };
            Destroy(baseBalloon);
            return newBalloon;
        }

        IEnumerator DisableFrenzyForPotentiallyProblematicNPCs()
        {
            yield return 1;
            yield return "Detecting and tagging potentially problematic modded NPCs...";
            NPCMetaStorage.Instance.FindAll(x => ((!x.flags.HasFlag(NPCFlags.CanMove) || (!x.flags.HasFlag(NPCFlags.HasTrigger))) && !x.tags.Contains("no_balloon_frenzy"))).Do(x => x.tags.Add("no_balloon_frenzy"));
        }

        void AddNPCs(string floorName, int floorNumber, SceneObject sceneObject)
        {
            CustomLevelObject[] levelObjects = sceneObject.GetCustomLevelObjects();
            if (!youtuberModeEnabled.Value)
            {
                if (floorName == "F1")
                {
                    sceneObject.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<NPC>("Zorpster"), weight = 100 });
                }
                if (floorName == "F2")
                {
                    sceneObject.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<NPC>("Zorpster"), weight = 25 }); // surprise zorpster
                }
            }
            else
            {
                if (floorName == "F1")
                {
                    sceneObject.forcedNpcs = sceneObject.forcedNpcs.AddToArray(assetMan.Get<NPC>("Zorpster"));
                    sceneObject.additionalNPCs = Mathf.Max(sceneObject.additionalNPCs - 1, 0);
                }
            }
            if (floorName.StartsWith("F"))
            {
                for (int i = 0; i < levelObjects.Length; i++)
                {
                    if (levelObjects[i].IsModifiedByMod(Info)) continue;
                    levelObjects[i].potentialItems = levelObjects[i].potentialItems.AddItem(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("CottonCandy"), weight = 80 }).ToArray();
                    if (balloonFrenzyEnabled.Value)
                    {
                        levelObjects[i].randomEvents.Add(new WeightedRandomEvent()
                        {
                            selection = assetMan.Get<BalloonFrenzy>("FrenzyEvent"),
                            weight = 50
                        });
                    }
                    levelObjects[i].MarkAsModifiedByMod(Info);
                }
                if (floorNumber >= 1)
                {
                    sceneObject.shopItems = sceneObject.shopItems.AddItem(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("CottonCandy"), weight = 75 }).ToArray();
                    sceneObject.MarkAsNeverUnload();
                }
                sceneObject.MarkAsNeverUnload();
            }
            if (sceneObject.GetMeta().tags.Contains("endless")) //endless map
            {
                sceneObject.potentialNPCs.Add(new WeightedNPC() { selection = assetMan.Get<NPC>("Zorpster"), weight = 100 });
                sceneObject.MarkAsNeverUnload();
                for (int i = 0; i < levelObjects.Length; i++)
                {
                    if (levelObjects[i].IsModifiedByMod(Info)) continue;
                    levelObjects[i].potentialItems = levelObjects[i].potentialItems.AddItem(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("CottonCandy"), weight = 80 }).ToArray();
                    if (balloonFrenzyEnabled.Value)
                    {
                        levelObjects[i].randomEvents.Add(new WeightedRandomEvent()
                        {
                            selection = assetMan.Get<BalloonFrenzy>("FrenzyEvent"),
                            weight = 50
                        });
                    }
                    levelObjects[i].MarkAsModifiedByMod(Info);
                }
                sceneObject.shopItems = sceneObject.shopItems.AddItem(new WeightedItemObject() { selection = assetMan.Get<ItemObject>("CottonCandy"), weight = 75 }).ToArray();
            }

        }

        IEnumerator PreLoadBulk()
        {
            yield return 2;
            yield return "Loading Zorpster Sprites...";
            zorpsterAnimations = SpriteSheetLoader.LoadAsepriteAnimationsFromFile(Path.Combine(AssetLoader.GetModPath(this), "Zorpster.json"), 40f, Vector2.one / 2f);
            //AddSpriteFolderToAssetMan("", 40f, AssetLoader.GetModPath(this), "ZorpAnim");
            yield return "Loading Zorpster Audio...";
            AddAudioFolderToAssetMan(new Color(107f / 255f, 193f / 255f, 27 / 255f), AssetLoader.GetModPath(this), "ZorpLines");
            yield break;
        }

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.bbplus.carnivalpackroot");
            harmony.PatchAllConditionals();
            
            //AddSpriteFolderToAssetMan("", 40f, AssetLoader.GetModPath(this), "ZorpAnim");
            //AddAudioFolderToAssetMan(new Color(107f/255f,193f/255f,27/255f), AssetLoader.GetModPath(this), "ZorpLines");
            LoadingEvents.RegisterOnAssetsLoaded(Info, RegisterImportant, LoadingEventOrder.Pre);
            LoadingEvents.RegisterOnAssetsLoaded(Info, PreLoadBulk(), LoadingEventOrder.Start);
            LoadingEvents.RegisterOnAssetsLoaded(Info, DisableFrenzyForPotentiallyProblematicNPCs(), LoadingEventOrder.Post);
            GeneratorManagement.Register(this, GenerationModType.Addend, AddNPCs);
            Instance = this;

            youtuberModeEnabled = Config.Bind<bool>("General", "Youtuber Mode", false, "If true, Zorpster will always appear on Floor 1.");
            balloonMayhamTestEnabled = Config.Bind<bool>("General", "Balloon Mayham", false, "If true, Hide and Seek will be replaced with Balloon Mayham.");
            balloonFrenzyEnabled = Config.Bind<bool>("Generation", "Balloon Frenzy Enabled", true, "If false, the balloon frenzy event will be disabled. Use if it causes performance problems.");

            if (balloonMayhamTestEnabled.Value)
            {
                GeneratorManagement.Register(this, GenerationModType.Finalizer, BalloonMayhamFinalizer);
            }

            ModdedSaveGame.AddSaveHandler(new CarnivalPackSaveGameIO());
        }

        void BalloonMayhamFinalizer(string floorName, int floorNumber, SceneObject sceneObject)
        {
            CustomLevelObject[] cml = sceneObject.GetCustomLevelObjects();
            for (int i = 0; i < cml.Length; i++)
            {
                CustomLevelObject level = cml[i];
                StructureWithParameters[] strucWithParms = level.forcedStructures.Where(x => x.prefab is Structure_PowerLever).ToArray();
                for (int j = 0; j < strucWithParms.Length; j++)
                {
                    strucWithParms[j].parameters.minMax[0] = new IntVector2(1, 1); // significantly nerf power out because with balloon frenzy its nightmare
                }
            }
        }
    }

    public class CarnivalPackSaveGameIO : ModdedSaveGameIOBinary
    {
        public override PluginInfo pluginInfo => CarnivalPackBasePlugin.Instance.Info;

        public override void Load(BinaryReader reader)
        {
            reader.ReadByte();
        }

        public override void Reset()
        {
            
        }

        public override void Save(BinaryWriter writer)
        {
            writer.Write((byte)0);
        }

        public override string[] GenerateTags()
        {
            List<string> generatedTags = new List<string>();
            if (CarnivalPackBasePlugin.Instance.youtuberModeEnabled.Value)
            {
                generatedTags.Add("YoutuberMode");
            }
            if (CarnivalPackBasePlugin.Instance.balloonMayhamTestEnabled.Value)
            {
                generatedTags.Add("BalloonMayham");
            }
            if (!CarnivalPackBasePlugin.Instance.balloonFrenzyEnabled.Value)
            {
                generatedTags.Add("NoFrenzy");
            }
            return generatedTags.ToArray();
        }

        public override string DisplayTags(string[] tags)
        {
            string baseMode = tags.Contains("YoutuberMode") ? "Youtuber Mode" : "Standard Mode";
            if (tags.Contains("BalloonMayham"))
            {
                baseMode += " + Balloon Mayham";
            }
            return baseMode + (tags.Contains("NoFrenzy") ? "\nNo Balloon Frenzy" : "");
        }
    }
}

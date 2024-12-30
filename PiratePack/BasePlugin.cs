using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PiratePack
{
    // Cann Plan
    // When Cann initializes, he generates 1 loop for every classroom.
    // He chooses a loop at random, and starts flying in that loop, gathering sounds.
    // Once he has gathered a certain number of unique sounds, each door he passes he will choose if he will enter.
    // Once he enters a room, he will chose an item to perch on.
    // While perching, every 5-10 seconds he will check the chance for him to leave the room.
    // The player entering and leaving, collecting the notebook if its a classroom, or the player picking up other items increases the chance for him to leave.
    // There is a slim chance for him to stay in a room and just fly to another item.
    // If he does leave, that room is removed from the loop and he repeats again. If the classroom is removed, and there are no other classrooms, remove that loop completely. (or decrease its weight?)


    [BepInPlugin("mtm101.rulerp.baldiplus.piratepack", "Pirate Pack", "0.0.0.0")]
    public class PiratePlugin : BaseUnityPlugin
    {
        public AssetManager assetMan = new AssetManager();

        public static RoomCategory sunkenFloorRoomCat;

        public static PiratePlugin Instance;

        internal static ManualLogSource Log;

        public Sprite[][] cannFlyFrames;

        public Sprite[] cannTalkFrames;

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.piratepack");
            harmony.PatchAllConditionals();
            LoadingEvents.RegisterOnLoadingScreenStart(Info, LoadEnumerator());
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(Info);
            Log = this.Logger;
            Instance = this;
        }

        void GeneratorChanges(string floorName, int levelId, SceneObject obj)
        {
            obj.potentialNPCs.Add(new WeightedNPC()
            {
                selection= assetMan.Get<NPC>("Cann"),
                weight=10000
            });
            obj.CustomLevelObject().forcedStructures = obj.CustomLevelObject().forcedStructures.AddToArray(new StructureWithParameters()
            {
                parameters = new StructureParameters(),
                prefab = assetMan.Get<StructureBuilder>("SunkenFloor")
            });
            obj.MarkAsNeverUnload();
            obj.CustomLevelObject().MarkAsNeverUnload();
        }

        IEnumerator LoadEnumerator()
        {
            yield return 3;
            yield return "Loading Cann...";

            assetMan.Add<Sprite>("CannPlaceholder", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "CannPlaceholder.png"));

            Color cannSubtitles = new Color(58f / 255f, 255f / 255f, 88f / 255f);

            assetMan.Add<SoundObject>("CannScreech1", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannSquak1.ogg"), "Vfx_Cann_Squak", SoundType.Voice, cannSubtitles));
            assetMan.Add<SoundObject>("CannScreech2", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannSquak2.ogg"), "Vfx_Cann_Squak", SoundType.Voice, cannSubtitles));
            assetMan.Add<SoundObject>("CannScreech3", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannSquak3.ogg"), "Vfx_Cann_Squak", SoundType.Voice, cannSubtitles));
            assetMan.Add<SoundObject>("CannEasterEgg", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannTheParrotSoundLikeMe.ogg"), "Vfx_Cann_EasterEgg", SoundType.Voice, cannSubtitles));

            assetMan.Add<SoundObject>("CannHungry1", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannFood1.ogg"), "Vfx_Cann_Hungry", SoundType.Voice, cannSubtitles));
            assetMan.Add<SoundObject>("CannHungry2", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannFood2.ogg"), "Vfx_Cann_Hungry", SoundType.Voice, cannSubtitles));
            assetMan.Add<SoundObject>("CannHungry3", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannFood3.ogg"), "Vfx_Cann_Hungry", SoundType.Voice, cannSubtitles));

            assetMan.Add<SoundObject>("CannEat", ObjectCreators.CreateSoundObject(AssetLoader.AudioClipFromMod(this, "CannEat.ogg"), "Vfx_Cann_Eat", SoundType.Voice, cannSubtitles));

            Cann cann = new NPCBuilder<Cann>(Info)
                .SetName("Cann")
                .SetPoster(AssetLoader.TextureFromMod(this, "cann_poster.png"), "PST_PRI_CannParrot1", "PST_PRI_CannParrot2")
                .SetAirborne()
                .SetMetaName("PST_PRI_CannParrot1")
                .AddSpawnableRoomCategories(RoomCategory.Office, RoomCategory.Special)
                .AddTrigger()
                .AddLooker()
                .SetEnum("Cann")
                .SetForcedSubtitleColor(cannSubtitles)
                .SetMinMaxAudioDistance(10f,500f)
                .Build();


            cann.spriteRenderer[0].sprite = assetMan.Get<Sprite>("CannPlaceholder");
            cann.spriteRenderer[0].transform.localPosition = Vector3.zero;

            cann.squakSounds = new SoundObject[]
            {
                assetMan.Get<SoundObject>("CannScreech1"),
                assetMan.Get<SoundObject>("CannScreech2"),
                assetMan.Get<SoundObject>("CannScreech3"),
            };
            cann.easterEggSound = assetMan.Get<SoundObject>("CannEasterEgg");
            cann.eatSound = assetMan.Get<SoundObject>("CannEat");
            cann.hungrySounds = new SoundObject[]
            {
                assetMan.Get<SoundObject>("CannHungry1"),
                assetMan.Get<SoundObject>("CannHungry2"),
                assetMan.Get<SoundObject>("CannHungry3"),
            };

            cann.audMan = cann.GetComponent<AudioManager>();


            // now turn into an array of arrays so we can read it easier in the future
            // the list is probably unnecessary actually, todo: remove list
            int cannFrames = 3;
            Vector2 cannPivot = new Vector2(0.5f, 0.2f);

            Sprite[,] loadedSprites = AssetLoader.SpritesFromSpritesheet2D(cannFrames, 8, 50f, cannPivot, AssetLoader.TextureFromMod(this, "CannFrames", "CannFly.png"));

            cannTalkFrames = AssetLoader.SpritesFromSpritesheet(3, 1, 50f, cannPivot, AssetLoader.TextureFromMod(this, "CannFrames", "CannTalk.png"));

            List<Sprite[]> spriteFrames = new List<Sprite[]>();

            for (int x = 0; x < cannFrames; x++)
            {
                Sprite[] directions = new Sprite[8];
                for (int y = 0; y < directions.Length; y++)
                {
                    directions[y] = loadedSprites[x, (y + 5) % 8];
                }
                spriteFrames.Add(directions);
            }

            cannFlyFrames = spriteFrames.ToArray();

            cann.animator = cann.gameObject.AddComponent<RotatedSpriteAnimator>();
            SpriteRotator rotator = cann.gameObject.AddComponent<SpriteRotator>();
            cann.animator.affectedObject = rotator;
            cann.rotator = rotator;
            rotator.ReflectionSetVariable("spriteRenderer", cann.spriteRenderer[0]);
            cann.volumeAnimator = cann.gameObject.AddComponent<CustomVolumeAnimator>();
            cann.volumeAnimator.sensitivity = AnimationCurve.Linear(0f,0f,1f,1f);
            cann.volumeAnimator.animations = new string[3] { "talk1", "talk2", "talk3" };
            cann.volumeAnimator.enabled = false;
            cann.volumeAnimator.volumeMultipler = 1f;

            assetMan.Add<NPC>("Cann", cann);

            yield return "Loading SunkenFloor...";

            sunkenFloorRoomCat = EnumExtensions.ExtendEnum<RoomCategory>("SunkenFloor");
            GameObject sunkenFloorObject = new GameObject("SunkenFloorBuilder");
            sunkenFloorObject.ConvertToPrefab(true);
            Structure_SunkenFloor sunkenFloorStructure = sunkenFloorObject.AddComponent<Structure_SunkenFloor>();
            sunkenFloorStructure.transparentTexture = AssetLoader.TextureFromMod(this, "FloodWaterTransparent.png");
            assetMan.Add<Structure_SunkenFloor>("SunkenFloor", sunkenFloorStructure);

            // create the SunkenFloorController prefab

            GameObject sunkenControllerObject = new GameObject("SunkenController");
            sunkenControllerObject.ConvertToPrefab(true);
            SunkenFloorController sunkenController = sunkenControllerObject.AddComponent<SunkenFloorController>();


            // set up the board
            GameObject boardObject = new GameObject("Board");
            boardObject.transform.SetParent(sunkenControllerObject.transform, false);
            boardObject.AddComponent<MeshFilter>().mesh = Resources.FindObjectsOfTypeAll<Mesh>().First(x => x.name == "Quad" && x.GetInstanceID() >= 0);
            Material boardMaterial = new Material(Resources.FindObjectsOfTypeAll<Material>().First(x => x.name == "TileBase_Alpha" && x.GetInstanceID() >= 0));
            boardMaterial.SetMainTexture(AssetLoader.TextureFromMod(this, "Platform.png"));
            boardObject.AddComponent<MeshRenderer>().material = boardMaterial;

            boardObject.transform.localScale *= 10f;
            boardObject.transform.eulerAngles = new Vector3(90f,0f,0f);

            // assign board to the controller
            sunkenController.board = boardObject;

            // and finally assign the controller to the structure
            sunkenFloorStructure.controllerPrefab = sunkenController;

            yield return "Modifying meta...";
            ItemMetaStorage.Instance.FindByEnum(Items.ZestyBar).tags.Add("cann_hate"); //chocolate is poisonous to parrots as minecraft taught me
        }
    }
}

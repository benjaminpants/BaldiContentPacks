using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.AssetTools;
using MTM101BaldAPI.ObjectCreation;
using MTM101BaldAPI.Registers;
using MTM101BaldAPI.SaveSystem;
using System;
using System.Collections;
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

        public static PiratePlugin Instance;

        internal static ManualLogSource Log;

        void Awake()
        {
            Harmony harmony = new Harmony("mtm101.rulerp.baldiplus.piratepack");
            harmony.PatchAllConditionals();
            LoadingEvents.RegisterOnLoadingScreenStart(Info, LoadEnumerator());
            GeneratorManagement.Register(this, GenerationModType.Addend, GeneratorChanges);
            ModdedSaveGame.AddSaveHandler(Info);
            Log = this.Logger;
        }

        void GeneratorChanges(string floorName, int levelId, SceneObject obj)
        {
            obj.potentialNPCs.Add(new WeightedNPC()
            {
                selection= assetMan.Get<NPC>("Cann"),
                weight=1000
            });
            obj.MarkAsNeverUnload();
            obj.CustomLevelObject().MarkAsNeverUnload();
        }

        IEnumerator LoadEnumerator()
        {
            yield return 1;
            yield return "Loading...";

            assetMan.Add<Sprite>("CannPlaceholder", AssetLoader.SpriteFromMod(this, Vector2.one / 2f, 50f, "CannPlaceholder.png"));

            Cann cann = new NPCBuilder<Cann>(Info)
                .SetName("Cann")
                .SetPoster(AssetLoader.TextureFromMod(this, "cann_poster.png"), "PST_PRI_CannParrot1", "PST_PRI_CannParrot2")
                .SetAirborne()
                .SetMetaName("PST_PRI_CannParrot1")
                .AddSpawnableRoomCategories(RoomCategory.Office, RoomCategory.Special)
                .AddTrigger()
                .AddLooker()
                .SetEnum("Cann")
                .Build();

            cann.spriteRenderer[0].sprite = assetMan.Get<Sprite>("CannPlaceholder");
            cann.spriteRenderer[0].transform.localPosition = Vector3.zero;
            cann.audMan = cann.GetComponent<AudioManager>();

            assetMan.Add<NPC>("Cann", cann);
        }
    }
}

using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Components;
using MTM101BaldAPI.Registers;
using System.Collections.Generic;
using System.EnterpriseServices;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace PiratePack
{

    public class CannSoundEntry
    {
        public SoundObject soundObject;
        public SoundType soundType;
        public Color overrideColor = Color.white;
        public bool subtitleOverride = false;
        public float soundPitch = 1f;
        public float minDistance = 10f;
        public float maxDistance = 10f;

        private static readonly FieldInfo _subtitleColor = AccessTools.Field(typeof(AudioManager), "subtitleColor");
        private static readonly FieldInfo _overrideSubtitleColor = AccessTools.Field(typeof(AudioManager), "overrideSubtitleColor");
        private static readonly FieldInfo _minDistance = AccessTools.Field(typeof(PropagatedAudioManager), "minDistance");
        private static readonly FieldInfo _maxDistance = AccessTools.Field(typeof(PropagatedAudioManager), "maxDistance");

        public CannSoundEntry(SoundObject obj, AudioManager audMan)
        {
            soundObject = obj;
            soundPitch = audMan.audioDevice.pitch;
            if (audMan.GetComponent<NPC>()) { soundType = SoundType.NPC; }
            else if (audMan.GetComponent<Item>()) { soundType = SoundType.Item; }
            else if ((audMan.transform.parent != null) && audMan.transform.parent.GetComponent<Door>()) { soundType = SoundType.Door; }
            else { soundType = SoundType.Other; }
            if (((bool)_overrideSubtitleColor.GetValue(audMan)))
            {
                subtitleOverride = true;
                overrideColor = (Color)_subtitleColor.GetValue(audMan);
            }
            if (audMan is PropagatedAudioManager)
            {
                minDistance = (float)_minDistance.GetValue(audMan);
                maxDistance = (float)_maxDistance.GetValue(audMan);
            }
            else
            {
                minDistance = audMan.audioDevice.minDistance;
                maxDistance = audMan.audioDevice.maxDistance;
            }
        }

        public enum SoundType
        {
            Other,
            Door,
            Item,
            NPC
        }
    }

    public class Cann : NPC
    {

        public delegate void SoundPlayFunction(SoundObject obj, AudioManager man);

        public static event Cann.SoundPlayFunction OnSoundObjectPlayed;

        public static void InvokeSoundPlayed(SoundObject obj, AudioManager man)
        {
            if (OnSoundObjectPlayed == null) return;
            OnSoundObjectPlayed(obj, man);
        }

        public float standardSpeed = 24f;
        public float fleeSpeed = 38f;
        public float addonPerSound = 0.4f;
        public int maxSounds = 32;
        public float hearingMultiplier = 1.25f;
        public int loopsAround = 1;
        public int chanceToStickAround = 25;

        public float minTimeToPlayRandom = 7f;
        public float maxTimeToPlayRandom = 30f;

        public Entity entity;
        public List<CannSoundEntry> heardSounds = new List<CannSoundEntry>();
        public List<WeightedCannLoop> loops = new List<WeightedCannLoop>();
        public AudioManager audMan;
        bool hasMethodAdded = false;

        public SoundObject[] squakSounds;
        public SoundObject[] hungrySounds;
        public SoundObject easterEggSound;
        public SoundObject eatSound;
        public RotatedSpriteAnimator animator;
        public SpriteRotator rotator;
        public CustomVolumeAnimator volumeAnimator;

        protected static readonly FieldInfo _subtitleColor = AccessTools.Field(typeof(AudioManager), "subtitleColor");
        protected static readonly FieldInfo _minDistance = AccessTools.Field(typeof(PropagatedAudioManager), "minDistance");
        protected static readonly FieldInfo _maxDistance = AccessTools.Field(typeof(PropagatedAudioManager), "maxDistance");
        public float defaultMinDistance = 10f;
        public float defaultMaxDistance = 500f;
        public Color defaultSubtitleColor;

        public Dictionary<RoomCategory, int> roomValueAddons = new Dictionary<RoomCategory, int>()
        {
            { RoomCategory.Class, 75 },
            { RoomCategory.Office, -100 }
        };

        public Dictionary<CannSoundEntry.SoundType, int> maxDupEntriesPerType = new Dictionary<CannSoundEntry.SoundType, int>()
        {
            { CannSoundEntry.SoundType.NPC, 4 },
            { CannSoundEntry.SoundType.Other, 3 },
            { CannSoundEntry.SoundType.Door, 1 },
            { CannSoundEntry.SoundType.Item, 2 },
        };

        public override void Initialize()
        {
            // why the fuck is SpriteRotator null
            animator.affectedObject = GetComponent<SpriteRotator>();
            volumeAnimator.animator = animator;
            animator.animations = new Dictionary<string, CustomAnimation<Sprite[]>>()
            {
                { "fly", new CustomAnimation<Sprite[]>(10, PiratePlugin.Instance.cannFlyFrames) },
                { "talk1", new CustomAnimation<Sprite[]>(1, new Sprite[1][] { new Sprite[1] { PiratePlugin.Instance.cannTalkFrames[0] } })},
                { "talk2", new CustomAnimation<Sprite[]>(1, new Sprite[1][] { new Sprite[1] { PiratePlugin.Instance.cannTalkFrames[1] } })},
                { "talk3", new CustomAnimation<Sprite[]>(1, new Sprite[1][] { new Sprite[1] { PiratePlugin.Instance.cannTalkFrames[2] } })},
            };
            animator.SetDefaultAnimation("fly", 1f);
            base.Initialize();
            FindLoops();
            if (loops.Count == 0)
            {
                PiratePlugin.Log.LogError("Cann unable to find any loops! Report the current seed to MTM101!");
                Despawn();
            }
            behaviorStateMachine.ChangeState(new Cann_FlyLoop(this, loopsAround));
            entity = GetComponent<Entity>();
            //ec.map.AddArrow(entity, Color.yellow);
            OnSoundObjectPlayed += OnSoundPlayed;
            hasMethodAdded = true;
            SetFleeing(false);
            defaultSubtitleColor = (Color)_subtitleColor.GetValue(audMan);
            defaultMinDistance = (float)_minDistance.GetValue(audMan);
            defaultMaxDistance = (float)_maxDistance.GetValue(audMan);
            volumeAnimator.audioSource = audMan.audioDevice;
            SetVolumeAnimatorState(false);
        }

        public bool fleeing = false;

        public void SetFleeing(bool flee)
        {
            fleeing = flee;
            UpdateSpeed();
        }

        public void UpdateSpeed()
        {
            float speedAddon = addonPerSound * heardSounds.Count;
            if (fleeing)
            {
                Navigator.maxSpeed = fleeSpeed + speedAddon;
                Navigator.SetSpeed(fleeSpeed + speedAddon);
            }
            else
            {
                Navigator.maxSpeed = standardSpeed + speedAddon;
                Navigator.SetSpeed(standardSpeed + speedAddon);
            }
        }

        public void PlayCuteSound()
        {
            audMan.FlushQueue(true);
            ResetSoundSettings();
            audMan.QueueRandomAudio(hungrySounds);
        }

        public void PlayEatSound()
        {
            audMan.FlushQueue(true);
            ResetSoundSettings();
            audMan.QueueAudio(eatSound);
        }

        public void ResetSoundSettings()
        {
            audMan.pitchModifier = 1f;
            _subtitleColor.SetValue(audMan, defaultSubtitleColor);
            _minDistance.SetValue(audMan, defaultMinDistance);
            _maxDistance.SetValue(audMan, defaultMaxDistance);

        }

        public void SquakAndAlert(PlayerManager pm)
        {
            audMan.FlushQueue(true);
            ResetSoundSettings();
            audMan.QueueRandomAudio(squakSounds);
            ec.MakeNoise(transform.position, 2);
            pm.RuleBreak("Bullying", 2f);
        }

        public void AddSound(CannSoundEntry sound)
        {
            heardSounds.Add(sound);
            // remove duplicate entries
            CannSoundEntry[] duplicateEntries = heardSounds.Where(x => x.soundObject == sound.soundObject).ToArray();
            if (duplicateEntries.Length > maxDupEntriesPerType[sound.soundType])
            {
                heardSounds.Remove(duplicateEntries[Random.Range(0, duplicateEntries.Length)]);
            }
            if (heardSounds.Count > maxSounds)
            {
                // prioritize removing door sounds over any other sound type
                CannSoundEntry[] uselessDoorSounds = heardSounds.Where(x => x.soundType == CannSoundEntry.SoundType.Door).ToArray();
                if (uselessDoorSounds.Length > 0)
                {
                    heardSounds.Remove(uselessDoorSounds[Random.Range(0, uselessDoorSounds.Length)]);
                    UpdateSpeed();
                    return;
                }
                heardSounds.RemoveAt(Random.Range(0, Mathf.Min(3, maxSounds - 1)));
            }
            UpdateSpeed();
        }


        void OnSoundPlayed(SoundObject obj, AudioManager man)
        {
            if (ec.CellFromPosition(transform.position).Silent) return; // can't hear if everything is silent...
            if (man == audMan) return; // No infinite loops in the halls...
            // sound is too far away for cam to have heard
            // todo: account for sound propagation?
            if (Vector3.Distance(transform.position, man.transform.position) > (((man is PropagatedAudioManager) ? (float)_maxDistance.GetValue(audMan) : man.audioDevice.maxDistance) * hearingMultiplier)) return;
            if (behaviorStateMachine.currentState is Cann_StateBase)
            {
                ((Cann_StateBase)behaviorStateMachine.currentState).HeardSound(obj, man);
            }
        }

        public override void Despawn()
        {
            base.Despawn();
            OnSoundObjectPlayed -= OnSoundPlayed;
            hasMethodAdded = false;
        }

        public bool PlayRandomSound(bool isObvious = false)
        {
            if (heardSounds.Count == 0) return false;
            CannSoundEntry entry = heardSounds[Random.Range(0, heardSounds.Count)];
            heardSounds.Remove(entry);
            PlaySoundEntry(entry, isObvious);
            UpdateSpeed();
            return true;
        }

        public bool ItemIsCannFood(ItemObject obj)
        {
            ItemMetaData meta = obj.GetMeta();
            if (meta == null) return false;
            return (((meta.tags.Contains("food") && !meta.tags.Contains("drink")) || (meta.tags.Contains("cann_like"))) && (!meta.tags.Contains("cann_hate")));
        }

        public void PlaySoundEntry(CannSoundEntry entry, bool isObvious = false)
        {
            if (!isObvious)
            {
                if (entry.subtitleOverride)
                {
                    _subtitleColor.SetValue(audMan, entry.overrideColor);
                }
                else
                {
                    _subtitleColor.SetValue(audMan, entry.soundObject.color);
                }
                _minDistance.SetValue(audMan, entry.minDistance);
                _maxDistance.SetValue(audMan, entry.maxDistance);
            }
            else
            {
                ResetSoundSettings();
            }
            audMan.pitchModifier = (isObvious ? 1.5f : 1f) * entry.soundPitch;
            if (Random.Range(1, 999) == 99)
            {
                audMan.QueueAudio(easterEggSound);
            }
            else
            {
                audMan.QueueAudio(entry.soundObject);
            }
        }

        void OnDestroy()
        {
            if (hasMethodAdded)
            {
                OnSoundObjectPlayed -= OnSoundPlayed;
            }
        }

        public Pickup[] GetItemsInRoom(RoomController rm)
        {
            List<Pickup> items = new List<Pickup>();
            foreach (Pickup itm in ec.items)
            {
                if (!itm.isActiveAndEnabled) continue;
                if (itm.transform.parent == rm.objectObject.transform)
                {
                    items.Add(itm);
                }
            }
            return items.ToArray();
        }

        void FindLoop(Cell tile, Direction directionA, Direction directionB, out List<Cell> finalList)
        {
            int navBin = tile.NavBin;
            Cell startTile = ec.CellFromPosition(tile.position + directionA.ToIntVector2());
            Cell targetTile = ec.CellFromPosition(tile.position + directionB.ToIntVector2());
            tile.NavBin = 15;
            ec.FindPath(startTile, targetTile, PathType.Nav, out finalList, out _);
            if (finalList == null)
            {
                finalList = new List<Cell>();
            }
            {
                finalList = finalList.ToList();
            }
            tile.NavBin = navBin;
        }

        static FieldInfo _activity = AccessTools.Field(typeof(RoomController), "activity");


        public int CalculateRoomWeight(RoomController rc)
        {
            // prioritize rooms with lots of doors (more likely for the player to stumble in)
            // and rooms with specified bonuses, mostly just to boost classrooms and de-boost offices if somehow an item is found inside one.
            // (offices make for bad camping spots)
            // and penalize classrooms with completed activities as it is less likely the player will walk into them again.
            int baseWeight = (rc.doors.Count * 100) + (roomValueAddons.ContainsKey(rc.category) ? roomValueAddons[rc.category] : 0);
            bool hasCompletedActivity = ((_activity.GetValue(rc) != null) && (!rc.HasIncompleteActivity));
            return Mathf.Max(baseWeight - (hasCompletedActivity ? 100 : 0), 50);
        }

        public void SetVolumeAnimatorState(bool useVolumeAnimator)
        {
            volumeAnimator.enabled = useVolumeAnimator;
            if (!useVolumeAnimator)
            {
                animator.SetDefaultAnimation("fly", 1f);
                animator.Play("fly", 1f);
            }
            else
            {
                animator.SetDefaultAnimation("talk1", 1f);
                animator.Play("talk1", 1f);
            }
        }

        public void FindLoops(bool useClassrooms = true)
        {
            loops.Clear();

            List<RoomController> roomsWithItems = ec.rooms.Where(x => (GetItemsInRoom(x).Length > 0)).ToList();


            List<RoomController> classRooms = ec.rooms.Where(x => _activity.GetValue(x) != null).ToList();
            if (!useClassrooms)
            {
                classRooms = roomsWithItems;
            }
            if (classRooms.Count == 0)
            {
                PiratePlugin.Log.LogWarning("Cann couldn't find any rooms with incomplete activities and items?!");
                // a dumb, dumb backup way of determining "classrooms"
                classRooms = roomsWithItems.Where(x => x.type == RoomType.Room && x.category != RoomCategory.Special && x.category != RoomCategory.Null).ToList();
                while (classRooms.Count > 7)
                {
                    classRooms.RemoveAt(UnityEngine.Random.Range(0,classRooms.Count));
                }
                if (classRooms.Count == 0)
                {
                    PiratePlugin.Log.LogError("Cann couldn't find any valid rooms to form loops! Report this seed!");
                    this.Despawn();
                    return;
                }
            }
            foreach (RoomController rc in classRooms)
            {
                CannLoop newLoop = new CannLoop();

                int loopWeight = 100;

                Door[] doors = rc.doors.Where(x => (x.bTile.room.type == RoomType.Hall || x.aTile.room.type == RoomType.Hall)).ToArray();

                if (doors.Length == 0)
                {
                    PiratePlugin.Log.LogWarning("Found no hallway linked doors for: " + rc.name + "!");
                    continue;
                }

                Door foundDoor = doors[UnityEngine.Random.Range(0, doors.Length)];
                if (foundDoor == null) continue;
                Cell foundCell = foundDoor.bTile;
                List<Direction> freeDirections = foundCell.AllOpenNavDirections.Where(dir => ec.CellFromPosition(foundCell.position + dir.ToIntVector2()).room.type == RoomType.Hall).ToList();
                int chosenIndex = UnityEngine.Random.Range(0, freeDirections.Count);
                Direction chosenInitialDirection = freeDirections[chosenIndex];
                freeDirections.RemoveAt(chosenIndex);
                bool foundCellLoop = false;
                // try to form a loop with all available directions
                while ((!foundCellLoop) && freeDirections.Count > 0)
                {
                    int chosenNewIndex = UnityEngine.Random.Range(0, freeDirections.Count);
                    FindLoop(foundCell, chosenInitialDirection, freeDirections[chosenNewIndex], out List<Cell> finalList);
                    freeDirections.RemoveAt(chosenNewIndex);
                    if (finalList.Count > 0)
                    {
                        foundCellLoop = true;
                        newLoop.cellsInLoop = finalList;
                    }
                }
                // if we ran out of free directions and STILL didn't find a loop, cease our attempts.
                if (!foundCellLoop)
                {
                    PiratePlugin.Log.LogWarning("Couldn't find loop for: " + rc.name + "!");
                    continue;
                }
                
                // now, it is time to assign rooms in our loop

                foreach (RoomController itemRoom in roomsWithItems)
                {
                    bool inLoop = false;
                    foreach (Door door in itemRoom.doors)
                    {
                        if (newLoop.cellsInLoop.Contains(door.bTile))
                        {
                            inLoop = true;
                            break;
                        }
                    }
                    if (!inLoop) continue;

                    newLoop.rooms.Add(new WeightedRoomController()
                    {
                        selection = itemRoom,
                        weight = CalculateRoomWeight(itemRoom)
                    });
                }

                if (roomsWithItems.Contains(rc))
                {
                    newLoop.rooms.Add(new WeightedRoomController()
                    {
                        selection = rc,
                        weight = CalculateRoomWeight(rc)
                    });
                }

                loopWeight += newLoop.cellsInLoop.Count * 5;
                loopWeight += newLoop.rooms.Count * 25;

                loops.Add(new WeightedCannLoop()
                {
                    selection = newLoop,
                    weight = loopWeight
                });

            }
        }
    }

    public class CannLoop
    {
        public List<Cell> cellsInLoop = new List<Cell>();

        public List<WeightedRoomController> rooms = new List<WeightedRoomController>();

        public RoomController GetRandomRoom(Cann cann)
        {
            rooms.RemoveAll(x => cann.GetItemsInRoom(x.selection).Length == 0);
            if (rooms.Count == 0)
            {
                int removed = cann.loops.RemoveAll(x => x.selection == this);
                if (removed == 0)
                {
                    PiratePlugin.Log.LogWarning("GetRandomRoom called with Cann who doesn't have us in their list?");
                }
                if (cann.loops.Count == 0)
                {
                    PiratePlugin.Log.LogDebug("Cann removed all loops from list... Generating new loops?");
                    cann.FindLoops(true);
                }
                return null;
            }
            rooms.ForEach(x =>
            {
                x.weight = cann.CalculateRoomWeight(x.selection);
            });
            return WeightedRoomController.RandomSelection(rooms.ToArray());
        }
    }

    public class WeightedCannLoop : WeightedSelection<CannLoop>
    {

    }


    public class Cann_StateBase : NpcState
    {
        protected Cann cann;
        public Cann_StateBase(NPC npc) : base(npc)
        {
            cann = (Cann)npc;
        }

        // will be called when Cann hears a sound during this state
        public virtual void HeardSound(SoundObject sound, AudioManager audMan)
        {

        }
    }


    /// <summary>
    /// Cann chooses a random loop and flies around it for the specified number of times, before deciding to enter one of the rooms.
    /// </summary>
    public class Cann_FlyLoop : Cann_StateBase
    {
        CannLoop currentLoop = null;
        int loops = 0;
        float timeTilRandomAmbience;

        public Cann_FlyLoop(NPC npc, int loops) : base(npc)
        {
            currentLoop = WeightedCannLoop.RandomSelection(cann.loops.ToArray());
            timeTilRandomAmbience = Random.Range(cann.minTimeToPlayRandom, cann.maxTimeToPlayRandom);
            this.loops = loops;
        }

        public Cann_FlyLoop(NPC npc, CannLoop loop, int loops) : base(npc)
        {
            currentLoop = loop;
            this.loops = loops;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_PredeterminedPath(cann, 0, currentLoop.cellsInLoop));
            cann.SetFleeing(false);
        }

        public override void Update()
        {
            base.Update();
            if (timeTilRandomAmbience > 0 && (cann.heardSounds.Count > 0))
            {
                timeTilRandomAmbience -= Time.deltaTime * cann.TimeScale;
                if (timeTilRandomAmbience <= 0)
                {
                    timeTilRandomAmbience = 0f;
                    cann.PlayRandomSound(false);
                }
            }
            if (loops <= 0) return;
            if (cann.heardSounds.Count >= cann.maxSounds)
            {
                loops = 0;
                DestinationEmpty();
            }
        }

        public override void DestinationEmpty()
        {
            loops--;
            //PiratePlugin.Log.LogDebug(loops + " loops left!");
            if (loops <= 0)
            {
                //PiratePlugin.Log.LogDebug("Cann finished loop cycle! Attempting room...");
                cann.behaviorStateMachine.ChangeState(new Cann_AttemptPerch(cann, currentLoop));
                return;
            }
            else
            {
                ChangeNavigationState(new NavigationState_PredeterminedPath(cann, 0, currentLoop.cellsInLoop));
            }
        }

        public override void HeardSound(SoundObject sound, AudioManager audMan)
        {
            base.HeardSound(sound, audMan);
            cann.AddSound(new CannSoundEntry(sound, audMan));
        }
    }

    public class Cann_Flee : Cann_StateBase
    {
        protected PlayerManager player;
        protected float fleeTime = 0f;
        protected int fleePriority = 0;
        public Cann_Flee(NPC npc, PlayerManager player, float fleeTime) : base(npc)
        {
            this.player = player;
            this.fleeTime = fleeTime;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_WanderFlee(cann, fleePriority, player.DijkstraMap));
            cann.SetFleeing(true);
        }

        public override void Update()
        {
            fleeTime -= Time.deltaTime * cann.TimeScale;
            if (fleeTime <= 0f)
            {
                int closestDistance = int.MaxValue;
                CannLoop closestLoop = null;
                foreach (CannLoop loop in cann.loops.Select(x => x.selection))
                {
                    cann.ec.FindPath(cann.ec.CellFromPosition(cann.transform.position), loop.cellsInLoop[0], PathType.Nav, out List<Cell> result, out bool success);
                    if (!success) continue;
                    if (result.Count < closestDistance)
                    {
                        closestDistance = result.Count;
                        closestLoop = loop;
                    }
                }
                if (closestLoop == null)
                {
                    PiratePlugin.Log.LogWarning("Cann couldn't find shortest loop??? wtf?!?");
                    cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(npc, cann.loopsAround));
                    return;
                }
                cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(npc, closestLoop, cann.loopsAround));
                return;
            }
        }

        public override void HeardSound(SoundObject sound, AudioManager audMan)
        {
            base.HeardSound(sound, audMan);
            cann.AddSound(new CannSoundEntry(sound, audMan));
        }
    }

    public class Cann_Distract : Cann_Flee
    {
        protected int navValue = 3;

        float timeUntilNextPlay = 0f;

        public Cann_Distract(NPC npc, PlayerManager player, float initialWait) : base(npc, player, float.PositiveInfinity)
        {
            timeUntilNextPlay = initialWait;
            fleePriority = 32; // so we bypass the party's calling
        }

        public override void Update()
        {
            base.Update();
            if (!cann.audMan.AnyAudioIsPlaying)
            {
                timeUntilNextPlay -= Time.deltaTime * cann.TimeScale;
            }
            if (timeUntilNextPlay <= 0f)
            {
                if (!cann.PlayRandomSound(true))
                {
                    timeUntilNextPlay = float.PositiveInfinity;
                    fleeTime = 5f;
                }
                else
                {
                    cann.ec.MakeNoise(cann.transform.position, 9);
                    timeUntilNextPlay = Random.Range(0.5f, 2f);
                    for (int i = 0; i < cann.ec.Npcs.Count; i++)
                    {
                        NPC npc = cann.ec.Npcs[i];
                        if (npc.GetMeta().tags.Contains("cann_ignore_distraction")) continue; // these characters already have ways of hearing and should ignore cann's distraction
                        npc.navigationStateMachine.ChangeState(new NavigationState_TargetPositionRevert(npc, navValue, cann.transform.position, false));
                    }
                }
            }
        }

        // dont want cann picking up on new sounds during the distraction phase, as this makes it last forever
        public override void HeardSound(SoundObject sound, AudioManager audMan)
        {
            
        }
    }

    // todo: handle if the player gets a new item while in the room cann is in
    public class Cann_Perch : Cann_StateBase
    {
        Pickup chosenPickup = null;
        RoomController pickupRoom = null;

        float timeUntilBoredomCheck = 0f;
        int chanceToLeave = 0;

        static FieldInfo _stillHasItem = AccessTools.Field(typeof(Pickup), "stillHasItem");

        // the player that cann is trying to appease
        // will open his mouth when the player approaches and will play cute sounds when the player is in the room
        PlayerManager appeasingPlayer = null;
        bool hasPlayedCuteSound = false;

        public Cann_Perch(NPC npc, Pickup toPerchUpon, RoomController room) : base(npc)
        {
            chosenPickup = toPerchUpon;
            pickupRoom = room;
        }
        public override void Enter()
        {
            base.Enter();
            cann.entity.SetFrozen(true);
            cann.entity.Teleport(chosenPickup.transform.position);
            cann.entity.OnTeleport += Teleported;
            chosenPickup.OnItemCollected += ItemCollected;
            timeUntilBoredomCheck = Random.Range(5f,10f);
            ChangeNavigationState(new NavigationState_DoNothing(cann, 0));
            cann.SetVolumeAnimatorState(true);
        }


        public override void PlayerInSight(PlayerManager pm)
        {
            if (appeasingPlayer != null) return;
            foreach (ItemObject itm in pm.itm.items)
            {
                if (cann.ItemIsCannFood(itm))
                {
                    appeasingPlayer = pm;
                    break;
                }
            }
            if (appeasingPlayer == null) return;
            if (hasPlayedCuteSound) return;
            cann.PlayCuteSound();
            hasPlayedCuteSound = true;
        }

        public override void PlayerLost(PlayerManager pm)
        {
            if (pm != appeasingPlayer) return;
            appeasingPlayer = null;
        }

        public override void Update()
        {
            base.Update();
            if (chosenPickup == null) // incase some mod does something weird?
            {
                // todo: swap for flee state
                cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, cann.loopsAround));
                return;
            }
            cann.spriteRenderer[0].gameObject.transform.position = chosenPickup.transform.GetComponentInChildren<SpriteRenderer>().transform.position + (Vector3.up * (1.28f / 2f));
            if (!cann.audMan.AnyAudioIsPlaying)
            {
                timeUntilBoredomCheck -= Time.deltaTime * cann.TimeScale;
            }
            if (timeUntilBoredomCheck <= 0f)
            {
                timeUntilBoredomCheck = Random.Range(2f, 8f);
                if (!cann.PlayRandomSound(appeasingPlayer != null))
                {
                    chanceToLeave = 100;
                }
                chanceToLeave += 8;
                if (UnityEngine.Random.Range(1, 100) <= chanceToLeave)
                {
                    if (!appeasingPlayer)
                    {
                        cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, cann.loopsAround));
                    }
                    else
                    {
                        cann.behaviorStateMachine.ChangeState(new Cann_AttemptPerch(npc, npc.ec.CellFromPosition(npc.transform.position).room, chosenPickup));
                    }
                }
            }
        }

        public override void Exit()
        {
            base.Exit();
            cann.entity.SetFrozen(false);
            if (chosenPickup)
            {
                chosenPickup.OnItemCollected -= ItemCollected;
            }
            cann.spriteRenderer[0].gameObject.transform.localPosition = Vector3.zero;
            cann.entity.OnTeleport -= Teleported;
            cann.SetVolumeAnimatorState(false);
        }


        public override void HeardSound(SoundObject sound, AudioManager audMan)
        {
            base.HeardSound(sound, audMan);
            // cann is too busy chirping to hear any sounds
            if (!cann.audMan.AnyAudioIsPlaying)
            {
                cann.AddSound(new CannSoundEntry(sound, audMan));
            }
        }

        void Teleported(Vector3 position)
        {
            cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, cann.loopsAround));
        }

        void ItemCollected(Pickup pickup, int player)
        {
            PlayerManager pm = npc.ec.Players[player];
            bool cannAppeased = false;
            // players inventory isn't full and they tried to give cann food
            if (cann.ItemIsCannFood(pm.itm.items[pm.itm.selectedItem]))
            {
                pm.itm.RemoveItem(pm.itm.selectedItem);
                cannAppeased = true;
            }
            else
            {
                // maybe the players inventory was full and the item got swapped to underneath us, check.
                if (pickup.isActiveAndEnabled && cann.ItemIsCannFood(pickup.item))
                {
                    _stillHasItem.SetValue(pickup, false);
                    pickup.gameObject.SetActive(false);
                    pickup.icon.spriteRenderer.enabled = false;
                    cannAppeased = true;
                }
            }

            if (cannAppeased)
            {
                cann.PlayEatSound();
                cann.behaviorStateMachine.ChangeState(new Cann_WaitForSound(cann, new Cann_Distract(cann, pm, 3f), true));
                return;
            }
            cann.SquakAndAlert(pm);
            if (Random.Range(1, 101) <= cann.chanceToStickAround)
            {
                cann.SetFleeing(true);
                cann.behaviorStateMachine.ChangeState(new Cann_AttemptPerch(npc, npc.ec.CellFromPosition(npc.transform.position).room, pickup));
            }
            else
            {
                cann.behaviorStateMachine.ChangeState(new Cann_Flee(cann, cann.ec.Players[player], 6f));
            }
        }

    }

    public class Cann_WaitForSound : Cann_StateBase
    {
        Cann_StateBase stateAfter;
        bool shouldBeTalking = false;
        public Cann_WaitForSound(NPC npc, Cann_StateBase stateAfter, bool shouldBeTalking) : base(npc)
        {
            this.stateAfter = stateAfter;
            this.shouldBeTalking = shouldBeTalking;
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(new NavigationState_DoNothing(npc, 32));
            if (shouldBeTalking)
            {
                cann.SetVolumeAnimatorState(true);
                cann.entity.SetFrozen(true);
            }
        }

        public override void Update()
        {
            base.Update();
            if (cann.audMan.AnyAudioIsPlaying) return;
            currentNavigationState.priority = 0;
            cann.behaviorStateMachine.ChangeState(stateAfter);
        }

        public override void Exit()
        {
            base.Exit();
            if (shouldBeTalking)
            {
                cann.SetVolumeAnimatorState(false);
                cann.entity.SetFrozen(false);
            }
        }
    }

    public class Cann_AttemptPerch : Cann_StateBase
    {

        int perchFails = 0;

        static FieldInfo _value = AccessTools.Field(typeof(ITM_YTPs), "value");

        Pickup selectedPickup = null;
        RoomController chosenRoom;

        public Cann_AttemptPerch(NPC npc, CannLoop loopToGrabRoomFrom) : base(npc)
        {
            chosenRoom = loopToGrabRoomFrom.GetRandomRoom(cann);
            if (chosenRoom == null)
            {
                return;
            }
            SelectPickupFromRoom(chosenRoom);
        }

        public Cann_AttemptPerch(NPC npc, RoomController chosenRoom, Pickup toIgnore) : base(npc)
        {
            this.chosenRoom = chosenRoom;
            SelectPickupFromRoom(chosenRoom, toIgnore);
        }

        public override void Enter()
        {
            base.Enter();
            if (selectedPickup == null)
            {
                npc.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 1));
                return;
            }
            ChangeNavigationState(new NavigationState_TargetPosition(npc, 32, selectedPickup.transform.position));
        }

        public override void Update()
        {
            base.Update();
            if ((selectedPickup == null) || ((!selectedPickup.isActiveAndEnabled)))
            {
                npc.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 1));
            }
        }

        public override void DestinationEmpty()
        {
            if (Vector3.Distance(new Vector3(npc.transform.position.x, 0f, npc.transform.position.z), new Vector3(selectedPickup.transform.position.x, 0f, selectedPickup.transform.position.z)) < 10f)
            {
                npc.behaviorStateMachine.ChangeState(new Cann_Perch(cann, selectedPickup, chosenRoom));
                cann.SetFleeing(false);
            }
            else
            {
                if (perchFails >= 10)
                {
                    npc.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 1));
                }
                SelectPickupFromRoom(chosenRoom, selectedPickup);
                ChangeNavigationState(new NavigationState_TargetPosition(npc, 32, selectedPickup.transform.position));
                perchFails++;
            }
        }

        void SelectPickupFromRoom(RoomController rc, Pickup toIgnore = null)
        {
            List<WeightedSelection<Pickup>> weightedPickups = new List<WeightedSelection<Pickup>>();
            Pickup[] pickups = cann.GetItemsInRoom(rc).Where(x => x != toIgnore).ToArray();
            if (pickups.Length == 0) return;
            foreach (Pickup p in pickups)
            {
                if (p.item == null) continue;
                if (p.item.item is ITM_YTPs)
                {
                    int ytps = (int)_value.GetValue(p.item.item);
                    weightedPickups.Add(new WeightedSelection<Pickup>()
                    {
                        selection = p,
                        weight = ytps*4
                    });
                    continue;
                }
                ItemMetaData itemMeta = p.item.GetMeta();
                if (itemMeta == null) continue;
                if (itemMeta.flags.HasFlag(ItemFlags.InstantUse))
                {
                    weightedPickups.Add(new WeightedSelection<Pickup>()
                    {
                        selection=p,
                        weight=255
                    });
                    continue;
                }
                weightedPickups.Add(new WeightedSelection<Pickup>()
                {
                    selection = p,
                    weight = p.item.price
                });
            }
            // just incase something goes wrong here, default to choosing a random pickup.
            if (weightedPickups.Count == 0)
            {
                selectedPickup = pickups[UnityEngine.Random.Range(0, pickups.Length)];
                return;
            }
            selectedPickup = WeightedSelection<Pickup>.RandomSelection(weightedPickups.ToArray());
            return;
        }
    }

    // navigate on and attempt to stay on the predetermined path
    public class NavigationState_PredeterminedPath : NavigationState
    {
        protected List<Cell> cellsToMoveTo = new List<Cell>();

        public NavigationState_PredeterminedPath(NPC owner, int priority, List<Cell> flyTo) : base(owner, priority)
        {
            cellsToMoveTo = flyTo.ToList(); // make a copy for our use
            DestinationEmpty();
        }

        // todo: check if TrapCheck is expensive?
        Cell FindNextValidTargetCell()
        {
            //(npc.ec.FindPath(npc.ec.CellFromPosition(npc.transform.position), cellsToMoveTo[0], PathType.Nav, out _, )
            while ((cellsToMoveTo.Count > 0) && npc.ec.TrapCheck(cellsToMoveTo[0]))
            {
                cellsToMoveTo.Remove(cellsToMoveTo[0]);
            }
            if (cellsToMoveTo.Count == 0) return null;
            Cell chosenCell = cellsToMoveTo[0];
            cellsToMoveTo.Remove(cellsToMoveTo[0]);
            return chosenCell;
        }

        bool FindNextCellAndPath()
        {
            Cell nextTarget = FindNextValidTargetCell();
            if (nextTarget == null) return false;
            npc.Navigator.FindPath(npc.transform.position, nextTarget.CenterWorldPosition);
            return true;
        }

        public override void DestinationEmpty()
        {
            if (!FindNextCellAndPath())
            {
                base.DestinationEmpty();
            }
        }
    }
}

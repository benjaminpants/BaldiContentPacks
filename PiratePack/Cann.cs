using HarmonyLib;
using MTM101BaldAPI;
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

        public CannSoundEntry(SoundObject obj, AudioManager audMan)
        {
            soundObject = obj;
            if (audMan.GetComponent<NPC>()) { soundType = SoundType.NPC; }
            else if (audMan.GetComponent<Item>()) { soundType = SoundType.Item; }
            else if ((audMan.transform.parent != null) && audMan.transform.parent.GetComponent<Door>()) { soundType = SoundType.Door; }
            else { soundType = SoundType.Other; }
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

        public float standardSpeed = 17f;
        public int maxSounds = 32;
        public float hearingMultiplier = 1.25f;

        public Entity entity;
        public List<CannSoundEntry> heardSounds = new List<CannSoundEntry>();
        public List<WeightedCannLoop> loops = new List<WeightedCannLoop>();
        public AudioManager audMan;
        bool hasMethodAdded = false;

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
            base.Initialize();
            FindLoops();
            behaviorStateMachine.ChangeState(new Cann_FlyLoop(this, 3));
            entity = GetComponent<Entity>();
            ec.map.AddArrow(entity, UnityEngine.Color.yellow);
            Navigator.maxSpeed = standardSpeed;
            Navigator.SetSpeed(standardSpeed);
            OnSoundObjectPlayed += OnSoundPlayed;
            hasMethodAdded = true;
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
                    return;
                }
                heardSounds.RemoveAt(Random.Range(0, Mathf.Min(3, maxSounds - 1)));
            }
        }


        void OnSoundPlayed(SoundObject obj, AudioManager man)
        {
            if (man == audMan) return; // No infinite loops in the halls...
            // sound is too far away for cam to have heard
            // todo: account for sound propagation?
            if (Vector3.Distance(transform.position, man.transform.position) > (man.audioDevice.maxDistance * hearingMultiplier)) return;
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

        public bool PlayRandomSound()
        {
            if (heardSounds.Count == 0) return false;
            CannSoundEntry entry = heardSounds[Random.Range(0, heardSounds.Count)];
            heardSounds.Remove(entry);
            audMan.PlaySingle(entry.soundObject);
            return true;
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
            finalList = finalList.ToList();
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

        public void FindLoops(bool useClassrooms = true)
        {
            loops.Clear();

            List<RoomController> roomsWithItems = ec.rooms.Where(x => (GetItemsInRoom(x).Length > 0)).ToList();


            List<RoomController> classRooms = ec.rooms.Where(x => _activity.GetValue(x) == null).ToList();
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

        public Cann_FlyLoop(NPC npc, int loops) : base(npc)
        {
            currentLoop = WeightedCannLoop.RandomSelection(cann.loops.ToArray());
            this.loops = loops;
        }

        public override void Enter()
        {
            base.Enter();
            npc.navigationStateMachine.ChangeState(new NavigationState_PredeterminedPath(cann, 0, currentLoop.cellsInLoop));
        }

        public override void DestinationEmpty()
        {
            loops--;
            PiratePlugin.Log.LogDebug(loops + " loops left!");
            if (loops <= 0)
            {
                PiratePlugin.Log.LogDebug("Cann finished loop cycle! Attempting room...");
                cann.behaviorStateMachine.ChangeState(new Cann_AttemptPerch(cann, currentLoop));
                return;
            }
            else
            {
                npc.navigationStateMachine.ChangeState(new NavigationState_PredeterminedPath(cann, 0, currentLoop.cellsInLoop));
            }
        }

        public override void HeardSound(SoundObject sound, AudioManager audMan)
        {
            base.HeardSound(sound, audMan);
            cann.AddSound(new CannSoundEntry(sound, audMan));
        }
    }

    public class Cann_Perch : Cann_StateBase
    {
        Pickup chosenPickup = null;

        public float timeUntilBoredomCheck = 0f;
        public int chanceToLeave = 0;

        public Cann_Perch(NPC npc, Pickup toPerchUpon) : base(npc)
        {
            chosenPickup = toPerchUpon;
        }
        public override void Enter()
        {
            base.Enter();
            cann.entity.SetFrozen(true);
            cann.entity.Teleport(chosenPickup.transform.position);
            cann.entity.OnTeleport += Teleported;
            chosenPickup.OnItemCollected += ItemCollected;
            timeUntilBoredomCheck = Random.Range(5f,10f);
        }

        public override void Update()
        {
            base.Update();
            if (chosenPickup == null) // incase some mod does something weird?
            {
                // todo: swap for flee state
                cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 3));
                return;
            }
            cann.spriteRenderer[0].gameObject.transform.position = chosenPickup.transform.GetComponentInChildren<SpriteRenderer>().transform.position + (Vector3.up * 1.28f);
            timeUntilBoredomCheck -= Time.deltaTime * cann.TimeScale;
            if (timeUntilBoredomCheck <= 0f)
            {
                timeUntilBoredomCheck = Random.Range(5f, 10f);
                if (!cann.PlayRandomSound())
                {
                    chanceToLeave = 100;
                }
                chanceToLeave += 5;
                if (UnityEngine.Random.Range(1, 100) <= chanceToLeave)
                {
                    // todo: add random chance to just switch to item in same room
                    cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 3));
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
        }

        void Teleported(Vector3 position)
        {
            cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 3));
        }

        void ItemCollected(Pickup pickup, int player)
        {
            cann.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 3));
        }

    }

    public class Cann_AttemptPerch : Cann_StateBase
    {

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

        public override void Enter()
        {
            base.Enter();
            if (selectedPickup == null)
            {
                npc.behaviorStateMachine.ChangeState(new Cann_FlyLoop(cann, 1));
                return;
            }
            npc.navigationStateMachine.ChangeState(new NavigationState_TargetPosition(npc, 32, selectedPickup.transform.position));
        }

        public override void DestinationEmpty()
        {
            if (Vector3.Distance(new Vector3(npc.transform.position.x, 0f, npc.transform.position.z), new Vector3(selectedPickup.transform.position.x, 0f, selectedPickup.transform.position.z)) < 10f)
            {
                PiratePlugin.Log.LogInfo("Succesful perch!");
                npc.behaviorStateMachine.ChangeState(new Cann_Perch(cann, selectedPickup));
            }
            else
            {
                SelectPickupFromRoom(chosenRoom);
                npc.navigationStateMachine.ChangeState(new NavigationState_TargetPosition(npc, 32, selectedPickup.transform.position));
            }
        }

        void SelectPickupFromRoom(RoomController rc, Pickup toIgnore = null)
        {
            List<WeightedSelection<Pickup>> weightedPickups = new List<WeightedSelection<Pickup>>();
            Pickup[] pickups = cann.GetItemsInRoom(rc);
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

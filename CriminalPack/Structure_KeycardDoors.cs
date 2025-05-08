using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace CriminalPack
{
    public enum KeycardPlacements
    {
        InFacultyRoom,
        InPreviousTierRoom,
        OutInTheOpen,
        AttachedToCharacter
    }

    public class WeightedKeycardPlacement : WeightedSelection<KeycardPlacements>
    {

    }

    public class KeycardPickupFollower : MonoBehaviour
    {
        public Pickup pickupToMove;
        public NPC npcToTrack;
        public Character characterEnumToSearch;
        public MovementModifier mm;
        public EnvironmentController ec;
        bool foundTarget = false;

        void Awake()
        {
            ec = GetComponent<EnvironmentController>();
            mm = new MovementModifier(Vector3.zero, 0.8f);
        }

        void Update()
        {
            if (pickupToMove == null) return;
            if (foundTarget && (!pickupToMove.gameObject.activeSelf))
            {
                npcToTrack.GetComponent<Entity>().ExternalActivity.moveMods.Remove(mm);
                Destroy(this);
                Destroy(pickupToMove.gameObject);
                return;
            }
            if (npcToTrack != null)
            {
                pickupToMove.transform.position = npcToTrack.transform.position + (npcToTrack.transform.forward * -5f);
                pickupToMove.icon.UpdatePosition(ec.map);
            }
            else
            {
                npcToTrack = ec.Npcs.Find(x => x.Character == characterEnumToSearch);
                if (npcToTrack != null && !foundTarget)
                {
                    pickupToMove.gameObject.SetActive(true);
                    pickupToMove.icon = ec.map.AddIcon(pickupToMove.iconPre, pickupToMove.transform, Color.white);
                    pickupToMove.icon.spriteRenderer.enabled = true;
                    npcToTrack.GetComponent<Entity>().ExternalActivity.moveMods.Add(mm);
                    foundTarget = true;
                }
            }
        }
    }

    public class Structure_KeycardDoors : StructureBuilder
    {
        public KeycardLockdownDoor[] doorPrefabs = new KeycardLockdownDoor[3];
        public ItemObject[] keycardItems = new ItemObject[3];
        public WindowObject windowObj;

        public List<List<RoomController>> lockedRooms = new List<List<RoomController>>()
        {
            new List<RoomController>(),
            new List<RoomController>(),
            new List<RoomController>()
        };
        public List<RoomController> lockedRoomsAll = new List<RoomController>();

        public WeightedKeycardPlacement[] potentialPlacements = new WeightedKeycardPlacement[]
        {
            new WeightedKeycardPlacement()
            {
                selection=KeycardPlacements.InFacultyRoom,
                weight=50
            },
            new WeightedKeycardPlacement()
            {
                selection=KeycardPlacements.InPreviousTierRoom,
                weight=60
            },
            new WeightedKeycardPlacement()
            {
                selection=KeycardPlacements.OutInTheOpen,
                weight=40
            },
            new WeightedKeycardPlacement()
            {
                selection=KeycardPlacements.AttachedToCharacter,
                weight=45
            }
        };

        public List<int> availableCards = new List<int>();
        LevelBuilder myBuilder;
        List<RoomController> classRooms = new List<RoomController>();
        List<Door> blockedDoors = new List<Door>();
        public List<Character> availableCharacterEnums = new List<Character>();

        KeycardManager kcm;

        void RepopulatePotentialCards(System.Random rng)
        {
            for (int i = 0; i < 3; i++)
            {
                int chosenClasses = rng.Next(parameters.minMax[i].x, parameters.minMax[i].z);
                for (int j = 0; j < chosenClasses; j++)
                {
                    availableCards.Add(i);
                }
            }
        }

        public override void Generate(LevelGenerator lg, System.Random rng)
        {
            base.Generate(lg, rng);
            kcm = lg.Ec.gameObject.AddComponent<KeycardManager>();
            myBuilder = lg;
            availableCharacterEnums.AddRange(lg.Ec.npcsToSpawn.Where(x => x.GetMeta().flags.HasFlag(NPCFlags.CanMove) && !x.GetMeta().tags.Contains("crmp_no_keycard")).Select(x => x.Character));

            availableCharacterEnums.Remove(Character.Baldi);

            classRooms.AddRange(lg.Ec.rooms.Where(x => x.HasIncompleteActivity));
            RepopulatePotentialCards(rng);

            while (classRooms.Count > 0)
            {
                int chosenIndex = rng.Next(availableCards.Count);
                int chosenCard = availableCards[chosenIndex];
                availableCards.RemoveAt(chosenIndex);
                int chosenClassIndex = rng.Next(classRooms.Count);
                BlockRoom(classRooms[chosenClassIndex], doorPrefabs[chosenCard], rng);
                classRooms.RemoveAt(chosenClassIndex);
                if (availableCards.Count == 0) // if we've ran out of a available keycards, repopulate again
                {
                    RepopulatePotentialCards(rng);
                }
            }

            List<RoomController> facultyRooms = ec.rooms.Where(x => x.category == RoomCategory.Faculty && (x.functions.GetComponent<LockedRoomFunction>() == null)).ToList();

            // assign 1 random faculty room to be keycard locked
            for (int i = 0; i < 3; i++)
            {
                if (facultyRooms.Count == 0) break;
                int chosenIndex = rng.Next(0, facultyRooms.Count);
                BlockRoom(facultyRooms[chosenIndex], doorPrefabs[i], rng);
                facultyRooms.RemoveAt(chosenIndex);
            }

            for (int i = 0; i < 3; i++)
            {
                if (lockedRooms[i].Count == 0) continue; // we got NOTHING assigned to us... no point in spawning the keycard
                System.Random controlledRandom = new System.Random(rng.Next());
                bool placedSuccesfully = false;
                while (!placedSuccesfully)
                {
                    KeycardPlacements placement = WeightedKeycardPlacement.ControlledRandomSelection(potentialPlacements, controlledRandom);
                    placedSuccesfully = PlaceKeycard(placement, i, controlledRandom);
                }
            }
        }

        public bool PlaceKeycard(KeycardPlacements placement, int id, System.Random rng)
        {
            switch (placement)
            {
                case KeycardPlacements.InFacultyRoom:
                    RoomController[] facultyRooms = ec.rooms.Where(x => x.category == RoomCategory.Faculty && x.itemSpawnPoints.Count > 0 && (x.functions.GetComponent<LockedRoomFunction>() == null) && !lockedRoomsAll.Contains(x)).ToArray();
                    if (facultyRooms.Length == 0) return false;
                    WeightedRoomController[] weightedRooms = new WeightedRoomController[facultyRooms.Length];
                    for (int i = 0; i < facultyRooms.Length; i++)
                    {
                        weightedRooms[i] = new WeightedRoomController()
                        {
                            selection = facultyRooms[i],
                            weight = facultyRooms[i].maxItemValue + 15
                        };
                    }
                    RoomController chosenRoom = WeightedRoomController.ControlledRandomSelection(weightedRooms, rng);
                    myBuilder.PlaceItemInRoom(keycardItems[id], chosenRoom, rng);
                    return true;
                case KeycardPlacements.InPreviousTierRoom:
                    if (id == 0) return false;
                    if (lockedRooms[id - 1].Count == 0) return false;
                    WeightedRoomController[] weightedPrevTierRooms = new WeightedRoomController[lockedRooms[id - 1].Count];
                    for (int i = 0; i < lockedRooms[id - 1].Count; i++)
                    {
                        weightedPrevTierRooms[i] = new WeightedRoomController()
                        {
                            selection = lockedRooms[id - 1][i],
                            weight = lockedRooms[id - 1][i].maxItemValue + 15
                        };
                    }
                    RoomController prevTierRoom = WeightedRoomController.ControlledRandomSelection(weightedPrevTierRooms, rng);
                    myBuilder.PlaceItemInRoom(keycardItems[id], prevTierRoom, rng);
                    return true;
                case KeycardPlacements.OutInTheOpen:
                    List<Cell> potentialPlaces = ec.mainHall.GetTilesOfShape(TileShapeMask.Corner, true);
                    if (potentialPlaces.Count == 0) return false;
                    Vector3 worldPos = potentialPlaces[rng.Next(potentialPlaces.Count)].CenterWorldPosition;
                    myBuilder.CreateItem(ec.mainHall, keycardItems[id], new Vector2(worldPos.x, worldPos.z), true);
                    return true;
                case KeycardPlacements.AttachedToCharacter:
                    if (availableCharacterEnums.Count == 0) return false;
                    Character chosenEnum = availableCharacterEnums[rng.Next(availableCharacterEnums.Count)];
                    int charIndex = ec.npcsToSpawn.IndexOf(ec.npcsToSpawn.First(x => x.Character == chosenEnum));
                    availableCharacterEnums.Remove(chosenEnum);
                    if (charIndex == -1) return false; //how
                    if ((ec.npcSpawnTile[charIndex] != null) && lockedRoomsAll.Contains(ec.npcSpawnTile[charIndex].room))
                    {
                        return false;
                    }
                    KeycardPickupFollower kpf = ec.gameObject.AddComponent<KeycardPickupFollower>();
                    Cell targetCell = ec.mainHall.ControlledRandomEntitySafeCellNoGarbage(rng);
                    kpf.pickupToMove = ec.CreateItem(targetCell.room, keycardItems[id], new Vector2(targetCell.CenterWorldPosition.x, targetCell.CenterWorldPosition.z));
                    kpf.characterEnumToSearch = chosenEnum;
                    kpf.pickupToMove.gameObject.SetActive(false);
                    return true;
                default:
                    throw new NotImplementedException("Unknown keycard placement: " + placement.ToString() + "!");
            }
        }

        public void BlockRoom(RoomController room, KeycardLockdownDoor kld, System.Random rng)
        {
            List<KeycardLockdownDoor> placedDoors = new List<KeycardLockdownDoor>();
            for (int j = 0; j < room.doors.Count; j++)
            {
                if (blockedDoors.Contains(room.doors[j])) continue; // dont block doors that have already been blocked
                PlaceDoor(kld, room.doors[j].bTile.position, room.doors[j].direction.GetOpposite(), 0f, false, out GameObject door);
                KeycardLockdownDoor doorComp = door.GetComponent<KeycardLockdownDoor>();
                kcm.lockdownDoors.Add(doorComp);
                blockedDoors.Add(room.doors[j]);
                placedDoors.Add(doorComp);
            }
            if (placedDoors.Count > 0)
            {
                lockedRooms[kld.myValue].Add(room);
                lockedRoomsAll.Add(room);
                LockedKeycardRoomFunction rf = room.functionObject.AddComponent<LockedKeycardRoomFunction>();
                rf.doors = placedDoors;
                room.functions.AddFunction(rf); // to prevent softlocks

                // todo: make it so it will more smartly find a potential window spot
                if (room.potentialDoorPositions.Count > 0)
                {
                    WeightedIntVector2[] potentialWindowPositions = new WeightedIntVector2[room.potentialDoorPositions.Count];
                    for (int i = 0; i < room.potentialDoorPositions.Count; i++)
                    {
                        Cell cellToEval = room.ec.CellFromPosition(room.potentialDoorPositions[i] + Directions.ToIntVector2(myBuilder.GetRandomPotentialWindowDirection(room, room.potentialDoorPositions[i], rng)));
                        potentialWindowPositions[i] = new WeightedIntVector2()
                        {
                            selection = room.potentialDoorPositions[i],
                            weight = 25 + (cellToEval.room.type == RoomType.Hall ? 100 : 0)
                        };
                    }
                    int potentialWindowPositionIndex = rng.Next(0, room.potentialDoorPositions.Count);
                    WindowObject oldObject = room.windowObject;
                    room.windowObject = windowObj;
                    myBuilder.BuildWindowIfPossible(room.potentialDoorPositions[potentialWindowPositionIndex], room, out _, out _);
                    room.RemoveFromDoorPositions(room.potentialDoorPositions[potentialWindowPositionIndex]);
                    room.windowObject = oldObject;
                }
            }
        }

        protected bool PlaceDoor(Door prefab, IntVector2 position, Direction direction, float forwardOffset, bool checkBlock, out GameObject newDoor)
        {
            IntVector2 intVector = direction.ToIntVector2();
            Cell cell = this.ec.CellFromPosition(position);
            Cell cell2 = this.ec.cells[position.x + intVector.x, position.z + intVector.z];
            Door door = UnityEngine.Object.Instantiate<Door>(prefab, cell.ObjectBase);
            newDoor = door.gameObject;
            door.transform.rotation = direction.ToRotation();
            door.ec = this.ec;
            door.position = cell.position;
            door.bOffset = direction.ToIntVector2();
            door.direction = direction;
            door.transform.position += door.transform.forward * forwardOffset;
            door.Initialize();
            if (checkBlock && (!ec.CheckPath(door.aTile, door.bTile, PathType.Nav) || !this.ec.CheckPath(door.bTile, door.aTile, PathType.Nav)))
            {
                door.UnInitialize();
                UnityEngine.Object.Destroy(door.gameObject);
                return false;
            }
            RendererContainer component = door.GetComponent<RendererContainer>();
            if (component != null)
            {
                ec.CellFromPosition(position).renderers.AddRange(component.renderers);
            }
            cell.HardCoverWall(direction, true);
            if (cell2 != null)
            {
                cell.HardCoverWall(direction.GetOpposite(), true);
            }
            return true;
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static UnityEngine.Random;

namespace PiratePack
{
    public class SunkenFloorController : MonoBehaviour
    {
        // a dummy room function used to make detection EASY with NO TRIGGERS!
        // and also doing it this way has the added benefit of if something somehow gets teleported here despite my best efforts
        // it will still hop onto the platform and not get stuck (i hope)
        public class SunkenFloorRoomFunction : RoomFunction
        {
            public SunkenFloorController myController;

            public override void OnEntityEnter(Entity entity)
            {
                base.OnEntityEnter(entity);
                myController.EntityEnter(entity);
            }

            public override void OnEntityExit(Entity entity)
            {
                base.OnEntityExit(entity);
                myController.EntityLeave(entity);
            }
        }

        public enum BoardState
        {
            Uninitialized = 0,
            AtStart = 1,
            AtEnd = 2,
            Transitioning = 4,
            GoingToStart = 5,
            GoingToEnd = 6
        }



        public void EntityEnter(Entity entity)
        {
            if ((overrider == null))
            {
                EntityOverrider potentialOverrider = new EntityOverrider();
                potentialOverrider.SetFrozen(true);
                potentialOverrider.SetInteractionState(false);
                //potentialOverrider.SetVisible(false);
                // if we can't override the entity, bounce it off
                if (!entity.Override(potentialOverrider)) { BounceEntity(entity); }
                overrider = potentialOverrider;
                StartCoroutine(BoardTransition(state.HasFlag(BoardState.AtStart), UnityEngine.Random.Range(10f,30f)));
            }
            else
            {
                BounceEntity(entity);
            }
        }

        public void EntityLeave(Entity left)
        {
            if (waitingToLeaveForNavigation == left)
            {
                waitingToLeaveForNavigation = null;
                SetCurrentValidWalk(state.HasFlag(BoardState.AtStart), false);
            }
        }

        public void BounceEntity(Entity ent)
        {
            throw new NotImplementedException();
        }

        IEnumerator BoardTransition(bool goingToEnd, float speed)
        {
            int increment = goingToEnd ? 1 : -1;
            int currentIndex = goingToEnd ? 0 : cells.Length - 1;
            int targetIndex = goingToEnd ? cells.Length - 1 : 0;
            SetCurrentValidWalk(goingToEnd, true);
            state = goingToEnd ? BoardState.GoingToEnd : BoardState.GoingToStart;
            while (currentIndex != targetIndex)
            {
                currentIndex += increment;
                float timeForTravel = 10f / speed;
                float timePassed = 0f;
                while (timePassed < timeForTravel)
                {
                    timePassed += Time.deltaTime * myRoom.ec.EnvironmentTimeScale;
                    board.transform.position = Vector3.Lerp(cells[currentIndex - increment].FloorWorldPosition, cells[currentIndex].FloorWorldPosition, timePassed / timeForTravel);
                    if (overrider.entity != null)
                    {
                        overrider.entity.Teleport(board.transform.position);
                    }
                    yield return null;
                }
                board.transform.position = cells[currentIndex].FloorWorldPosition;
                if (overrider.entity != null)
                {
                    overrider.entity.Teleport(board.transform.position);
                }
            }
            SetCurrentValidWalk(!goingToEnd, false);
            // we are no longer transitioning
            state &= ~BoardState.Transitioning;
            overrider.Release();
            if (overrider.entity != null)
            {
                overrider.entity.Teleport(board.transform.position);
                // block the direction we just came from navigation wise
                // to prevent npcs from just turning around.
                // todo: THIS STILL DOESN'T WORK! WHY?
                PiratePlugin.Log.LogInfo("Blocking: " + directions[targetIndex] + "!");
                BlockCell(cells[targetIndex].position, directions[targetIndex]); // wtf
                waitingToLeaveForNavigation = overrider.entity;
            }
            overrider = null;
        }

        public EntityOverrider overrider;
        public Entity waitingToLeaveForNavigation;
        public BoardState state = BoardState.Uninitialized;


        public Cell[] cells;
        public int startCellOriginalNavBin = 0;
        public int endCellOriginalNavBin = 0;
        public Direction[] directions;
        public GameObject[][] colliders;
        public RoomController myRoom;

        public GameObject board;

        public void Initialize()
        {
            startCellOriginalNavBin = cells[0].NavBin;
            endCellOriginalNavBin = cells[cells.Length - 1].NavBin;
            colliders = new GameObject[cells.Length][];

            List<Direction> allDirections = Directions.All();
            for (int i = 0; i < cells.Length; i++)
            {
                List<GameObject> collidersToClone = new List<GameObject>();
                foreach (Direction dir in allDirections)
                {
                    GameObject colliderToCheck = cells[i].Tile.Collider(dir);
                    // this isn't blocked off normally, we need to fix that.
                    if (!colliderToCheck.activeSelf)
                    {
                        collidersToClone.Add(colliderToCheck);
                    }
                }
                colliders[i] = new GameObject[collidersToClone.Count];
                // todo: change wall layer so npcs can see the player during travel
                for (int j = 0; j < collidersToClone.Count; j++)
                {
                    GameObject createdObject = Instantiate(collidersToClone[j], transform, true);
                    createdObject.SetActive(false); // just incase i change the logic for which colliders to clone
                    colliders[i][j] = createdObject;
                    colliders[i][j].transform.Rotate(new Vector3(0f,180f,0f)); //make colliders face outwards instead of inwards
                    //createdObject.transform.position = obj.transform.position;
                }
            }

            SetCurrentValidWalk(true);

            // initialize board position
            board.transform.position = cells[0].FloorWorldPosition;
            state = BoardState.AtStart;


            SunkenFloorRoomFunction func = myRoom.functions.gameObject.AddComponent<SunkenFloorRoomFunction>();
            func.myController = this;
            myRoom.functions.AddFunction(func);
        }


        public void SetExtraCollidersForCell(int index, bool value)
        {
            foreach (GameObject obj in colliders[index])
            {
                obj.SetActive(value);
            }
        }

        // all the blocks we have done
        // stored like this for easy undoing
        public List<(IntVector2, Direction)> blocks = new List<(IntVector2, Direction)>();

        public void BlockCell(IntVector2 position, Direction dir)
        {
            myRoom.ec.CellFromPosition(position).Block(dir, true);
            blocks.Add((position, dir));
        }

        public void BlockCell(Cell cell, Direction dir)
        {
            BlockCell(cell.position, dir);
        }

        public void BlockAllOutsideNavs(Cell c)
        {
            for (int i = 0; i < 4; i++)
            {
                IntVector2 cellPos = c.position + ((Direction)i).ToIntVector2();
                if (myRoom.cells.Contains(myRoom.ec.CellFromPosition(cellPos))) continue;
                BlockCell(cellPos, ((Direction)i).GetOpposite());
            }
        }

        public void UnblockAllCells()
        {
            PiratePlugin.Log.LogInfo("Unblocking all blocked cells!");
            foreach ((IntVector2, Direction) block in blocks)
            {
                myRoom.ec.CellFromPosition(block.Item1).Block(block.Item2, false);
            }
            PiratePlugin.Log.LogInfo("Unblocked " + blocks.Count + " blocks!");
            blocks.Clear();
        }

        public void SetCurrentValidWalk(bool startingCellDir, bool blockOffBoth = false)
        {
            UnblockAllCells();
            for (int i = 0; i < cells.Length; i++)
            {
                // this is the starting cell, meaning it can be entered from any regularly valid direction (so npcs can get on the board)
                // if we are supposed to block off both ends (because the board is travelling) block it off instead
                if (i == (startingCellDir ? 0 : (cells.Length - 1)))
                {
                    cells[i].NavBin = blockOffBoth ? 15 : (startingCellDir ? startCellOriginalNavBin : endCellOriginalNavBin);
                    SetExtraCollidersForCell(i, blockOffBoth);
                    if (blockOffBoth)
                    {
                        BlockAllOutsideNavs(cells[i]);
                    }
                    continue;
                }
                // this is the ending cell, meaning it can't be entered from any direction as the platform isn't there
                if (i == (startingCellDir ? (cells.Length - 1) : 0))
                {
                    cells[i].NavBin = 15;
                    SetExtraCollidersForCell(i, true);
                    BlockAllOutsideNavs(cells[i]);
                    continue;
                }
                cells[i].NavBin = 15 & ~(startingCellDir ? (directions[i].ToBinary()) : (directions[i].GetOpposite().ToBinary()));

                SetExtraCollidersForCell(i, true);
                BlockAllOutsideNavs(cells[i]);
            }
            myRoom.ec.RecalculateNavigation();
        }
    }



    public class Structure_SunkenFloor : StructureBuilder
    {

        public class SunkenFloorInstance
        {
            public RoomController room;
            public List<Cell> chosenCells = new List<Cell>();
            public List<Direction> followedDirections = new List<Direction>();
        }

        public Texture2D transparentTexture; // should be assigned to "Transparent"

        public List<SunkenFloorInstance> createdInstances = new List<SunkenFloorInstance>();

        public SunkenFloorController controllerPrefab;

        public override void Initialize(EnvironmentController ec, StructureParameters parameters)
        {
            base.Initialize(ec, parameters);
        }

        public SunkenFloorInstance AttemptBuild(Cell startingCell, LevelGenerator lg, System.Random rng)
        {
            SunkenFloorInstance instance = new SunkenFloorInstance();

            int maxPossibleSize = Mathf.Max(Mathf.Min(ec.levelSize.x, ec.levelSize.z) / 3, 7);

            int maxCells = rng.Next(6, maxPossibleSize + 1); // the max amount of cells this stream can cover

            int attempts = 0;

            while (attempts < 10)
            {
                attempts++;
                instance.chosenCells.Clear();
                instance.chosenCells.Add(startingCell);
                Direction targetDirection = startingCell.RandomUncoveredDirection(rng);

                if (AddCellsUntilLimitHit(instance, maxCells, targetDirection, rng))
                {
                    instance.room = GameObject.Instantiate<RoomController>(lg.roomControllerPre, ec.transform);
                    instance.room.type = RoomType.Room;
                    instance.room.category = PiratePlugin.sunkenFloorRoomCat;
                    instance.room.name = "Sunken Floor Room";

                    instance.room.wallTex = ec.mainHall.wallTex;
                    instance.room.ceilTex = ec.mainHall.ceilTex;
                    instance.room.florTex = transparentTexture; //no floor! so water is VISIBLE!!
                    instance.room.ec = ec;
                    instance.room.color = new Color(214f / 255f, 232f / 255f, 255f / 255f);
                    instance.room.GenerateTextureAtlas();

                    ec.rooms.Add(instance.room);

                    return instance;
                }
            }

            PiratePlugin.Log.LogWarning("Couldn't create SunkenFloorInstance after 10 attempts! Bad spawn location at " + startingCell.position.x + ", " + startingCell.position.z + "?");

            return null;

        }

        public bool AddCellsUntilLimitHit(SunkenFloorInstance instance, int cellLimit, Direction targetDirection, System.Random rng)
        {

            List<Direction> availableDirections = new List<Direction>();
            Direction previousDirection = Direction.Null;


            while ((instance.chosenCells.Count < cellLimit))
            {
                Cell currentCell = instance.chosenCells[instance.chosenCells.Count - 1];
                Directions.FillOpenDirectionsFromBin(availableDirections, currentCell.NavBin | currentCell.ConstBin);
                availableDirections.Remove(previousDirection.GetOpposite());
                availableDirections.Remove(targetDirection.GetOpposite());
                availableDirections.RemoveAll(x => ec.CellFromPosition(currentCell.position + x.ToIntVector2()).room.type != RoomType.Hall); // remove all non-hall tiles
                availableDirections.RemoveAll(x => ec.CellFromPosition(currentCell.position + x.ToIntVector2()).HardCoverageBin == 15);
                //availableDirections.RemoveAll(x => !ec.mainHall.entitySafeCells.Contains(currentCell.position + x.ToIntVector2())); // remove all non-entity safe cells

                if (availableDirections.Count == 0)
                {
                    if (instance.chosenCells.Count <= 2)
                    {
                        PiratePlugin.Log.LogInfo("SunkenFloor generator got shoved into corner, failure...");
                        return false;
                    }
                    PiratePlugin.Log.LogInfo("SunkenFloor generator got shoved into corner, exiting w/o failure...");
                    break;
                }

                Direction chosenDirection;

                if (availableDirections.Contains(targetDirection))
                {
                    chosenDirection = targetDirection;
                }
                else
                {
                    chosenDirection = availableDirections[rng.Next(0, availableDirections.Count)];
                }

                previousDirection = chosenDirection;

                instance.chosenCells.Add(ec.CellFromPosition(currentCell.position + chosenDirection.ToIntVector2()));
                instance.followedDirections.Add(chosenDirection);
            }

            instance.followedDirections.Add(instance.followedDirections[instance.followedDirections.Count - 1]);

            // perform sanity check

            if (CheckCellForValidExits(instance, instance.chosenCells[0]))
            {
                PiratePlugin.Log.LogInfo("SunkenFloor generator's entrance has no exits...");
                return false;
            }

            if (CheckCellForValidExits(instance, instance.chosenCells[instance.chosenCells.Count - 1]))
            {
                PiratePlugin.Log.LogInfo("SunkenFloor generator's exit has no exits...");
                return false;
            }

            return true;
        }

        public bool CheckCellForValidExits(SunkenFloorInstance instance, Cell cellToCheck)
        {
            List<Direction> availableDirections = new List<Direction>();
            Directions.FillOpenDirectionsFromBin(availableDirections, cellToCheck.NavBin | cellToCheck.ConstBin);
            availableDirections.RemoveAll(x => ec.CellFromPosition(cellToCheck.position + x.ToIntVector2()).room.type != RoomType.Hall); // remove all non-hall tiles
            availableDirections.RemoveAll(x => !ec.mainHall.entitySafeCells.Contains(cellToCheck.position + x.ToIntVector2())); // remove all non-entity safe cells
            availableDirections.RemoveAll(x => instance.chosenCells.Find(z => z.position == (cellToCheck.position + x.ToIntVector2())) != null); // remove all cells that we own

            return availableDirections.Count > 0;
        }

        public override void PostHallPrep(LevelGenerator lg, System.Random rng)
        {
            base.PostHallPrep(lg, rng);

            List<Cell> potentialCells = ec.AllCells().Where(x => x.room.type == RoomType.Hall && !x.offLimits).ToList();

            for (int i = 0; i < 2; i++)
            {
                SunkenFloorInstance instance = AttemptBuild(potentialCells[rng.Next(0, potentialCells.Count)], lg, rng);
                if (instance != null)
                {
                    createdInstances.Add(instance);
                    foreach (Cell chosenCell in instance.chosenCells)
                    {
                        chosenCell.HardCoverEntirely();
                        chosenCell.offLimits = true;
                        // since room hasn't been changed yet to make sure the room connects to the hallways
                        // .room here refers to the hallway
                        chosenCell.room.potentialDoorPositions.Remove(chosenCell.position);
                        chosenCell.room.entitySafeCells.Remove(chosenCell.position);
                        chosenCell.room.eventSafeCells.Remove(chosenCell.position);
                    }
                }
            }

        }

        public override void Generate(LevelGenerator lg, System.Random rng)
        {
            base.Generate(lg, rng);

            foreach (SunkenFloorInstance instance in createdInstances)
            {
                foreach (Cell chosenCell in instance.chosenCells)
                {
                    ec.CreateCell(chosenCell.ConstBin, instance.room.transform, chosenCell.position, instance.room, true, false);
                    chosenCell.offLimits = false;
                }
                SunkenFloorController sfc = GameObject.Instantiate<SunkenFloorController>(controllerPrefab, instance.room.transform);
                sfc.cells = instance.chosenCells.ToArray();
                sfc.directions = instance.followedDirections.ToArray();
                sfc.myRoom = instance.room;
                sfc.Initialize();
            }
        }
    }
}

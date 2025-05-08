using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.PlusExtensions;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{
    public class Structure_Scanner : StructureBuilder
    {

        public ItemScanner prefab;

        public override void PostOpenCalcGenerate(LevelGenerator lg, System.Random rng)
        {
            base.PostOpenCalcGenerate(lg, rng);
            List<List<Cell>> halls = lg.Ec.FindHallways();
            for (int i = 0; i < halls.Count; i++)
            {
                halls[i].RemoveAll(x => Directions.OpenDirectionsFromBin(x.ConstBin).Count > 2);
                halls[i].RemoveAll(x => x.shape == TileShapeMask.Corner);
            }
            halls.RemoveAll(x => x.Count == 0);
            int scannerCount = rng.Next(parameters.minMax[0].x, parameters.minMax[0].z);
            for (int i = 0; i < scannerCount; i++)
            {
                if (halls.Count == 0)
                {
                    Debug.LogWarning("Couldn't find hall for scanner #" + i + "!");
                    return;
                }
                int chosenHallIndex = rng.Next(0, halls.Count);
                Cell chosenCell = halls[chosenHallIndex][rng.Next(0, halls[chosenHallIndex].Count)];
                halls.RemoveAt(chosenHallIndex);
                List<Direction> potentialDirections = Directions.OpenDirectionsFromBin(chosenCell.ConstBin);
                Direction chosenDirection = potentialDirections[rng.Next(0, potentialDirections.Count)];
                Place(chosenCell, chosenDirection);
            }
        }

        // time to generate posters!
        public override void Generate(LevelGenerator lg, System.Random rng)
        {
            base.Generate(lg, rng);

            List<ExtendedPosterObject> possiblePosters = new List<ExtendedPosterObject>(CriminalPackPlugin.itemPosters);
            RoomController[] posterableRooms = ec.rooms.Where(x => x.HasFreeWall).ToArray();
            if (ec.mainHall.HasFreeWall)
            {
                posterableRooms = posterableRooms.AddToArray(ec.mainHall);
            }
            List<WeightedRoomController> potentialPosterRooms = new List<WeightedRoomController>();
            for (int i = 0; i < posterableRooms.Length; i++)
            {
                potentialPosterRooms.Add(new WeightedRoomController()
                {
                    selection = posterableRooms[i],
                    weight = (posterableRooms[i].type == RoomType.Hall ? 600 : 25 + posterableRooms[i].cells.Count) / (posterableRooms[i].category == RoomCategory.Special ? 5 : 1)
                });
            }

            int chosenPosterCount = rng.Next(parameters.minMax[1].x, parameters.minMax[1].z);

            int placedPosters = 0;
            while ((placedPosters != chosenPosterCount) && (potentialPosterRooms.Count > 0))
            {
                RoomController selectedRoom = WeightedRoomController.ControlledRandomSelectionList(WeightedRoomController.Convert(potentialPosterRooms), rng);
                if (possiblePosters.Count == 0)
                {
                    possiblePosters = new List<ExtendedPosterObject>(CriminalPackPlugin.itemPosters);
                }
                ExtendedPosterObject chosenPoster = possiblePosters[rng.Next(0, possiblePosters.Count)];
                possiblePosters.Remove(chosenPoster);
                ec.BuildPosterInRoom(selectedRoom, chosenPoster, rng);
                potentialPosterRooms.RemoveAll(x => !x.selection.HasFreeWall);
                placedPosters++;
            }
            
        }

        public void Place(Cell cellAt, Direction dir)
        {
            ItemScanner scanner = GameObject.Instantiate<ItemScanner>(prefab, cellAt.room.objectObject.transform);
            scanner.transform.position = cellAt.FloorWorldPosition;
            scanner.transform.rotation = dir.ToRotation();
            scanner.ec = ec;
            scanner.room = cellAt.room;
            cellAt.HardCoverEntirely();
        }
    }

    public class ItemScanner : MonoBehaviour
    {

        public AudioManager audMan;
        public SoundObject scanStart;
        public SoundObject scanGood;
        public SoundObject scanBad;

        public MeshRenderer[] lightMeshes;

        // how much time it should take for items to process
        public float minTimeToProcess = 0.3f;
        public float maxTimeToProcess = 2f;
        public EnvironmentController ec;
        public RoomController room;

        // this isn't a dictionary due to unity serialization
        public Material greenLight;
        public Material yellowLight;
        public Material redLight;
        public Material blackLight;

        public void SwapMaterial(Material swapTo)
        {
            for (int i = 0; i < lightMeshes.Length; i++)
            {
                lightMeshes[i].materials = new Material[2] {
                    lightMeshes[i].materials[0],
                    swapTo
                };
            }
        }

        public bool powered { get; protected set; } = true;

        IEnumerator currentWaitingForPowerNumerator = null;

        public void SetPower(bool power)
        {
            if (power == powered) return; // dont do logic if the attempted state is our current one
            if (!power && (currentWaitingForPowerNumerator != null)) return; // dont do logic if we are being shut down and currently have one queued.
            if (power)
            {
                powered = true;
                SwapMaterial(greenLight);
                if (currentWaitingForPowerNumerator != null)
                {
                    StopCoroutine(currentWaitingForPowerNumerator);
                    currentWaitingForPowerNumerator = null;
                }
            }
            else
            {
                currentWaitingForPowerNumerator = WaitTilValidPowerdownableState();
                StartCoroutine(currentWaitingForPowerNumerator);
            }
        }

        IEnumerator WaitTilValidPowerdownableState()
        {
            while ((timeTilReset != 0f) && (timeTilProcess != 0f))
            {
                yield return null;
            }
            audMan.FlushQueue(true);
            SwapMaterial(blackLight);
            powered = false;
        }

        float timeTilProcess = 0f;
        PlayerManager targetPlayer;
        List<Items> foundContraband = new List<Items>();
        int playerItemCount = 0;

        float timeTilReset = 0f;

        void SendPrincipal(Principal npc)
        {
            npc.WhistleReact(transform.position);
            npc.GetComponent<AudioManager>().FlushQueue(true);
        }

        bool initiatedShutoff = false;

        IEnumerator PowerOffAfterTime(float time)
        {
            yield return new WaitForSecondsEnvironmentTimescale(ec, time);
            SetPower(false);
        }

        void Update()
        {
            if (!powered) return;
            if (room.type == RoomType.Hall)
            {
                if (ec.timeOut && !initiatedShutoff)
                {
                    StartCoroutine(PowerOffAfterTime(UnityEngine.Random.Range(15f, 120f)));
                    initiatedShutoff = true;
                }
            }
            else
            {
                SetPower(room.Powered);
            }
            if (timeTilReset > 0f)
            {
                timeTilReset -= Time.deltaTime * ec.EnvironmentTimeScale;
                if (timeTilReset <= 0f)
                {
                    SwapMaterial(greenLight);
                    timeTilReset = 0f;
                    targetPlayer = null;
                }
            }
            if (timeTilProcess > 0f)
            {
                timeTilProcess -= Time.deltaTime * ec.EnvironmentTimeScale;
                if (timeTilProcess <= 0f)
                {
                    timeTilProcess = 0f;
                    if (targetPlayer == null)
                    {
                        CriminalPackPlugin.Log.LogWarning("Scanner went off without targetPlayer?? What the fuck?");
                        return;
                    }

                    Image[] itemImages = (Image[])_itemSprites.GetValue(Singleton<CoreGameManager>.Instance.GetHud(targetPlayer.playerNumber));
                    for (int i = 0; i < targetPlayer.itm.maxItem + 1; i++)
                    {
                        if (((bool[])_slotLocked.GetValue(targetPlayer.itm))[i]) continue;
                        if (itemImages[i] != null)
                        {
                            if (itemImages[i].GetComponent<ScannerItemSpriteFade>())
                            {
                                Destroy(itemImages[i].GetComponent<ScannerItemSpriteFade>());
                            }
                            itemImages[i].gameObject.AddComponent<ScannerItemSpriteFade>().myColor = foundContraband.Contains(targetPlayer.itm.items[i].itemType) ? Color.red : Color.green;
                        }
                    }

                    if (playerItemCount == 0)
                    {
                        targetPlayer = null;
                        audMan.FlushQueue(true);
                        timeTilReset = 0.1f;
                        return;
                    }
                    if (foundContraband.Count > 0)
                    {
                        SwapMaterial(redLight);
                        audMan.FlushQueue(true);
                        audMan.PlaySingle(scanBad);
                        ec.MakeNoise(transform.position, 11);
                        timeTilReset = 5f;
                        targetPlayer.RuleBreak("Contraband", 6f);
                        for (int i = 0; i < ec.Npcs.Count; i++)
                        {
                            // doing this instead of enum just incase someone makes a principal alternative
                            if (ec.Npcs[i] is Principal)
                            {
                                SendPrincipal((Principal)ec.Npcs[i]);
                            }
                        }
                    }
                    else
                    {
                        targetPlayer = null;
                        audMan.FlushQueue(true);
                        audMan.PlaySingle(scanGood);
                        SwapMaterial(greenLight);
                    }
                }
            }
        }

        void ActivateForPlayer(PlayerManager pm)
        {
            SwapMaterial(yellowLight);
            audMan.PlaySingle(scanStart);
            targetPlayer = pm;
            timeTilProcess = Mathf.Lerp(minTimeToProcess,maxTimeToProcess, playerItemCount / (float)(pm.itm.maxItem + 1f));
        }


        static FieldInfo _slotLocked = AccessTools.Field(typeof(ItemManager), "slotLocked");
        static FieldInfo _itemSprites = AccessTools.Field(typeof(HudManager), "itemSprites");

        void OnTriggerEnter(Collider other)
        {
            if (!powered) return;
            if (timeTilReset != 0f) return;
            if (timeTilProcess != 0f) return;
            if (!other.gameObject.CompareTag("Player")) return;
            PlayerManager foundPlayer = other.GetComponent<PlayerManager>();
            if (foundPlayer.Tagged) return;
            foundContraband.Clear();
            playerItemCount = 0;
            for (int i = 0; i < foundPlayer.itm.maxItem + 1; i++)
            {

                if (((bool[])_slotLocked.GetValue(foundPlayer.itm))[i]) continue;
                ItemMetaData meta = foundPlayer.itm.items[i].GetMeta();
                if (meta == null) continue;
                if (meta.id == Items.None) continue;
                playerItemCount++;
                if (meta.tags.Contains("crmp_contraband"))
                {
                    foundContraband.Add(foundPlayer.itm.items[i].itemType);
                }
            }
            ActivateForPlayer(foundPlayer);
        }
    }
}

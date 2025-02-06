using HarmonyLib;
using MTM101BaldAPI.Registers;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class Structure_Scanner : StructureBuilder
    {

        public ItemScanner prefab;

        public override void PostOpenCalcGenerate(LevelGenerator lg, System.Random rng)
        {
            base.PostOpenCalcGenerate(lg, rng);
            List<Cell> validCells = lg.Ec.mainHall.GetTilesOfShape(TileShapeMask.Straight, false);
            for (int i = 0; i < 6; i++)
            {
                int chosenCellIndex = rng.Next(0, validCells.Count);
                Cell chosenCell = validCells[chosenCellIndex];
                validCells.RemoveAt(chosenCellIndex);
                List<Direction> potentialDirections = Directions.OpenDirectionsFromBin(chosenCell.ConstBin);
                Direction chosenDirection = potentialDirections[rng.Next(0, potentialDirections.Count)];
                Place(chosenCell, chosenDirection);
                chosenCell.HardCoverEntirely();
            }
        }

        public void Place(Cell cellAt, Direction dir)
        {
            ItemScanner scanner = GameObject.Instantiate<ItemScanner>(prefab, cellAt.room.objectObject.transform);
            scanner.transform.position = cellAt.FloorWorldPosition;
            scanner.transform.rotation = dir.ToRotation();
            scanner.ec = ec;
        }
    }

    public class ItemScanner : MonoBehaviour
    {
        public MeshRenderer[] lightMeshes;

        // how much time it should take for items to process
        public float timeToProcess = 1f;
        public EnvironmentController ec;

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

        float timeTilProcess = 0f;
        PlayerManager targetPlayer;
        List<Items> foundContraband = new List<Items>();

        float timeTilReset = 0f;

        void SendPrincipal(Principal npc)
        {
            npc.WhistleReact(transform.position);
            npc.GetComponent<AudioManager>().FlushQueue(true);
        }

        void Update()
        {
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
                    if (foundContraband.Count > 0)
                    {
                        SwapMaterial(redLight);
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
                        SwapMaterial(greenLight);
                    }
                }
            }
        }

        void ActivateForPlayer(PlayerManager pm)
        {
            SwapMaterial(yellowLight);
            targetPlayer = pm;
            timeTilProcess = timeToProcess;
        }


        static FieldInfo _slotLocked = AccessTools.Field(typeof(ItemManager), "slotLocked");

        void OnTriggerEnter(Collider other)
        {
            if (timeTilReset != 0f) return;
            if (timeTilProcess != 0f) return;
            if (!other.gameObject.CompareTag("Player")) return;
            foundContraband.Clear();
            PlayerManager foundPlayer = other.GetComponent<PlayerManager>();
            for (int i = 0; i < foundPlayer.itm.maxItem + 1; i++)
            {

                if (((bool[])_slotLocked.GetValue(foundPlayer.itm))[i]) continue;
                ItemMetaData meta = foundPlayer.itm.items[i].GetMeta();
                if (meta == null) continue;
                if (meta.tags.Contains("contraband"))
                {
                    foundContraband.Add(foundPlayer.itm.items[i].itemType);
                }
            }
            ActivateForPlayer(foundPlayer);
        }
    }
}

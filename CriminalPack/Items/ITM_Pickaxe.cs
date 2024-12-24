using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class ITM_Pickaxe : Item
    {
        private RaycastHit hit;

        public WindowObject windowToPlace;

        public int uses;

        public bool SwitchToNextUse(PlayerManager pm)
        {
            if (uses > 0)
            {
                pm.itm.SetItem(CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("Pickaxe" + uses), pm.itm.selectedItem);
                return false;
            }
            return true;
        }

        public override bool Use(PlayerManager pm)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            if (!Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out hit, pm.pc.reach, pm.pc.ClickLayers, QueryTriggerInteraction.Ignore))
            {
                UnityEngine.Object.Destroy(base.gameObject);
                return false;
            }
            if (!hit.transform.CompareTag("Wall"))
            {
                UnityEngine.Object.Destroy(base.gameObject);
                return false;
            }
            Direction direction = Directions.DirFromVector3(this.hit.transform.forward, 5f);
            if (!pm.ec.ContainsCoordinates(IntVector2.GetGridPosition(hit.transform.position + hit.transform.forward * -5f)))
            {
                UnityEngine.Object.Destroy(base.gameObject);
                return false;
            }
            Cell cell = pm.ec.CellFromPosition(IntVector2.GetGridPosition(hit.transform.position + hit.transform.forward * 5f));
            Cell cell2 = pm.ec.CellFromPosition(IntVector2.GetGridPosition(hit.transform.position + hit.transform.forward * -5f));
            if (cell2.WallHardCovered(direction))
            {
                UnityEngine.Object.Destroy(base.gameObject);
                return false;
            }
            //Debug.Log(cell.ConstBin);
            //Debug.Log(cell.position.x);
            //Debug.Log(cell.position.z);
            if (cell.ConstBin == 0) //the cell is empty, activate standard digging logic.
            {
                RoomController roomToBelongTo = cell2.room;
                RoomController digRoom = pm.ec.GetPickaxeRoom();
                UnityEngine.Debug.Log("1");
                cell.RemoveChunkProperly();
                Chunk oldChunk = cell2.Chunk;
                if (oldChunk == null)
                {
                    oldChunk = cell.Chunk;
                }
                cell2.RemoveChunkProperly();
                UnityEngine.Debug.Log("2");
                cell.openTileGroup.cells.Remove(cell);
                cell.openTileGroup.exitCells.Remove(cell);
                cell.openTileGroup.exits.RemoveAll(x => x.cell == cell);
                cell.openTileGroup = new OpenTileGroup();
                cell.openTileGroup.cells.Add(cell);
                UnityEngine.Debug.Log("3");
                pm.ec.CreateCell(0, digRoom.transform, cell.position, digRoom, false, false);
                pm.ec.CreateCell(0, roomToBelongTo.transform, cell2.position, cell2.room, false, false);
                UnityEngine.Debug.Log("4");
                pm.ec.UpdateCell(cell2.position);
                pm.ec.UpdateCell(cell.position);
                UnityEngine.Debug.Log("5");
                // fix chunks
                Cell newCell = pm.ec.CellFromPosition(cell.position);
                Cell newCell2 = pm.ec.CellFromPosition(cell2.position);
                UnityEngine.Debug.Log("6");
                newCell.SetChunk(oldChunk);
                newCell2.SetChunk(oldChunk);
                oldChunk.AddCell(newCell);
                oldChunk.AddCell(newCell2);
                UnityEngine.Debug.Log("7");
                // place window and fix old window placements messed up by updatecell
                pm.ec.BuildWindow(newCell, direction.GetOpposite(), windowToPlace, false);
                UnityEngine.Debug.Log("8");
                newCell = pm.ec.CellFromPosition(cell.position);
                newCell2 = pm.ec.CellFromPosition(cell2.position);
                pm.ec.map.Find(cell.position.x, cell.position.z, newCell.ConstBin, newCell.room);
                pm.ec.map.Find(cell2.position.x, cell2.position.z, newCell2.ConstBin, newCell2.room);
                UnityEngine.Debug.Log("10");
                // perform some extra logic so we dont have to waste a pickaxe use just to finish out a dig
                Cell cell3 = pm.ec.CellFromPosition(cell2.position + direction.ToIntVector2());
                if ((!cell3.Null) && !cell3.WallHardCovered(direction))
                {
                    pm.ec.BuildWindow(cell3, direction, windowToPlace, false);
                }
                UnityEngine.Debug.Log("11");
                Window[] windows = roomToBelongTo.transform.GetComponentsInChildren<Window>();
                for (int i = 0; i < windows.Length; i++)
                {
                    pm.ec.ConnectCells(windows[i].position, windows[i].direction);
                }
                UnityEngine.Debug.Log("12");
                pm.ec.CullingManager.CalculateOcclusionCullingForChunk(oldChunk.Id);
                digRoom.entitySafeCells.Add(newCell.position);
                UnityEngine.Object.Destroy(base.gameObject);
                UnityEngine.Debug.Log("13");
                return SwitchToNextUse(pm);
            }
            if (!cell.WallHardCovered(direction.GetOpposite()))
            {
                pm.ec.BuildWindow(cell2, direction, windowToPlace, false);
                UnityEngine.Object.Destroy(base.gameObject);
                return SwitchToNextUse(pm);
            }
            UnityEngine.Object.Destroy(base.gameObject);
            return false;
        }
    }
}

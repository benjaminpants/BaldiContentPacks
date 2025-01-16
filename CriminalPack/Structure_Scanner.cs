using System;
using System.Collections.Generic;
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
        }
    }

    public class ItemScanner : MonoBehaviour
    {

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LevelTyped;
using MTM101BaldAPI;
using UnityEngine;

// this is all in a seperate file with a seperate class as to avoid causing invalid code to be generated when level typed isn't present
// because as long as this code is never encountered without level typed it wont break.

namespace CriminalPack
{
    internal static class LevelTypedAdder
    {
        internal static void Add()
        {
            LevelTypedPlugin.Instance.AddExtraGenerator(new LevelTypedPrisonGen());
        }
    }


    public class LevelTypedPrisonGen : LevelTypedGenerator
    {
        public override LevelType levelTypeToBaseOff => LevelType.Factory;

        public override LevelType myLevelType => CriminalPackPlugin.prisonType;

        public override string levelObjectName => "Prison";

        public override void ApplyChanges(string levelName, int levelId, CustomLevelObject obj)
        {
            obj.type = CriminalPackPlugin.prisonType;
            List<StructureWithParameters> structures = obj.forcedStructures.ToList();
            structures.RemoveAll(x => x.prefab is Structure_Rotohalls);
            structures.RemoveAll(x => x.prefab is Structure_ConveyorBelt);
            structures.RemoveAll(x => x.prefab.name == "LockdownDoorConstructor");
            structures.RemoveAll(x => x.prefab is Structure_LevelBox);
            obj.forcedStructures = structures.ToArray();
            obj.potentialSpecialRooms = new WeightedRoomAsset[0];
            obj.minSpecialRooms = 0;
            obj.maxSpecialRooms = 0;
            CriminalPackPlugin.Instance.ModifyIntoPrison(obj, levelId);
            LevelTypedPlugin.Instance.AddBusPassEnsurerIfNecessary(obj);
        }

        public override int GetWeight(int defaultWeight)
        {
            return Mathf.CeilToInt(base.GetWeight(defaultWeight) * 0.9f);
        }

        public override bool ShouldGenerate(string levelName, int levelId, SceneObject sceneObject)
        {
            return !CriminalPackPlugin.Instance.ShouldGeneratePrisonType(levelName,levelId,sceneObject);
        }
    }
}

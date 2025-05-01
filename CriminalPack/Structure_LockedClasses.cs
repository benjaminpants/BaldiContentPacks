using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class Structure_LockedClasses : StructureBuilder
    {

        public ItemObject[] keys;
        public GameLock[] lockPrefabs;

        public readonly static FieldInfo _lockPrefab = AccessTools.Field(typeof(LockedRoomFunction), "lockPrefab");
        public readonly static FieldInfo _key = AccessTools.Field(typeof(LockedRoomFunction), "key");
        readonly static FieldInfo _potentialKeys = AccessTools.Field(typeof(LockedRoomFunction), "potentialKeys"); // WHAT THE FUCK

        public override void Generate(LevelGenerator lg, System.Random rng)
        {
            base.Generate(lg, rng);
            int createdFunctions = 0;
            for (int i = 0; i < lg.Ec.rooms.Count; i++)
            {
                if (lg.Ec.rooms[i].HasIncompleteActivity) //does this room have an activity/notebook?
                {
                    LockedRoomFunction func = lg.Ec.rooms[i].functions.gameObject.AddComponent<LockedRoomFunction>();
                    _key.SetValue(func, keys.Clone());
                    _lockPrefab.SetValue(func, lockPrefabs.Clone());
                    lg.Ec.rooms[i].functions.AddFunction(func);
                    func.Initialize(lg.Ec.rooms[i]); //we missed the initialize call, so call it now.
                    createdFunctions++;
                }
            }
            List<ItemObject> potKeys = (List<ItemObject>)_potentialKeys.GetValue(null);
            potKeys.Clear();
            // IM GOING TO KILL EVERYONE STARTING WITH YOU
            for (int i = 0; i < createdFunctions; i++)
            {
                potKeys.AddRange(keys);
            }
        }
    }
}

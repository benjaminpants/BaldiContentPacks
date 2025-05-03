using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class KeycardLockdownDoor : LockdownDoor
    {
        public int myValue;

        public bool openForever = false;

        public void OpenForever()
        {
            openForever = true;
            Open(true, false);
        }

        public override void Shut()
        {
            if (openForever) return; //NO.
            base.Shut();
        }
    }

    public class LockedKeycardRoomFunction : RoomFunction
    {
        public List<KeycardLockdownDoor> doors = new List<KeycardLockdownDoor>();

        public override void OnPlayerEnter(PlayerManager player)
        {
            base.OnPlayerEnter(player);
            for (int i = 0; i < doors.Count; i++)
            {
                doors[i].Open(true, false);
            }
        }

        public override void OnPlayerExit(PlayerManager player)
        {
            base.OnPlayerExit(player);
            for (int i = 0; i < doors.Count; i++)
            {
                if (doors[i].openForever) continue;
                doors[i].Shut();
            }
        }
    }

    public class KeycardManager : MonoBehaviour
    {
        public List<KeycardLockdownDoor> lockdownDoors = new List<KeycardLockdownDoor>();

        public void AcquireKeycard(int id)
        {
            for (int i = lockdownDoors.Count - 1; i >= 0; i--)
            {
                if (lockdownDoors[i].myValue == id)
                {
                    lockdownDoors[i].OpenForever();
                    lockdownDoors.RemoveAt(i);
                }
            }

            for (int i = 0; i < Singleton<CoreGameManager>.Instance.TotalPlayers; i++)
            {
                Singleton<CoreGameManager>.Instance.GetHud(i).GetComponent<KeycardHud>().GiveCard(id);
            }
        }
    }

    [HarmonyPatch(typeof(HudManager))]
    [HarmonyPatch("ReInit")]
    class HudManagerReInitPatch
    {
        static void Postfix(HudManager __instance)
        {
            if (__instance.TryGetComponent<KeycardHud>(out KeycardHud comp))
            {
                comp.ReInit();
            }
        }
    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class KeycardLockdownDoor : LockdownDoor
    {
        public int myValue;

        public Color myColor;

        public bool openForever = false;

        public override void Open(bool cancelTimer, bool makeNoise)
        {
            if (openForever) return;
            base.Open(cancelTimer, makeNoise);
        }

        public void OpenForever()
        {
            Open(true, false);
            openForever = true;
        }

        public override void Initialize()
        {
            base.Initialize();
            aMapTile.SpriteRenderer.color = myColor;
            aMapTile.SpriteRenderer.sprite = mapLockedSprite;
            bMapTile.SpriteRenderer.color = myColor;
            bMapTile.SpriteRenderer.sprite = mapLockedSprite;
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
        public List<NPC> holdTheDoorFor = new List<NPC>();
        bool holdingDoor = false;

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
                doors[i].Shut();
            }
        }

        // all this logic just so Mrs. Pomp doesn't seriously struggle with the prison style.
        readonly static FieldInfo _currentTargetTile = AccessTools.Field(typeof(Navigator), "currentTargetTile");
        void Update()
        {
            if (room == null) return;
            for (int i = 0; i < room.ec.Npcs.Count; i++)
            {
                if (room.ec.Npcs[i] is NoLateTeacher) //switch this for a tags check?
                {
                    Cell targetCell = (Cell)_currentTargetTile.GetValue(room.ec.Npcs[i].Navigator);
                    if (targetCell == null) continue;
                    Cell npcCell = room.ec.CellFromPosition(room.ec.Npcs[i].transform.position);
                    // if the npc wants to enter this room and isn't already in this room, open the door for them
                    // if the npc wants to leave the room and is in this room, open the door for them
                    // otherwise, don't.

                    if (((NoLateTeacher)room.ec.Npcs[i]).TargetClassRoom == room && npcCell.room != room)
                    {
                        holdTheDoorFor.Add(room.ec.Npcs[i]);
                    }
                    else
                    {
                        holdTheDoorFor.Remove(room.ec.Npcs[i]);
                    }
                    /*
                    if ((targetCell.room == room) && (npcCell.room != room))
                    {
                        if (!holdTheDoorFor.Contains(room.ec.Npcs[i]))
                        {
                            holdTheDoorFor.Add(room.ec.Npcs[i]);
                        }
                    }
                    else if ((targetCell.room != room) && npcCell.room == room)
                    {
                        if (!holdTheDoorFor.Contains(room.ec.Npcs[i]))
                        {
                            holdTheDoorFor.Add(room.ec.Npcs[i]);
                        }
                    }
                    else if (holdTheDoorFor.Contains(room.ec.Npcs[i]))
                    {
                        holdTheDoorFor.Remove(room.ec.Npcs[i]);
                    }*/
                }
            }

            // if we are already holding the door and no one wants it held
            // close it
            // otherwise, if there are people wanting the door to be opened
            // open it
            if (holdingDoor && holdTheDoorFor.Count == 0)
            {
                holdingDoor = false;
                for (int i = 0; i < doors.Count; i++)
                {
                    doors[i].Shut();
                }
            }
            else if (holdTheDoorFor.Count > 0)
            {
                holdingDoor = true;
                for (int i = 0; i < doors.Count; i++)
                {
                    doors[i].Open(true, false);
                }
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

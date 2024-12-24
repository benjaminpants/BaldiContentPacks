using HarmonyLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class ITM_Crowbar : Item
    {
        static FieldInfo _aMapTile = AccessTools.Field(typeof(SwingDoor), "aMapTile");
        static FieldInfo _bMapTile = AccessTools.Field(typeof(SwingDoor), "bMapTile");


        public SwingDoor doorPrefab;
        public SoundObject useSound;
        public SoundObject doorPrySound;

        void OverrideDoor(SwingDoor toReplace, PlayerManager pm)
        {
            List<RoomController> controllersWithDoor = new List<RoomController>();
            pm.ec.rooms.ForEach(x =>
            {
                x.doors.Remove(toReplace); // remove this door from the doors list
                controllersWithDoor.Add(x);
            });
            SwingDoor swingClone = GameObject.Instantiate<SwingDoor>(doorPrefab, toReplace.transform.parent);
            swingClone.position = toReplace.position;
            swingClone.bOffset = toReplace.bOffset;
            swingClone.direction = toReplace.direction;
            swingClone.ec = toReplace.ec;
            swingClone.transform.position = toReplace.transform.position;
            swingClone.transform.rotation = toReplace.transform.rotation;
            swingClone.bTile.Block(swingClone.direction, false);
            swingClone.bTile.Block(swingClone.direction.GetOpposite(), false);
            swingClone.Open(false, true);
            for (int i = 0; i < controllersWithDoor.Count; i++)
            {
                controllersWithDoor[i].doors.Add(swingClone);
            }
            Destroy(((MapTile)_aMapTile.GetValue(toReplace)).gameObject);
            Destroy(((MapTile)_bMapTile.GetValue(toReplace)).gameObject);
            Destroy(toReplace.gameObject);
            swingClone.audMan.PlaySingle(useSound);
        }

        public override bool Use(PlayerManager pm)
        {
            if (Physics.Raycast(pm.transform.position, Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward, out hit, pm.pc.reach, pm.pc.ClickLayers))
            {
                if (hit.transform.GetComponent<CoinDoor>())
                {
                    GameObject.Destroy(hit.transform.GetComponent<CoinDoor>());
                    OverrideDoor(hit.transform.GetComponent<SwingDoor>(), pm);
                    UnityEngine.Object.Destroy(base.gameObject);
                    return true;
                }
                if (hit.transform.GetComponent<LockdownDoor>())
                {
                    LockdownDoor door = hit.transform.GetComponent<LockdownDoor>();
                    Singleton<CoreGameManager>.Instance.audMan.PlaySingle(doorPrySound);
                    door.InstantOpen();
                    if (!door.open)
                    {
                        door.Shut();
                    }
                    UnityEngine.Object.Destroy(base.gameObject);
                    return true;
                }
                SwingDoor component = this.hit.transform.GetComponent<SwingDoor>();
                if (component != null)
                {
                    UnityEngine.Debug.Log(component.GetType().Name);
                    switch (component.GetType().Name)
                    {
                        case "Door_SwingingOneWay":
                            OverrideDoor(component, pm);
                            UnityEngine.Object.Destroy(base.gameObject);
                            return true;
                        default:
                            UnityEngine.Object.Destroy(base.gameObject);
                            return false;
                    }
  
                }
            }
            UnityEngine.Object.Destroy(base.gameObject);
            return false;
        }

        private RaycastHit hit;
    }
}

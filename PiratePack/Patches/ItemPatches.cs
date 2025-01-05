using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PiratePack.Patches
{

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("UpdateSelect")]
    class ItemManagerSelectPatch
    {
        static void Postfix(ItemManager __instance)
        {
            ShieldTracker myShieldTracker = __instance.GetComponent<ShieldTracker>();
            if (myShieldTracker == null) return;
            if (__instance.items[__instance.selectedItem].itemType == PiratePlugin.shieldItemType)
            {
                myShieldTracker.EquipShield(__instance.selectedItem);
            }
            else
            {
                myShieldTracker.UnequipShield();
            }
        }
    }

    public class ShieldTracker : MonoBehaviour
    {
        public ItemManager itm;
        int currentShieldSlot = -1;
        public ShieldManager currentInstance;
        static readonly FieldInfo _disabled = AccessTools.Field(typeof(ItemManager), "disabled");

        // time to build our shield
        void Awake()
        {
            itm = GetComponent<ItemManager>();
        }

        void Update()
        {
            if (currentInstance == null) return;
            currentInstance.gameObject.SetActive(!(bool)_disabled.GetValue(itm) && Singleton<CoreGameManager>.Instance.GetCamera(itm.pm.playerNumber).Controllable && itm.pm.plm.Entity.InBounds);
        }

        public void EquipShield(int itemId)
        {
            if (itemId != currentShieldSlot)
            {
                if (currentInstance != null)
                {
                    Destroy(currentInstance.gameObject);
                    currentInstance = null;
                }
            }
            currentShieldSlot = itemId;
            if (currentInstance == null)
            {
                currentInstance = Instantiate(PiratePlugin.Instance.assetMan.Get<ShieldManager>("ShieldManager"), transform.parent);
                currentInstance.transform.eulerAngles = itm.pm.transform.eulerAngles + new Vector3(0f, 180f, 0f);
                currentInstance.pm = itm.pm;
                currentInstance.gameObject.SetActive(!(bool)_disabled.GetValue(itm));
                currentInstance.myTracker = this;
            }
            currentInstance.itemSlot = currentShieldSlot;
        }

        public void UnequipShield()
        {
            if (currentShieldSlot == -1) return;
            currentShieldSlot = -1;
            if (currentInstance == null) return;
            Destroy(currentInstance.gameObject);
            currentInstance = null;
        }
    }

    [HarmonyPatch(typeof(ItemManager))]
    [HarmonyPatch("Awake")]
    class ItemManagerAwake
    {
        static void Postfix(ItemManager __instance)
        {
            __instance.gameObject.AddComponent<ShieldTracker>();
        }
    }
}

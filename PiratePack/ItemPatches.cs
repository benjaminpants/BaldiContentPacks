using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PiratePack
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
            currentInstance.gameObject.SetActive(!(bool)_disabled.GetValue(itm));
        }

        public void EquipShield(int itemId)
        {
            currentShieldSlot = itemId;
            if (currentInstance == null)
            {
                currentInstance = GameObject.Instantiate<ShieldManager>(PiratePlugin.Instance.assetMan.Get<ShieldManager>("ShieldManager"), transform.parent);
                currentInstance.transform.eulerAngles = itm.pm.transform.eulerAngles + new Vector3(0f,180f,0f);
                currentInstance.pm = itm.pm;
                currentInstance.gameObject.SetActive(!(bool)_disabled.GetValue(itm));
            }
            currentInstance.itemSlot = currentShieldSlot;
        }

        public void UnequipShield()
        {
            if (currentShieldSlot == -1) return;
            currentShieldSlot = -1;
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

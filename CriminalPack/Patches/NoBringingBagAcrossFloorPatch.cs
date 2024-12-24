using System;
using System.Collections.Generic;
using HarmonyLib;

namespace CriminalPack.Patches
{
    // prevents the player from carrying items across floors to prevent Dealer's wrath
    [HarmonyPatch(typeof(CoreGameManager))]
    [HarmonyPatch("BackupPlayers")]
    class NoBringingBagAcrossFloorPatch
    {
        static void Prefix(int ___setPlayers, PlayerManager[] ___players)
        {
            for (int i = 0; i < ___setPlayers; i++)
            {
                for (int j = 0; j < ___players[i].itm.items.Length; j++)
                {
                    if (___players[i].itm.items[j] == CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("Pouch"))
                    {
                        ___players[i].itm.items[j] = CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("PouchEmpty");
                    }
                }
            }
        }
    }

    // prevents the player from carrying items across floors to prevent Dealer's wrath
    [HarmonyPatch(typeof(StorageLocker))]
    [HarmonyPatch("UpdateContents")]
    class NoBringingBagAcrossFloorViaLockerPatch
    {
        static void Postfix(Pickup[] ___pickup)
        {
            for (int i = 0; i < ___pickup.Length; i++)
            {
                if (___pickup[i].item == CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("Pouch"))
                {
                    Singleton<CoreGameManager>.Instance.currentLockerItems[i] = CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("PouchEmpty");
                }
            }
        }
    }
}

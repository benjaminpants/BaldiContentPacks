using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CriminalPack.Patches
{
    [HarmonyPatch(typeof(StickerScreenController))]
    [HarmonyPatch("PickUpSticker")]
    class PickUpStickerPatch
    {
        static MethodInfo _DestroyStickers = AccessTools.Method(typeof(StickerScreenController), "DestroyStickers");
        static MethodInfo _InitializeStickers = AccessTools.Method(typeof(StickerScreenController), "InitializeStickers");
        static bool Prefix(StickerScreenController __instance, int inventoryId, ref bool __result)
        {
            if (!Singleton<StickerManager>.Instance.stickerInventory[inventoryId].opened) return true;
            if (Singleton<StickerManager>.Instance.stickerInventory[inventoryId].sticker != CriminalPackPlugin.IOUStickerEnum) return true;
            if (((IOUStickerState)Singleton<StickerManager>.Instance.stickerInventory[inventoryId]).disguisingAs == null) return true;
            __result = false;
            Singleton<MusicManager>.Instance.PlaySoundEffect(CriminalPackPlugin.Instance.assetMan.Get<SoundObject>("DecoyBoom"));
            ((IOUStickerState)Singleton<StickerManager>.Instance.stickerInventory[inventoryId]).disguisingAs = null;
            _DestroyStickers.Invoke(__instance, null);
            _InitializeStickers.Invoke(__instance, null);
            return false;
        }
    }
}

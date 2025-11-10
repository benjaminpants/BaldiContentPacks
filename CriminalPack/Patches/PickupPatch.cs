using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace CriminalPack.Patches
{
    [HarmonyPatch(typeof(Pickup))]
    [HarmonyPatch("Start")]
    class PickupStartPatch
    {
        static void Postfix(Pickup __instance)
        {
            if (__instance.item.itemType != CriminalPackPlugin.IOUDecoyEnum) return;
            SceneObject objct = Singleton<CoreGameManager>.Instance.sceneObject;
            List<ItemObject> objectsSorted;
            int possibleToSelect = 3;
            System.Random rng;
            if (objct.levelObject || (objct.randomizedLevelObject != null && objct.randomizedLevelObject.Length > 0))
            {
                LevelObject levelObj = objct.GetCurrentCustomLevelObject();
                objectsSorted = levelObj.potentialItems.Select(x => x.selection).ToList();
                rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed() + (int)(__instance.transform.position.x * 100) + (int)(__instance.transform.position.z * 100));
                rng.Next();
            }
            else
            {
                possibleToSelect = 5;
                EnvironmentController ec = Singleton<BaseGameManager>.Instance.Ec;
                objectsSorted = ec.items.Select(x => x.item).Where(x => (x.itemType != CriminalPackPlugin.IOUDecoyEnum) && (x.itemType != CriminalPackPlugin.IOUEnum) && (x.itemType != CriminalPackPlugin.pouchEnum) && (!EnumExtensions.GetExtendedName<Items>((int)x.itemType).StartsWith("Keycard"))).Distinct().ToList();
                if (objectsSorted.Count == 0)
                {
                    objectsSorted = ItemMetaStorage.Instance.All().Select(x => x.value).Where(x => (x.itemType != CriminalPackPlugin.IOUDecoyEnum) && (x.itemType != CriminalPackPlugin.IOUEnum) && (x.itemType != CriminalPackPlugin.pouchEnum) && (!EnumExtensions.GetExtendedName<Items>((int)x.itemType).StartsWith("Keycard"))).ToList();
                }
                rng = new System.Random(99 + (int)(__instance.transform.position.x * 100) + (int)(__instance.transform.position.z * 100));
                rng.Next();
            }
            objectsSorted.Sort((a, b) => b.value.CompareTo(a.value));
            __instance.itemSprite.sprite = objectsSorted[rng.Next(0, UnityEngine.Mathf.Min(possibleToSelect, objectsSorted.Count))].itemSpriteLarge;
        }
    }

    [HarmonyPatch(typeof(Pickup))]
    [HarmonyPatch("Clicked")]
    class PickupClickedPatch
    {
        static bool Prefix(Pickup __instance, int player, bool ___free)
        {
            if (!___free) return false; // stop shops from triggering IOU stuff (for now, in the future try to find a way to make it work for the lols)
            if (__instance.GetComponent<DisableIOUSpamClick>()) return false;
            if (__instance.GetComponent<RandomIOUChanceFailed>()) return false;
            if (__instance.item.itemType != CriminalPackPlugin.IOUDecoyEnum)
            {
                if (__instance.item.GetMeta().flags.HasFlag(ItemFlags.Unobtainable)) return true; // unobtainable items shouldn't be IOUed
                if (__instance.item.GetMeta().tags.Contains("crmp_iou_sticker_nooverride")) return true; // items with this tag shouldn't be overwritten
                float chanceToBeIOUAnyway = Singleton<StickerManager>.Instance.StickerValue(CriminalPackPlugin.IOUStickerEnum) * 0.05f;
                // we are checking if the chance failed, hence the use of > instead of <=
                if ((chanceToBeIOUAnyway == 0f) || (UnityEngine.Random.Range(0f, 1f) > chanceToBeIOUAnyway))
                {
                    __instance.gameObject.AddComponent<RandomIOUChanceFailed>(); // technically if a pickup isn't destroyed this could stop it from being IOUed but. like. who cares at that point.
                    return true;
                }
            }
            __instance.item = CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("IOU");
            __instance.gameObject.AddComponent<DisableIOUSpamClick>();
            __instance.GetComponentInChildren<PickupBob>().enabled = false;
            PropagatedAudioManager audMan = __instance.gameObject.AddComponent<PropagatedAudioManager>();
            audMan.PlaySingle(CriminalPackPlugin.Instance.assetMan.Get<SoundObject>("DecoyInflate"));
            PlayerManager pm = Singleton<CoreGameManager>.Instance.GetPlayer(player);
            __instance.StartCoroutine(BurstAndExplode(__instance, pm.ec, pm, audMan));
            return false;
        }

        const float timeToExplode = 1f;
        static IEnumerator BurstAndExplode(Pickup instance, EnvironmentController ec, PlayerManager player, AudioManager audMan) //hehehe lik e penis
        {
            float explosionProgress = 0f;
            while (explosionProgress < timeToExplode)
            {
                explosionProgress += Time.deltaTime * ec.EnvironmentTimeScale;
                instance.itemSprite.transform.localScale += Vector3.one * Time.deltaTime * ec.EnvironmentTimeScale * 0.5f;
                yield return null;
            }
            instance.itemSprite.sprite = CriminalPackPlugin.Instance.assetMan.Get<Sprite>("IOUBOOM");
            audMan.FlushQueue(true);
            audMan.PlaySingle(CriminalPackPlugin.Instance.assetMan.Get<SoundObject>("DecoyBoom"));
            instance.itemSprite.transform.localScale = Vector3.one;
            float growAnimation = 0f;
            Vector3 startingSize = instance.itemSprite.transform.localScale;
            while (growAnimation < 0.2f)
            {
                growAnimation += Time.deltaTime * ec.EnvironmentTimeScale;
                instance.itemSprite.transform.localScale = startingSize + (Vector3.one * Mathf.Sin(growAnimation * 12f));
                yield return null;
            }
            instance.itemSprite.transform.localScale = Vector3.one;
            GameObject.Destroy(instance.GetComponent<DisableIOUSpamClick>());
            instance.GetComponentInChildren<PickupBob>().enabled = true;
            instance.AssignItem(instance.item);
            if (((instance.transform.position - player.transform.position).magnitude < (player.pc.reach * 2f)) && !player.itm.InventoryFull())
            {
                instance.Clicked(player.playerNumber);
            }
            // cleanup!
            while (audMan.AnyAudioIsPlaying)
            {
                yield return null;
            }
            GameObject.Destroy(audMan);
            yield break;
        }

        internal class DisableIOUSpamClick : MonoBehaviour
        {

        }

        internal class RandomIOUChanceFailed : MonoBehaviour
        {

        }
    }
}

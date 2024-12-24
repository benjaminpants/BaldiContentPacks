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
            if (objct.levelObject)
            {
                objectsSorted = objct.levelObject.potentialItems.Select(x => x.selection).ToList();
                rng = new System.Random(Singleton<CoreGameManager>.Instance.Seed() + (int)(__instance.transform.position.x * 10) + (int)(__instance.transform.position.z * 10));
                rng.Next();
            }
            else
            {
                possibleToSelect = 5;
                objectsSorted = ItemMetaStorage.Instance.All().Select(x => x.value).ToList();
                rng = new System.Random(99 + (int)(__instance.transform.position.x * 10) + (int)(__instance.transform.position.z * 10));
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
        static bool Prefix(Pickup __instance, int player)
        {
            if (__instance.GetComponent<DisableIOUSpamClick>()) return false;
            if (__instance.item.itemType != CriminalPackPlugin.IOUDecoyEnum) return true;
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
    }
}

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CarnivalPack
{
    [HarmonyPatch(typeof(Principal))]
    [HarmonyPatch("Scold")]
    class PrincipalPatch
    {
        static bool Prefix(string brokenRule, AudioManager ___audMan)
        {
            if (brokenRule == "FrenzyBalloonNoPop")
            {
                if (___audMan.filesQueued < 4)
                {
                    ___audMan.QueueAudio(CarnivalPackBasePlugin.Instance.assetMan.Get<SoundObject>("PrincipalNotPopBalloon"));
                }
                else
                {
                    Debug.LogWarning("Principal tried to scold for balloon frenzy with 4 files already in queue! Ceasing further queues to prevent clogging!");
                }
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Principal_ChasingNpc))]
    [HarmonyPatch("OnStateTriggerStay")]
    class OnStateTriggerStayPatch
    {
        static void Postfix(Collider other, NPC ___targetedNpc)
        {
            if (other.transform == ___targetedNpc.transform)
            {
                if (___targetedNpc.TryGetComponent<FrenzyCounter>(out FrenzyCounter frenz))
                {
                    frenz.timeRemaining = 15f;
                }
            }
        }
    }

    [HarmonyPatch(typeof(Principal))]
    [HarmonyPatch("SendToDetention")]
    class SendToDetentionPatch
    {
        static void Postfix(PlayerManager ___targetedPlayer)
        {
            if (___targetedPlayer.TryGetComponent<FrenzyCounter>(out FrenzyCounter frenz))
            {
                frenz.timeRemaining = Mathf.Max(frenz.timeRemaining, 15f);
            }
        }
    }
}

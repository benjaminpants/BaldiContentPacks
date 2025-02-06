using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace CriminalPack.Patches
{
    [HarmonyPatch(typeof(Principal))]
    [HarmonyPatch("Scold")]
    class PrincipalPatch
    {
        static bool Prefix(string brokenRule, AudioManager ___audMan)
        {
            if (brokenRule == "Fraud")
            {
                ___audMan.QueueAudio(CriminalPackPlugin.Instance.assetMan.Get<SoundObject>("PrincipalFraud"));
                return false;
            }
            if (brokenRule == "Contraband")
            {
                ___audMan.QueueAudio(CriminalPackPlugin.Instance.assetMan.Get<SoundObject>("PrincipalContraband"));
                return false;
            }
            return true;
        }
    }
}

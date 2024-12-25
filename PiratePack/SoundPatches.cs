using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace PiratePack
{
    // todo: this method has some annoying issues caused by things that disable subtitles.
    // fix me!
    [HarmonyPatch(typeof(AudioManager))]
    [HarmonyPatch("CreateSubtitle")]
    class SoundPatches
    {
        static void Postfix(AudioManager __instance, SoundObject soundObject)
        {
            if (!soundObject.subtitle) return; // if this doesn't create a subtitle its probably not very important... todo: verify?
            if (__instance.loop) return; // dont want to play looping sounds (todo: make cann handle this)
            Cann.InvokeSoundPlayed(soundObject, __instance);
        }
    }
}

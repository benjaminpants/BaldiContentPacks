using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{

    public class ITM_Mask : Item
    {
        public Canvas maskCanvas;

        List<Principal> aggroedPrincipals = new List<Principal>();

        string oldRuleBreak = "";
        float oldRuleTime = 0f;

        static FieldInfo _player = AccessTools.Field(typeof(Principal_ChasingPlayer), "player");

        static FieldInfo _guiltTime = AccessTools.Field(typeof(PlayerManager), "guiltTime");

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            maskCanvas.gameObject.SetActive(true);
            maskCanvas.worldCamera = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).canvasCam;
            oldRuleBreak = pm.ruleBreak;
            oldRuleTime = (float)_guiltTime.GetValue(pm);
            pm.ec.Npcs.ForEach(npc =>
            {
                if (npc.Character != Character.Principal) return;
                if (npc.looker.PlayerInSight(pm)) return; // don't apply to principals that can SEE YOU PUT ON THE MASK
                if (npc.behaviorStateMachine.currentState is Principal_ChasingPlayer)
                {
                    if ((PlayerManager)_player.GetValue((Principal_ChasingPlayer)npc.behaviorStateMachine.currentState) != pm) return;
                    aggroedPrincipals.Add((Principal)npc);
                }
            });
            pm.ClearGuilt();
            aggroedPrincipals.ForEach(pri =>
            {
                pri.LoseTrackOfPlayer(pm);
                pri.behaviorStateMachine.ChangeState(new Principal_Wandering(pri));
            });
            StartCoroutine(Timer(15f));
            return true;
        }

        public IEnumerator Timer(float time)
        {
            while (time > 0f)
            {
                time -= Time.deltaTime * pm.PlayerTimeScale;
                if (oldRuleBreak != "")
                {
                    oldRuleTime -= Time.deltaTime * pm.PlayerTimeScale;
                }
                yield return null;
            }
            pm.ClearGuilt();
            if (oldRuleTime > 0)
            {
                pm.RuleBreak(oldRuleBreak, oldRuleTime);
            }
            pm.ec.Npcs.ForEach(npc =>
            {
                if (npc.Character != Character.Principal) return;
                if (npc.behaviorStateMachine.currentState is Principal_ChasingPlayer)
                {
                    if ((PlayerManager)_player.GetValue((Principal_ChasingPlayer)npc.behaviorStateMachine.currentState) != pm) return;
                }
                ((Principal)npc).LoseTrackOfPlayer(pm);
                if (aggroedPrincipals.Contains((Principal)npc))
                {
                    npc.behaviorStateMachine.ChangeState(new Principal_ChasingPlayerScold((Principal)npc, pm, oldRuleBreak));
                    npc.behaviorStateMachine.currentState.DestinationEmpty(); //so the principal doesn't magically know where the player is
                }
                else
                {
                    npc.behaviorStateMachine.ChangeState(new Principal_Wandering((Principal)npc));
                }
            });
            Destroy(maskCanvas.gameObject);
            Destroy(gameObject);
        }
    }
}

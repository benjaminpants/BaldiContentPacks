using HarmonyLib;
using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace CriminalPack
{
    public class Dealer_Wander_Waiting_NotYet : Dealer_Wander
    {
        public float timeBeforeGrapple;
        public Dealer_Wander_Waiting_NotYet(NPC npc, float timeBeforeGrapple) : base(npc)
        {
            this.timeBeforeGrapple = timeBeforeGrapple;
        }
        public override void Enter()
        {
            base.Enter();
            requestTime = timeBeforeGrapple;
        }

        public override void AttemptGrapple(PlayerManager player)
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_Grapple_Inquire_Warning(dealer, player));
        }
    }

    public class Dealer_Wander_Waiting_Bad : Dealer_Wander_Waiting_NotYet
    {
        public Dealer_Wander_Waiting_Bad(NPC npc, float timeBeforeGrapple) : base(npc, timeBeforeGrapple)
        {
        }

        public override void AttemptGrapple(PlayerManager player)
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_Grapple_Inquire_Bad(dealer, player));
        }
    }

    public class Dealer_InformReturnIfAborted : Dealer_Inform
    {
        protected Dealer_Statebase toTransitionToIfFail;
        protected SoundObject onFail;
        public override void PlayerAbort()
        {
            base.PlayerAbort();
            dealer.audMan.PlaySingle(onFail);
            toTransitionTo = toTransitionToIfFail;
        }

        public Dealer_InformReturnIfAborted(NPC npc, PlayerManager player, SoundObject[] toPlay, Dealer_Statebase toTransitionTo, Dealer_Statebase toTransitionToIfFail, SoundObject onFail) : base(npc, player, toPlay, toTransitionTo)
        {
            this.toTransitionToIfFail = toTransitionToIfFail;
            this.onFail = onFail;
        }
    }

    public class Dealer_Grapple_Inquire_Bad : Dealer_Grapple
    {
        public Dealer_Grapple_Inquire_Bad(NPC npc, PlayerManager player) : base(npc, player)
        {
        }

        public override void OnGrappleSucceed()
        {
            dealer.playerCharacterPair = (null, null);
            npc.behaviorStateMachine.ChangeState(new Dealer_InformReturnIfAborted(npc, player, dealer.audInquireBad, new Dealer_ScoundralSteal(npc, player), new Dealer_Wander_Waiting_Bad(npc, 1f), dealer.audInterrupted));
        }

        public override void OnGrappleFail()
        {
            npc.behaviorStateMachine.ChangeState(new Dealer_Wander_Waiting_Bad(npc, dealer.repeatAttemptCooldown));
        }
    }

    public class Dealer_Grapple_Inquire_Warning : Dealer_Grapple
    {
        public Dealer_Grapple_Inquire_Warning(NPC npc, PlayerManager player) : base(npc, player)
        {
        }

        public override void OnGrappleSucceed()
        {
            // if the player somehow doesn't have the pouch or as otherwise hidden it, consider it lost
            // this does technically mean the player could put the pouch away, get in trouble with the dealer, principals whistle, and get an item for free
            // however this is an advanced strategy so i consider it perfectly acceptable.
            if (!player.itm.items.Contains(CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("Pouch")))
            {
                dealer.ClearDelivery();
                npc.behaviorStateMachine.ChangeState(new Dealer_InformReturnIfAborted(npc, player, dealer.audInquireLost, new Dealer_ScoundralSteal(npc, player), new Dealer_Wander_Waiting_Bad(npc, 1f), dealer.audInterrupted));
                return;
            }
            npc.behaviorStateMachine.ChangeState(new Dealer_Inform(npc, player, dealer.audInquireWarning, new Dealer_Wander_Waiting_Bad(npc, dealer.dealerDeliveryWait)));
        }

        public override void OnGrappleFail()
        {
            npc.behaviorStateMachine.ChangeState(new Dealer_Wander_Waiting_NotYet(npc, dealer.repeatAttemptCooldown));
        }
    }

    public class Dealer_Wander_Waiting_Good : Dealer_Wander_Waiting_NotYet
    {
        public Dealer_Wander_Waiting_Good(NPC npc, float timeBeforeGrapple) : base(npc, timeBeforeGrapple)
        {
        }

        int arrowId = -1;

        static FieldInfo _arrows = AccessTools.Field(typeof(Map), "arrows");

        public override void Enter()
        {
            base.Enter();
            npc.ec.map.AddArrow(npc.transform.GetComponent<Entity>(), new UnityEngine.Color(171f / 255f, 91f / 255f, 142f / 255f));
            arrowId = npc.ec.map.arrowTargets.Count - 1;
        }

        public override void Exit()
        {
            base.Exit();
            npc.ec.map.arrowTargets.RemoveAt(arrowId);
            List<MapIcon> arrowList = ((List<MapIcon>)_arrows.GetValue(npc.ec.map));
            UnityEngine.Object.Destroy(arrowList[arrowId].gameObject);
            arrowList.RemoveAt(arrowId);
        }

        public override void AttemptGrapple(PlayerManager player)
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_Grapple_Inquire_Good(dealer, player));
        }
    }

    public class Dealer_Inform_Points : Dealer_InformReturnIfAborted
    {
        bool rewardedPoints = false;

        public Dealer_Inform_Points(NPC npc, PlayerManager player, SoundObject[] toPlay, Dealer_Statebase toTransitionTo) : base(npc, player, toPlay, toTransitionTo, new Dealer_Wander_Waiting_Good(npc, 1f), ((Dealer)npc).audInterrupted)
        {
        }

        public override bool CanPlayerAbort()
        {
            return !rewardedPoints;
        }

        public override void Update()
        {
            base.Update();
            if (rewardedPoints) return;
            if (hasEnded) return;
            if (dealer.audMan.filesQueued == 0)
            {
                rewardedPoints = true;
                Singleton<CoreGameManager>.Instance.AddPoints(dealer.pointsToReward, myPlayer.playerNumber, true);
            }
        }
    }

    public class Dealer_Grapple_Inquire_Good : Dealer_Grapple
    {
        public Dealer_Grapple_Inquire_Good(NPC npc, PlayerManager player) : base(npc, player)
        {
        }

        public override void OnGrappleSucceed()
        {
            dealer.ClearDelivery();
            npc.behaviorStateMachine.ChangeState(new Dealer_Inform_Points(npc, player, dealer.audInquireGood, new Dealer_Wander(npc)));
        }

        public override void OnGrappleFail()
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_Wander_Waiting_Good(dealer, dealer.repeatAttemptCooldown));
        }
    }
}

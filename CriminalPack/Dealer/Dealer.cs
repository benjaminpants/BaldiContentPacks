using HarmonyLib;
using MTM101BaldAPI.Components;
using Rewired;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{

    public struct CharacterChoice
    {
        public SoundObject sound;
        public Character charEnum;
        public Func<NPC, NpcState> createState;

        public CharacterChoice(SoundObject sound, Character chr, Func<NPC, NpcState> createState)
        {
            this.sound = sound;
            this.charEnum = chr;
            this.createState = createState;
        }
    }

    public class WeightedCharacterChoice : WeightedSelection<CharacterChoice>
    {
        
    }

    public class Dealer : NPC
    {

        public static List<WeightedCharacterChoice> characterChoices = new List<WeightedCharacterChoice>();
        public float defaultSpeed = 14f;
        public float fleeSpeed = 22f;
        public float chargeBaseSpeed = 3f;
        public float cooldownTime = 20f;
        public float repeatAttemptCooldown = 2.5f;
        public float requireSeePlayerTime = 0.06f;
        public float dealerDeliveryWait = 30f;
        public int pointsToReward = 150;
        public float grappleDistance = 100f; // limit grapple to 10 tiles.
        public AudioManager audMan;
        public Entity entity;
        public CustomSpriteAnimator animator;
        public List<ItemObject> stolenItems = new List<ItemObject>();
        public SoundObject[] audGottaScram = new SoundObject[0];
        public SoundObject[] audHey = new SoundObject[0];
        public SoundObject[] audInteract = new SoundObject[0];
        public SoundObject[] audInquireWarning = new SoundObject[0];
        public SoundObject[] audInquireGood = new SoundObject[0];
        public SoundObject[] audInquireBad = new SoundObject[0];
        public SoundObject[] audInquireLost = new SoundObject[0];
        public SoundObject audStealNoSpare;
        public SoundObject audSteal;
        public SoundObject audInterrupted;
        public WeightedItemObject[] weightedItems = new WeightedItemObject[0];
        public Items stolenItem = Items.None;
        public (PlayerManager, Character?) playerCharacterPair = (null, null);
        public bool hasActiveDelivery => playerCharacterPair != (null, null);
        public SoundObject audGrappled;

        public override void Despawn()
        {
            behaviorStateMachine.currentState.Exit();
            base.Despawn();
        }

        public void SetGuilty()
        {
            base.SetGuilt(10f, "Bullying");
        }

        private IEnumerator TurnPlayer(PlayerManager player, float speed)
        {
            float time = 0.5f;
            Vector3 vector;
            while (time > 0f)
            {
                vector = Vector3.RotateTowards(player.transform.forward, (base.transform.position - player.transform.position).normalized, Time.deltaTime * 2f * Mathf.PI * speed, 0f);
                player.transform.rotation = Quaternion.LookRotation(vector, Vector3.up);
                time -= Time.deltaTime;
                yield return null;
            }
            yield break;
        }

        public void OnBagUsed(PlayerManager pm, Character chr, Items stolenItem = Items.None)
        {
            if (!hasActiveDelivery) return;
            if ((pm, chr) == playerCharacterPair)
            {
                ClearDelivery();
                behaviorStateMachine.ChangeState(new Dealer_Wander_Waiting_Good(this, 1f));
            }
            else if (pm == playerCharacterPair.Item1)
            {
                this.stolenItem = stolenItem;
                behaviorStateMachine.ChangeState(new Dealer_Wander_Waiting_Bad(this, 2f));
            }
        }

        public override void SentToDetention()
        {
            base.SentToDetention();
            ClearGuilt();
            behaviorStateMachine.ChangeState(new Dealer_Detention(this, ec.CellFromPosition(transform.position).room));
            for (int i = 0; i < stolenItems.Count; i++)
            {
                ITM_DealerBag newBag = GameObject.Instantiate<ITM_DealerBag>((ITM_DealerBag)CriminalPackPlugin.Instance.assetMan.Get<ItemObject>("Pouch").item);
                newBag.transform.position = ec.Npcs.Find(x => x.Character == Character.Principal).transform.position; //todo: add code for handling multiple principals
                newBag.transform.rotation = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, 360f), Vector3.up);
                newBag.InitializeDrop(ec, stolenItems[i]);
            }
            stolenItems.Clear();
        }

        public void CausePlayerTurn(PlayerManager pm)
        {
            StartCoroutine(TurnPlayer(pm, 2f));
        }

        public void ClearDelivery()
        {
            playerCharacterPair = (null, null);
            stolenItem = Items.None;
        }

        public override void Initialize()
        {
            base.Initialize();
            entity = GetComponent<Entity>();
            navigator.SetSpeed(defaultSpeed);
            navigator.maxSpeed = defaultSpeed;
            animator.animations.Add("Idle", new CustomAnimation<UnityEngine.Sprite>(1, new Sprite[]
            {
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer2")
            }));
            animator.animations.Add("Grapple", new CustomAnimation<UnityEngine.Sprite>(6, new Sprite[]
            {
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_grapple2"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_grapple"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_grapple3"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_grapple"),
            }));
            animator.animations.Add("Talk", new CustomAnimation<UnityEngine.Sprite>(12, new Sprite[]
            {
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_talk1"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_talk2"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_talk3"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_talk2"),
            }));
            animator.animations.Add("CloakOpen", new CustomAnimation<UnityEngine.Sprite>(new Sprite[]
            {
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_open1"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_open2"),
            }, 0.5f));
            animator.animations.Add("CloakClose", new CustomAnimation<UnityEngine.Sprite>(new Sprite[]
            {
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_open2"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_open1"),
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer"),
            }, 0.5f));
            animator.animations.Add("CloakIdle", new CustomAnimation<UnityEngine.Sprite>(new Sprite[]
            {
                CriminalPackPlugin.Instance.assetMan.Get<Sprite>("dealer_open2"),
            }, 0.25f));
            animator.SetDefaultAnimation("Idle", 1f);
            SetLookerLimitation(false);
            this.behaviorStateMachine.ChangeState(new Dealer_Wander(this));
        }

        public void SetLookerLimitation(bool limited)
        {
            looker.distance = limited ? grappleDistance : (grappleDistance * 1.75f);
        }

        public CharacterChoice SelectRandomCharacter()
        {
            List<Character> validCharacters = ec.npcsToSpawn.Select(x => x.Character).Distinct().ToList();
            return WeightedCharacterChoice.RandomSelection(characterChoices.Where(x => validCharacters.Contains(x.selection.charEnum)).ToArray());
        }
    }

    public class Dealer_Statebase : NpcState
    {
        protected Dealer dealer;
        public Dealer_Statebase(NPC npc) : base(npc)
        {
            dealer = (Dealer)npc;
        }
    }

    public class Dealer_Grapple : Dealer_Statebase
    {

        readonly static FieldInfo _forces = AccessTools.Field(typeof(Entity), "forces");
        float minGrappleTime = 0.25f;
        bool caughtPlayer = false;
        protected PlayerManager player;
        Force myForce;
        MovementModifier myMoveMod = new MovementModifier(Vector3.zero, 0f);
        public Dealer_Grapple(NPC npc, PlayerManager player) : base(npc)
        {
            this.player = player;
        }

        public override void Enter()
        {
            base.Enter();
            dealer.animator.Play("Grapple", 1f);
            dealer.animator.SetDefaultAnimation("Grapple", 1f);
            myForce = new Force((player.transform.position - dealer.transform.position).normalized, 85f, -30f);
            dealer.Navigator.SetSpeed(dealer.chargeBaseSpeed);
            dealer.Navigator.maxSpeed = dealer.chargeBaseSpeed;
            dealer.entity.AddForce(myForce);
            ChangeNavigationState(new NavigationState_TargetPlayer(dealer, 126, player.transform.position));
        }

        public override void Update()
        {
            base.Update();
            minGrappleTime -= Time.deltaTime * dealer.ec.NpcTimeScale;
            if (dealer.entity.ExternalActivity.Multiplier == 0f) //if we get hit by bsoda or are otherwise entirely stopped, fail the grapple instantly so we dont CHEAT
            {
                OnGrappleFail();
                return;
            }
            if (minGrappleTime > 0f) return;
            if (!(((List<Force>)_forces.GetValue(dealer.entity)).Contains(myForce)))
            {
                if ((!caughtPlayer))
                {
                    OnGrappleFail();
                }
                else
                {
                    OnGrappleSucceed();
                }
            }
        }

        public virtual void OnGrappleSucceed()
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_InformItem(dealer, player, dealer.audInteract, dealer.audInterrupted, new Dealer_Wander_Waiting_NotYet(dealer, dealer.dealerDeliveryWait)));
        }

        public virtual void OnGrappleFail()
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_Wander(dealer, dealer.repeatAttemptCooldown));
        }

        public override void OnStateTriggerEnter(Collider other)
        {
            base.OnStateTriggerEnter(other);
            if (caughtPlayer) return;
            if (other.GetComponent<PlayerManager>() != player) return;
            if (!player.Tagged)
            {
                player.GetComponent<PlayerEntity>().AddForce(myForce); //this will cause the force to de-accelerate at twice the speed it normally would, this can be considered "intended" behavior for now.
                player.Am.moveMods.Add(myMoveMod);
                caughtPlayer = true;
                dealer.CausePlayerTurn(player);
                dealer.audMan.PlaySingle(dealer.audGrappled);
            }
        }

        public override void Exit()
        {
            base.Exit();
            player.GetComponent<PlayerEntity>().RemoveForce(myForce);
            player.Am.moveMods.Remove(myMoveMod);
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_TargetPlayer(dealer, 126, player.transform.position));
        }
    }

    public class Dealer_Inform : Dealer_Statebase
    {
        protected SoundObject[] toPlay;
        protected Dealer_Statebase toTransitionTo;
        MovementModifier myModifier = new MovementModifier(Vector3.zero, 0.35f);
        protected PlayerManager myPlayer;
        protected virtual string introAnimation => "Talk";
        protected virtual string talkAnimation => "Talk";
        protected virtual string endAnimation => "Idle";
        protected virtual float endDelay => 0.1f;
        float delayCountdown = 0f;
        protected bool hasEnded = false;
        public Dealer_Inform(NPC npc, PlayerManager player, SoundObject[] toPlay, Dealer_Statebase toTransitionTo) : base(npc)
        {
            this.toPlay = toPlay;
            this.toTransitionTo = toTransitionTo;
            dealer.animator.Play(introAnimation, 1f);
            dealer.animator.SetDefaultAnimation(talkAnimation, 1f);
            myPlayer = player;
        }

        // called when the player leaves the sight/range while he is talking.
        public virtual void PlayerAbort()
        {

        }

        public virtual bool CanPlayerAbort()
        {
            return true;
        }

        public override void Enter()
        {
            base.Enter();
            dealer.SetLookerLimitation(true);
            ChangeNavigationState(new NavigationState_DoNothing(dealer, 127));
            dealer.audMan.FlushQueue(true);
            myPlayer.Am.moveMods.Add(myModifier);
            // technically this would malfunction if there was more than one player.
            if (!dealer.looker.PlayerInSight())
            {
                PlayerLost(myPlayer);
                return;
            }
            for (int i = 0; i < toPlay.Length; i++)
            {
                dealer.audMan.QueueAudio(toPlay[i]);
            }
        }

        public override void PlayerLost(PlayerManager player)
        {
            base.PlayerLost(player);
            if (hasEnded) return;
            if (player == myPlayer)
            {
                if (CanPlayerAbort())
                {
                    End();
                    PlayerAbort();
                }
            }
        }

        public override void Update()
        {
            base.Update();
            if (hasEnded)
            {
                delayCountdown -= Time.deltaTime * dealer.ec.NpcTimeScale;
                if (delayCountdown <= 0f)
                {
                    dealer.behaviorStateMachine.ChangeState(toTransitionTo);
                    return;
                }
                return;
            }
            if (dealer.audMan.AnyAudioIsPlaying) return;
            End();
        }

        protected void End()
        {
            dealer.SetLookerLimitation(false);
            dealer.audMan.FlushQueue(true);
            dealer.animator.SetDefaultAnimation("", 1f);
            dealer.animator.Play(endAnimation, 1f);
            delayCountdown = endDelay;
            hasEnded = true;
        }

        public override void Exit()
        {
            base.Exit();
            myPlayer.Am.moveMods.Remove(myModifier);
        }
    }

    public class Dealer_InformItem : Dealer_Inform
    {

        protected override string introAnimation => "CloakOpen";
        protected override string talkAnimation => "CloakIdle";
        protected override string endAnimation => "CloakClose";
        protected override float endDelay => 0.5f;
        SoundObject onFail;
        public Dealer_InformItem(NPC npc, PlayerManager player, SoundObject[] toPlay, SoundObject onFail, Dealer_Statebase toTransitionTo) : base(npc, player, toPlay, toTransitionTo)
        {
            this.onFail = onFail;
        }

        bool hasGivenItem = false;

        Character chosenCharacter = Character.Null;

        public override void Enter()
        {
            CharacterChoice choice = dealer.SelectRandomCharacter();
            // using audGrappled as a placeholder
            toPlay = toPlay.ToArray(); //makes a clone of the array even though i swear arrays werent passed by reference
            toPlay[toPlay.ToList().IndexOf(dealer.audGrappled)] = choice.sound;
            chosenCharacter = choice.charEnum;
            base.Enter();
        }


        public override bool CanPlayerAbort()
        {
            return !hasGivenItem;
        }

        public override void PlayerAbort()
        {
            base.PlayerAbort();
            if (!hasGivenItem)
            {
                dealer.audMan.PlaySingle(onFail);
                toTransitionTo = new Dealer_Wander(npc, dealer.repeatAttemptCooldown);
            }
        }

        public override void Update()
        {
            base.Update();
            if (hasGivenItem) return;
            if (!hasEnded)
            {
                if (dealer.audMan.filesQueued == 0)
                {
                    ItemObject selectedItem = WeightedItemObject.RandomSelection(dealer.weightedItems);
                    myPlayer.itm.AddItem(selectedItem);
                    dealer.playerCharacterPair = (myPlayer, chosenCharacter);
                    hasGivenItem = true;
                }
            }
        }
    }
    public class Dealer_ScoundralSteal : Dealer_Statebase
    {
        float timeRemaining = 30f;
        PlayerManager player;

        public Dealer_ScoundralSteal(NPC npc, PlayerManager pm) : base(npc)
        {
            this.player = pm;
        }

        public override void Enter()
        {
            base.Enter();
            dealer.Navigator.SetSpeed(dealer.defaultSpeed * 1.25f);
            dealer.Navigator.maxSpeed = dealer.defaultSpeed * 1.25f;
            dealer.animator.Play("Idle", 1f);
            dealer.animator.SetDefaultAnimation("Idle", 1f);
            dealer.SetGuilty();
            base.ChangeNavigationState(new NavigationState_WanderRandom(dealer, 0));
            if (dealer.stolenItem == Items.None)
            {
                dealer.audMan.PlaySingle(dealer.audStealNoSpare);
                for (int i = 0; i < player.itm.items.Length; i++)
                {
                    if (player.itm.items[i].itemType != Items.None)
                    {
                        dealer.stolenItems.Add(player.itm.items[i]);
                    }
                }
                player.itm.ClearItems();
            }
            else
            {
                dealer.audMan.PlaySingle(dealer.audSteal);
                bool sparedItem = false;
                for (int i = 0; i < player.itm.items.Length; i++)
                {
                    if ((!sparedItem) && player.itm.items[i].itemType == dealer.stolenItem)
                    {
                        sparedItem = true;
                        continue;
                    }
                    if (player.itm.items[i].itemType != Items.None)
                    {
                        dealer.stolenItems.Add(player.itm.items[i]);
                    }
                    player.itm.items[i] = player.itm.nothing;
                }
                player.itm.UpdateItems();
                Singleton<CoreGameManager>.Instance.GetHud(player.playerNumber).SetItemSelect(player.itm.selectedItem, player.itm.items[player.itm.selectedItem].nameKey);
            }
        }

        static FieldInfo _targetedNpc = AccessTools.Field(typeof(Principal_ChasingNpc), "targetedNpc");

        public override void Update()
        {
            base.Update();
            timeRemaining -= Time.deltaTime * npc.ec.NpcTimeScale;
            if (timeRemaining < 0f)
            {
                npc.behaviorStateMachine.ChangeState(new Dealer_Wander(npc));
                return;
            }
            if (dealer.Disobeying)
            {
                NPC[] principals = npc.ec.Npcs.Where(x => x.Character == Character.Principal).ToArray();
                for (int i = 0; i < principals.Length; i++)
                {
                    if (principals[i].behaviorStateMachine.currentState.GetType() == typeof(Principal_ChasingNpc))
                    {
                        if ((NPC)_targetedNpc.GetValue(principals[i].behaviorStateMachine.currentState) == this.npc)
                        {
                            npc.behaviorStateMachine.ChangeState(new Dealer_FleePrincipal(npc, principals[i].GetComponent<Principal>()));
                            return;
                        }
                    }
                }
            }
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            base.ChangeNavigationState(new NavigationState_WanderRandom(dealer, 0));
        }
    }

    public class Dealer_Detention : Dealer_Statebase
    {
        RoomController detention;
        float detentionTimeLeft = 15f;
        NavigationState_PartyEvent currentNavState;
        public Dealer_Detention(NPC npc, RoomController detention) : base(npc)
        {
            this.detention = detention;
            dealer.Navigator.SetSpeed(dealer.defaultSpeed / 2f);
            dealer.Navigator.maxSpeed = dealer.defaultSpeed / 2f;
            dealer.animator.Play("Idle", 1f);
            dealer.animator.SetDefaultAnimation("Idle", 1f);
            currentNavState = new NavigationState_PartyEvent(npc, 33, detention);
        }

        public override void Enter()
        {
            base.Enter();
            ChangeNavigationState(currentNavState);
        }

        public override void Update()
        {
            base.Update();
            detentionTimeLeft -= Time.deltaTime * npc.ec.EnvironmentTimeScale; //detention time is based off of enviroment time scale for the player so we will be doing the same here
            if (detentionTimeLeft <= 0f)
            {
                npc.behaviorStateMachine.ChangeState(new Dealer_Wander(npc));
            }
        }

        public override void Exit()
        {
            base.Exit();
            if (currentNavState == null) return;
            currentNavState.priority = 0; //so we actually can leave
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(currentNavState = new NavigationState_PartyEvent(npc, 33, detention));
        }
    }

    public class Dealer_FleePrincipal : Dealer_Statebase
    {
        Principal toFlee;
        float timeToForgetPrincipal = 10f;
        DijkstraMap principalFleeMap;
        public Dealer_FleePrincipal(NPC npc, Principal toFlee) : base(npc)
        {
            this.toFlee = toFlee;
            principalFleeMap = new DijkstraMap(npc.ec, PathType.Nav, toFlee.transform);
        }

        public override void Enter()
        {
            base.Enter();
            dealer.audMan.FlushQueue(true);
            principalFleeMap.Activate();
            ChangeNavigationState(new NavigationState_WanderFlee(npc, 32, principalFleeMap));
            dealer.Navigator.SetSpeed(dealer.fleeSpeed);
            dealer.Navigator.maxSpeed = dealer.fleeSpeed;
            dealer.animator.Play("Idle", 1f);
            dealer.animator.SetDefaultAnimation("Idle", 1f);
            dealer.audMan.PlayRandomAudio(dealer.audGottaScram);
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            ChangeNavigationState(new NavigationState_WanderFlee(npc, 32, principalFleeMap));
        }

        static FieldInfo _targetedNpc = AccessTools.Field(typeof(Principal_ChasingNpc), "targetedNpc");

        public override void Update()
        {
            base.Update();
            if ((toFlee == null) || (timeToForgetPrincipal < 0f))
            {
                StopFleeing();
                return;
            }
            NPC[] principals = npc.ec.Npcs.Where(x => x.Character == Character.Principal).ToArray();
            for (int i = 0; i < principals.Length; i++)
            {
                if (principals[i].behaviorStateMachine.currentState.GetType() == typeof(Principal_ChasingNpc))
                {
                    if ((NPC)_targetedNpc.GetValue(principals[i].behaviorStateMachine.currentState) == this.npc)
                    {
                        return;
                    }
                }
            }
            timeToForgetPrincipal -= Time.deltaTime * npc.ec.NpcTimeScale;
        }

        public void StopFleeing()
        {
            npc.behaviorStateMachine.ChangeState(new Dealer_Wander(npc));
        }

        public override void Exit()
        {
            base.Exit();
        }
    }

    public class Dealer_Wander : Dealer_Statebase
    {
        public float requestTime;
        protected float playerSeeDelay = 1f;

        public Dealer_Wander(NPC npc) : base(npc)
        {
            requestTime = dealer.cooldownTime;
        }

        public Dealer_Wander(NPC npc, float cooldownTimeOverride) : base(npc)
        {
            requestTime = cooldownTimeOverride;
        }

        public override void Enter()
        {
            base.Enter();
            playerSeeDelay = dealer.requireSeePlayerTime;
            dealer.Navigator.SetSpeed(dealer.defaultSpeed);
            dealer.Navigator.maxSpeed = dealer.defaultSpeed;
            dealer.animator.Play("Idle", 1f);
            dealer.animator.SetDefaultAnimation("Idle", 1f);
            base.ChangeNavigationState(new NavigationState_WanderRandom(dealer, 0));
        }

        public override void Update()
        {
            base.Update();
            requestTime -= Time.deltaTime * dealer.ec.NpcTimeScale;
        }

        public override void PlayerInSight(PlayerManager player)
        {
            if (player.Tagged) return;
            base.PlayerInSight(player);
            if (requestTime > 0f) return;
            playerSeeDelay -= Time.deltaTime * dealer.ec.NpcTimeScale;
            if (playerSeeDelay > 0f) return;
            if ((player.transform.position - dealer.transform.position).magnitude <= dealer.grappleDistance)
            {
                dealer.audMan.PlayRandomAudio(dealer.audHey);
                AttemptGrapple(player);
            }
            else
            {
                base.ChangeNavigationState(new NavigationState_TargetPlayer(dealer, 1, player.transform.position));
            }
        }

        public virtual void AttemptGrapple(PlayerManager player)
        {
            dealer.behaviorStateMachine.ChangeState(new Dealer_Grapple(dealer, player));
        }

        public override void PlayerLost(PlayerManager player)
        {
            playerSeeDelay = dealer.requireSeePlayerTime;
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            base.ChangeNavigationState(new NavigationState_WanderRandom(dealer, 0));
        }
    }
}

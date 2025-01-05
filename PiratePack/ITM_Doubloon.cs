using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PiratePack
{
    public class NavigationState_Doubloon : NavigationState_TargetPosition
    {
        ITM_Doubloon doubloon;
        public bool beingIntentionallyRemoved = false;
        public NavigationState_Doubloon(NPC npc, int priority, Vector3 position, ITM_Doubloon doubloon) : base(npc, priority, position, true)
        {
            this.doubloon = doubloon;
        }

        // dont tell the npc the destination is empty
        // if we do, it may fuck things up for the NPC
        // instead, do nothing, logically the coin pickup logic should trigger
        public override void DestinationEmpty()
        {
            
        }

        // how the fuck
        public override void Exit()
        {
            base.Exit();
            if (beingIntentionallyRemoved) return;
            if (doubloon == null) return;
            if (!doubloon.affectedNPCs.ContainsKey(npc)) return;
            doubloon.affectedNPCs.Remove(npc);
        }
    }

    // FEATURING: a lot of borrowed code from the criminal pack DealerBag, lol.
    // i mean hey, it is easier now that its all in one repo
    public class ITM_Doubloon : Item, IEntityTrigger
    {
        private static readonly FieldInfo _rendererBase = AccessTools.Field(typeof(Entity), "rendererBase");

        public Entity entity;
        public SpriteRenderer renderBase;
        public AudioManager audMan;
        const float floorY = -4.25f;
        float yPos = 0f;
        float yVel = 0f;
        float forwardSpeed = 16f;
        public Sprite[] sprites;
        public float currentSprite = 0f;
        public bool hitFloor = false;
        public bool collecting = false;
        public float timeRemainingBeforeDestruction = 30f;
        public DoubloonSparkle sparklePrefab;

        public SoundObject bounceSound;
        public SoundObject collectSound;
        public SoundObject noticeSound;

        public Dictionary<NPC, NavigationState_Doubloon> affectedNPCs = new Dictionary<NPC, NavigationState_Doubloon>();

        static readonly FieldInfo _currentState = AccessTools.Field(typeof(NavigationStateMachine), "currentState");

        public void EntityTriggerEnter(Collider other)
        {
            if (collecting) return;
            NPC npc = other.GetComponent<NPC>();
            if (npc == null)
            {
                if (!hitFloor) return;
                PlayerManager pm = other.GetComponent<PlayerManager>();
                if (pm == null) return;
                Singleton<CoreGameManager>.Instance.AddPoints(25, pm.playerNumber, true);
                CollectCoin();
                return;
            }
            if (affectedNPCs.ContainsKey(npc)) //if we have been touched again by an NPC that we have affected
            {
                CollectCoin();
                return;
            }
            if (hitFloor) return; // don't apply state if we have hit the floor
            NavigationState_Doubloon state = new NavigationState_Doubloon(npc, 128, transform.position, this);
            npc.navigationStateMachine.ChangeState(state);
            // if the state wasn't actually applied, stop here
            if (_currentState.GetValue(npc.navigationStateMachine) != state) return;
            affectedNPCs.Add(npc, state);
            // create sparkle
            DoubloonSparkle spark = GameObject.Instantiate<DoubloonSparkle>(sparklePrefab, transform.parent);
            spark.transform.position = renderBase.transform.position - (transform.forward * 0.25f); // send the sparkle slightly backwards to hopefully make it more visible
            audMan.PlaySingle(noticeSound);
            spark.ec = npc.ec;
        }

        public void EntityTriggerExit(Collider other)
        {
        }

        public void EntityTriggerStay(Collider other)
        {
        }

        public void CollectCoin()
        {
            collecting = true;
            audMan.PlaySingle(collectSound);
            RestoreNavigationForAllAffected();
            entity.SetVisible(false);
            StartCoroutine(DestroyAfterSound());
        }

        public void RestoreNavigationForAllAffected()
        {
            foreach (NPC npc in affectedNPCs.Keys)
            {
                affectedNPCs[npc].beingIntentionallyRemoved = true;
                affectedNPCs[npc].priority = -1; // make sure the state always gets replaced
                npc.behaviorStateMachine.RestoreNavigationState(); // go back to normal
            }
        }

        IEnumerator DestroyAfterSound()
        {
            yield return null;
            while (audMan.AnyAudioIsPlaying)
            {
                yield return null;
            }
            Destroy(gameObject);
        }

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            entity.Initialize(pm.ec, pm.transform.position);
            entity.SetGrounded(false);
            transform.forward = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward;
            renderBase = ((Transform)_rendererBase.GetValue(entity)).GetChild(0).GetComponent<SpriteRenderer>();
            forwardSpeed += pm.plm.RealVelocity * 1.35f;
            entity.OnEntityMoveInitialCollision += OnEntCollision;
            return true;
        }

        void Update()
        {
            entity.UpdateInternalMovement(transform.forward * forwardSpeed);
            forwardSpeed = Mathf.Max(forwardSpeed - (Time.deltaTime * pm.ec.EnvironmentTimeScale), 0f);
            foreach (NavigationState_Doubloon state in affectedNPCs.Values)
            {
                state.UpdatePosition(transform.position);
            }
            if (collecting)
            {
                forwardSpeed = 0f;
            }
            if (hitFloor)
            {
                yVel = 0f;
                yPos = floorY;
                renderBase.transform.localPosition = Vector3.up * yPos;
                renderBase.sprite = sprites[2];
                // perform reduction again so this should be 3x the friction
                forwardSpeed = Mathf.Max(forwardSpeed - ((Time.deltaTime * pm.ec.EnvironmentTimeScale) * 2f), 0f);
                timeRemainingBeforeDestruction -= (Time.deltaTime * pm.ec.EnvironmentTimeScale);
                if (timeRemainingBeforeDestruction <= 0f)
                {
                    RestoreNavigationForAllAffected();
                    Destroy(this.gameObject);
                }
                return;
            }
            yVel -= (Time.deltaTime * pm.ec.EnvironmentTimeScale) / 4f;
            yPos += yVel;
            if (yPos <= floorY)
            {
                yVel *= -0.9f;
                forwardSpeed *= 0.8f;
                audMan.PlaySingle(bounceSound);
                if (yVel <= 0.1f)
                {
                    hitFloor = true;
                    entity.SetGrounded(true);
                }
            }
            renderBase.transform.localPosition = Vector3.up * yPos;
            currentSprite += (forwardSpeed * Time.deltaTime * pm.ec.EnvironmentTimeScale) / 4f;
            renderBase.sprite = sprites[Mathf.FloorToInt(currentSprite) % sprites.Length];
        }

        void OnEntCollision(RaycastHit ray)
        {
            transform.forward = Vector3.Reflect(transform.forward, ray.normal);
            audMan.PlaySingle(bounceSound);
        }
    }
}

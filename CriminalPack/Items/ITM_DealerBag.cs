using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static CriminalPack.ITM_DealerBag;

namespace CriminalPack
{
    public class ITM_DealerBag : Item, IEntityTrigger
    {
        public static List<WeightedItemObject> potentialItems = new List<WeightedItemObject>();

        private static readonly FieldInfo _rendererBase = AccessTools.Field(typeof(Entity), "rendererBase");

        protected ItemObject toDrop;

        public AudioManager audMan;
        public SoundObject[] bounceSounds;
        public SoundObject openSound;
        public SpriteRenderer renderBase;
        public Entity entity;
        public EnvironmentController ec;
        float forwardSpeed = 20f;
        float yVel = 5f;
        float yPos = 0f;
        float rotationVal = 0f;
        const float floorY = -4.25f;
        const float tolerantEpsilon = (float.Epsilon * 4);
        const float openTime = 1.25f;
        int rotateDirection = 1;
        bool popped = false;

        // todo: figure out wtf is going on with this
        public void EntityTriggerEnter(Collider other, bool validCollision)
        {
            if (pm == null) return;
            if (popped) return;
            if (!validCollision) return;
            if (other.TryGetComponent(out NPC npc))
            {
                WeightedCharacterChoice choice = Dealer.characterChoices.Find(x => x.selection.charEnum == npc.Character);
                if (choice != null)
                {
                    npc.behaviorStateMachine.ChangeState(choice.selection.createState(npc));
                    OnBagUsed(pm, npc.Character);
                    Destroy(base.gameObject);
                }
            }
        }

        public void EntityTriggerExit(Collider other, bool validCollision)
        {
            
        }

        public void EntityTriggerStay(Collider other, bool validCollision)
        {
            
        }


        public void InitializeDrop(EnvironmentController ec, ItemObject toDrop = null)
        {
            this.ec = ec;
            if (toDrop == null)
            {
                this.toDrop = WeightedItemObject.RandomSelection(potentialItems.ToArray());
            }
            else
            {
                this.toDrop = toDrop;
            }
            entity.Initialize(ec, base.transform.position);
            entity.SetGrounded(false);
            renderBase = ((Transform)_rendererBase.GetValue(entity)).GetChild(0).GetComponent<SpriteRenderer>();
            rotateDirection = (UnityEngine.Random.Range(0, 2) * 2) - 1;
        }

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            transform.position = pm.transform.position;
            transform.forward = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward;
            InitializeDrop(pm.ec);
            forwardSpeed += pm.plm.RealVelocity;
            return true;
        }

        static FieldInfo _survivePickup = AccessTools.Field(typeof(Pickup), "survivePickup");
        IEnumerator FlyUpAndBecomeItem()
        {
            float time = 0f;
            Vector3 targetPos = Vector3.zero;
            Vector3 startingPos = renderBase.transform.localPosition;
            while (time < openTime)
            {
                time += Time.deltaTime * ec.EnvironmentTimeScale;
                renderBase.transform.localPosition = Vector3.Lerp(startingPos, targetPos, time / openTime);
                yield return null;
            }
            renderBase.transform.localPosition = startingPos;
            if (toDrop.itemType != Items.None)
            {
                // it took me months to find the problem when mystman12 changed CreateItem to use local positions instead of absolute ones.
                // sorry for the delay.
                RoomController rc = ec.CellFromPosition(transform.position).room;
                Pickup pickup = ec.CreateItem(rc, toDrop, transform.position);
                ec.items.Remove(pickup);
                _survivePickup.SetValue(pickup, false);
                pickup.transform.position = transform.position; //whatever
                OnBagUsed(pm, Character.Null, toDrop.itemType);
            }
            yield return DestroyAfterSoundPlay();
            yield break;
        }

        IEnumerator DestroyAfterSoundPlay()
        {
            audMan.PlaySingle(openSound);
            Destroy(renderBase);
            Destroy(entity); //so we stop moving and just stop doing things in general as this object only needs to persist long enough to finish the sound
            while (audMan.AnyAudioIsPlaying) { yield return null; }
            Destroy(base.gameObject);
        }
        

        void OnBagUsed(PlayerManager pm, Character chr, Items stolenItem = Items.None)
        {
            ec.Npcs.ForEach((NPC npc) =>
            {
                if (npc.Character == CriminalPackPlugin.dealerEnum)
                {
                    ((Dealer)npc).OnBagUsed(pm, chr, stolenItem);
                }
            });
        }

        private void Update()
        {
            if (ec == null) return;
            if (popped) return;
            if ((entity.Velocity.magnitude < tolerantEpsilon) && yPos <= (floorY + tolerantEpsilon))
            {
                popped = true;
                renderBase.SetSpriteRotation(0f);
                entity.UpdateInternalMovement(Vector3.zero);
                renderBase.transform.localPosition = Vector3.up * floorY;
                StartCoroutine(FlyUpAndBecomeItem());
                return;
            }
            float delta = Time.deltaTime * ec.EnvironmentTimeScale;
            yVel -= delta * 8f;
            yPos += yVel * delta;
            if (yPos <= floorY)
            {
                yVel *= -0.4f;
                forwardSpeed *= 0.5f;
                yPos = floorY;
                rotationVal = 0f;
                rotateDirection *= -1;
                entity.SetGrounded(true);
                audMan.PlayRandomAudio(bounceSounds);
            }
            else
            {
                entity.SetGrounded(false);
            }
            renderBase.transform.localPosition = Vector3.up * yPos;
            entity.UpdateInternalMovement(transform.forward * forwardSpeed);
            rotationVal += (forwardSpeed * 1.3f) * delta * rotateDirection;
            renderBase.SetSpriteRotation(rotationVal);


        }
    }
}

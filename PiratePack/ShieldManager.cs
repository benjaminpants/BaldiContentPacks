using MTM101BaldAPI.Reflection;
using MTM101BaldAPI.Registers;
using PiratePack.Patches;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PiratePack
{
    // todo: use raycast to not clip through walls
    public class ShieldManager : MonoBehaviour
    {
        public PlayerManager pm;
        public float degreesPerSecond = 90f;
        public float distanceFromPlayer = 7f;
        public int itemSlot;
        public Transform renderer;
        public Quaternion facingQuaternion;
        public float animationTime = 0f;
        public float animationLength = 3f;
        public float wobbleStrength = 0f;
        public float timeWobbling = 0f;
        public ShieldTracker myTracker;
        public float dissolveTime = 0f;

        public class TemporaryTriggerDisable : MonoBehaviour
        {
            public Entity target;
            public MovementModifier moveMod;
            public float timeRemaining = 0.5f;


            void Update()
            {
                moveMod.movementMultiplier = 0.3f + (0.7f - timeRemaining);
                timeRemaining -= Time.deltaTime; // no timescaling here
                if (timeRemaining <= 0f)
                {
                    target.ExternalActivity.moveMods.Remove(moveMod);
                    target.SetTrigger(true);
                    Destroy(this);
                }
            }
        }

        public void Dissolve()
        {
            // de-attach from our tracker
            if (myTracker.currentInstance == this)
            {
                myTracker.currentInstance = null;
                myTracker = null;
            }
            dissolveTime = 2.8f;
            lastRotation = pm.transform.rotation;
            GetComponentInChildren<SpriteRotator>().ReflectionSetVariable("sprites", PiratePlugin.Instance.shieldDissolveAngles); // this is the worst way to do this ever but ok
        }

        public float cooldown = 0f;

        public void OnTriggerEnter(Collider other)
        {
            if (dissolveTime > 0f) return;
            if (cooldown > 0f) return;
            Entity foundEntity = other.GetComponent<Entity>();
            if (foundEntity)
            {
                if (foundEntity.gameObject.GetComponent<Balloon>()) return; // don't try to push balloons
                if (foundEntity.Frozen) return; // don't try to push away frozen entities
                if (foundEntity.Squished && !pm.plm.Entity.Squished) return; // can't push away entities that are squished while we aren't.
                MovementModifier slowSlightly = new MovementModifier(Vector3.zero, 0.5f);
                foundEntity.SetTrigger(false);
                foundEntity.ExternalActivity.moveMods.Add(slowSlightly);
                TemporaryTriggerDisable tempDisable = foundEntity.gameObject.AddComponent<TemporaryTriggerDisable>();
                tempDisable.target = foundEntity;
                tempDisable.moveMod = slowSlightly;
                foundEntity.AddForce(new Force(foundEntity.transform.position - transform.position, 20f, -20f));
                cooldown = 0.25f;
                animationTime = animationLength;
                wobbleStrength = 30f;
                facingQuaternion = Quaternion.FromToRotation(transform.forward, (foundEntity.transform.position - transform.position).normalized);

                // reduce usages down until we can't anymore
                ItemMetaData meta = ItemMetaStorage.Instance.FindByEnum(PiratePlugin.shieldItemType);
                // find our current item index
                int index = meta.itemObjects.ToList().IndexOf(pm.itm.items[pm.itm.selectedItem]);
                if (index == 0)
                {
                    Dissolve(); // dissolve first so we dont get destroyed too fast
                    pm.itm.SetItem(pm.itm.nothing, pm.itm.selectedItem);
                    Singleton<CoreGameManager>.Instance.audMan.PlaySingle(PiratePlugin.Instance.assetMan.Get<SoundObject>("ShieldDissolve"));
                    wobbleStrength += 7f;
                }
                else
                {
                    pm.itm.SetItem(meta.itemObjects[index - 1], pm.itm.selectedItem);
                    Singleton<CoreGameManager>.Instance.audMan.PlaySingle(PiratePlugin.Instance.assetMan.Get<SoundObject>("ShieldBonk"));
                }
            }
        }

        float EaseOutBack(float x)
        {
            float c4 = (2f * Mathf.PI) / 3;

            return x == 0f
              ? 0f
              : x == 1f
              ? 1f
              : Mathf.Pow(2, -10 * x) * Mathf.Sin((x * 10f - 0.75f) * c4) + 1;
        }

        public void Update()
        {
            cooldown = Mathf.Max(0f, cooldown - Time.deltaTime * pm.PlayerTimeScale);
        }

        Quaternion lastRotation;

        public void LateUpdate()
        {
            //Vector3 oldPosition = transform.position;
            if (dissolveTime > 0f)
            {
                renderer.rotation = lastRotation;
                dissolveTime -= Time.deltaTime * pm.ec.EnvironmentTimeScale; // switch to enviroment time scale now that we are no longer attached to the player
                transform.position += Vector3.down * Time.deltaTime * pm.ec.EnvironmentTimeScale * 2.7f;
                if (dissolveTime <= 0f)
                {
                    Destroy(gameObject); // bye bye
                }
            }
            else
            {
                transform.position = pm.transform.position;
                transform.rotation = Quaternion.RotateTowards(transform.rotation, pm.transform.rotation, degreesPerSecond * pm.PlayerTimeScale * Time.deltaTime);
                transform.position += transform.forward * distanceFromPlayer;
                renderer.rotation = pm.transform.rotation;
            }

            animationTime = Mathf.Max(0f, animationTime - Time.deltaTime * pm.PlayerTimeScale);
            wobbleStrength = Mathf.Max(0f, wobbleStrength - Time.deltaTime * pm.PlayerTimeScale * 15f);

            if (wobbleStrength != 0f)
            {
                timeWobbling += Time.deltaTime * pm.PlayerTimeScale;
            }
            else
            {
                timeWobbling = 0f;
            }
            if (facingQuaternion != null)
            {
                renderer.rotation = Quaternion.LerpUnclamped(renderer.rotation, renderer.rotation * facingQuaternion, EaseOutBack(animationTime / animationLength)) * Quaternion.Euler(0f, Mathf.Sin(timeWobbling * 10f) * wobbleStrength, 0f);
            }
        }
    }
}

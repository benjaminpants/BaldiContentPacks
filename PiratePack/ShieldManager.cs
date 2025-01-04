﻿using System;
using System.Collections.Generic;
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

        public class TemporaryTriggerDisable : MonoBehaviour
        {
            public Entity target;
            public MovementModifier moveMod;
            public float timeRemaining = 0.5f;


            void Update()
            {
                moveMod.movementMultiplier = 0.5f + (0.5f - timeRemaining);
                timeRemaining -= Time.deltaTime; // no timescaling here
                if (timeRemaining <= 0f)
                {
                    target.ExternalActivity.moveMods.Remove(moveMod);
                    target.SetTrigger(true);
                    Destroy(this);
                }
            }
        }

        public float cooldown = 0f;

        public void OnTriggerEnter(Collider other)
        {
            if (cooldown > 0f) return;
            Entity foundEntity = other.GetComponent<Entity>();
            if (foundEntity)
            {
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
                Singleton<CoreGameManager>.Instance.audMan.PlaySingle(PiratePlugin.Instance.assetMan.Get<SoundObject>("ShieldBonk"));
                facingQuaternion = Quaternion.FromToRotation(transform.forward, (foundEntity.transform.position - transform.position).normalized);
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

        public void LateUpdate()
        {
            //Vector3 oldPosition = transform.position;
            transform.position = pm.transform.position;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, pm.transform.rotation, degreesPerSecond * pm.PlayerTimeScale * Time.deltaTime);
            transform.position += transform.forward * distanceFromPlayer;
            renderer.rotation = pm.transform.rotation;

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

            /*targetAngle = pm.transform.eulerAngles.y - targetAngle;
            angle += Mathf.Sign(targetAngle - angle) * Mathf.Min(degreesPerSecond * Time.deltaTime * pm.PlayerTimeScale, Mathf.Abs(targetAngle - angle));
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            transform.position += transform.forward * distanceFromPlayer;*/
        }
    }
}

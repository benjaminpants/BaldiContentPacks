using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PiratePack
{
    // FEATURING: a lot of borrowed code from the criminal pack DealerBag, lol.
    // i mean hey, it is easier now that its all in one repo
    public class ITM_Doubloon : Item, IEntityTrigger
    {
        private static readonly FieldInfo _rendererBase = AccessTools.Field(typeof(Entity), "rendererBase");

        public Entity entity;
        public SpriteRenderer renderBase;
        public AudioManager audMan;
        float forwardSpeed = 10f;

        public void EntityTriggerEnter(Collider other)
        {
        }

        public void EntityTriggerExit(Collider other)
        {
        }

        public void EntityTriggerStay(Collider other)
        {
        }

        public override bool Use(PlayerManager pm)
        {
            this.pm = pm;
            entity.Initialize(pm.ec, pm.transform.position);
            entity.SetGrounded(false);
            transform.forward = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward;
            renderBase = ((Transform)_rendererBase.GetValue(entity)).GetChild(0).GetComponent<SpriteRenderer>();
            forwardSpeed += pm.plm.RealVelocity;
            entity.OnEntityMoveInitialCollision += OnEntCollision;
            return true;
        }

        void Update()
        {
            entity.UpdateInternalMovement(transform.forward * forwardSpeed);
            forwardSpeed = Mathf.Max(forwardSpeed - (Time.deltaTime * pm.ec.EnvironmentTimeScale), 0f);
        }

        void OnEntCollision(RaycastHit ray)
        {
            transform.forward = Vector3.Reflect(transform.forward, ray.normal);
        }
    }
}

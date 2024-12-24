using HarmonyLib;
using MTM101BaldAPI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class ITM_IOU : Item
    {
        private static readonly FieldInfo _rendererBase = AccessTools.Field(typeof(Entity), "rendererBase");

        public Entity entity;
        public AudioManager audMan;
        public SoundObject crumple;
        public SoundObject slap;
        EnvironmentController ec;
        SpriteRenderer renderBase;
        float forwardSpeed = 45f;
        float rotation = 0f;
        bool hasHit = false;

        IEnumerator DestroyAfterNoGuilt(MeshRenderer toDestroy)
        {
            yield return new WaitForSecondsEnvironmentTimescale(ec, 1f);
            while (pm.ruleBreak == "Fraud")
            {
                yield return null;
            }
            toDestroy.material.SetMainTexture(CriminalPackPlugin.Instance.assetMan.Get<Texture2D>("IOU_WallFade"));
            yield return new WaitForSecondsEnvironmentTimescale(ec, 1f);
            Destroy(toDestroy.gameObject);
            UnityEngine.Object.Destroy(base.gameObject);
        }

        bool AttemptInsert(IItemAcceptor itemAcceptor)
        {
            int[] possibleItems = EnumExtensions.GetValues<Items>();
            foreach (int item in possibleItems)
            {
                if (itemAcceptor.ItemFits((Items)item))
                {
                    itemAcceptor.InsertItem(pm, pm.ec);
                    return true;
                }
            }
            return false;
        }

        void Update()
        {
            if (entity == null) return;
            if (hasHit)
            {
                entity.UpdateInternalMovement(Vector3.zero);
                return;
            }
            Vector3 forwardVector = transform.forward * forwardSpeed;
            entity.UpdateInternalMovement(forwardVector);
            rotation += Time.deltaTime * ec.EnvironmentTimeScale * 180f;
            renderBase.SetSpriteRotation(rotation);
            if (Physics.Raycast(transform.position, forwardVector.normalized, out RaycastHit hit, 2.25f, pm.pc.ClickLayers))
            {
                bool didHit = false;
                List<IItemAcceptor> acceptor = hit.transform.GetComponents<IItemAcceptor>().ToList();
                acceptor.Sort((a, b) => (a.ItemFits(Items.Quarter) ? 0 : 1).CompareTo(b.ItemFits(Items.Quarter) ? 0 : 1));
                foreach (IItemAcceptor itemAcceptor in acceptor)
                {
                    if (itemAcceptor != null) 
                    {
                        if (AttemptInsert(itemAcceptor))
                        {
                            didHit = true;
                            break;
                        }
                        /*itemAcceptor.InsertItem(pm, pm.ec);
                        didHit = true;
                        break;*/
                    }
                }
                audMan.FlushQueue(true);
                audMan.PlaySingle(slap);
                hasHit = true;
                if (didHit)
                {
                    pm.RuleBreak("Fraud", 15f);
                }
                renderBase.enabled = false;
                GameObject slapObject = new GameObject();
                slapObject.name = "SlapObject";
                slapObject.transform.localScale *= 5f;
                slapObject.AddComponent<MeshFilter>().mesh = CriminalPackPlugin.Instance.assetMan.Get<Mesh>("Quad");
                MeshRenderer renderer = slapObject.AddComponent<MeshRenderer>();
                renderer.material = renderer.material = new Material(CriminalPackPlugin.Instance.assetMan.Get<Shader>("Shader Graphs/TileStandard_AlphaClip"));
                renderer.material.SetMainTexture(CriminalPackPlugin.Instance.assetMan.Get<Texture2D>("IOU_Wall"));
                renderer.material.SetTexture("_LightMap", CriminalPackPlugin.Instance.assetMan.Get<Texture2D>("LightMap"));
                slapObject.transform.position = hit.point + (hit.normal * 0.002f);
                slapObject.transform.forward = hit.normal * -1;
                StartCoroutine(DestroyAfterNoGuilt(renderer));
            }
        }

        public override bool Use(PlayerManager pm)
        {
            ec = pm.ec;
            this.pm = pm;
            transform.position = pm.transform.position;
            transform.forward = Singleton<CoreGameManager>.Instance.GetCamera(pm.playerNumber).transform.forward;
            entity.Initialize(ec, pm.transform.position);
            entity.SetGrounded(false);
            renderBase = ((Transform)_rendererBase.GetValue(entity)).GetChild(0).GetComponent<SpriteRenderer>();
            audMan.PlaySingle(crumple);
            return true;
        }
    }
}

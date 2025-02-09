using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CarnivalPack
{
    public class FrenzyBalloon : MonoBehaviour, IClickable<int>, IEntityTrigger
    {
        public Balloon myBalloon;
        public AudioManager audMan;
        public SoundObject inflateSound;
        public SoundObject popSound;
        public SpriteRenderer sprite;
        public BalloonFrenzy frenzy;
        public Sprite[] potentialSprites = new Sprite[0];
        protected bool popping = false;

        void Awake()
        {
            myBalloon = GetComponent<Balloon>();
            sprite = GetComponentInChildren<SpriteRenderer>();
        }

        public void Initialize(RoomController rc)
        {
            myBalloon.Initialize(rc);
            sprite.sprite = potentialSprites[UnityEngine.Random.Range(0, potentialSprites.Length)];
            audMan.PlaySingle(inflateSound);
        }

        public virtual void Pop(Entity popperResponsible)
        {
            popping = true;
            myBalloon.Stop();
            sprite.gameObject.SetActive(false);
            myBalloon.Entity.Enable(false);
            audMan.FlushQueue(true);
            audMan.PlaySingle(popSound);
            if (gameObject.activeSelf)
            {
                StartCoroutine(WaitForPop());
            }
            else
            {
                GameObject.Destroy(gameObject);
            }
            if (popperResponsible == null) return;
            if (popperResponsible.gameObject.TryGetComponent<FrenzyCounter>(out FrenzyCounter frenz))
            {
                frenz.OnBalloonPopped();
            }
        }

        IEnumerator WaitForPop()
        {
            while (audMan.AnyAudioIsPlaying)
            {
                yield return null;
            }
            GameObject.Destroy(gameObject);
        }

        void OnDestroy()
        {
            frenzy.spawnedBalloons.Remove(this);
        }

        public bool ClickableHidden()
        {
            return popping;
        }

        public bool ClickableRequiresNormalHeight()
        {
            return false;
        }

        public void ClickableSighted(int player)
        {
            
        }

        public void ClickableUnsighted(int player)
        {
            
        }

        public void Clicked(int player)
        {
            if (popping) return;
            Pop(Singleton<CoreGameManager>.Instance.GetPlayer(player).GetComponent<Entity>());
        }


        static FieldInfo _pm = AccessTools.Field(typeof(Item), "pm");
        public void EntityTriggerEnter(Collider other)
        {
            if (other.CompareTag("NPC"))
            {
                Pop(other.GetComponent<Entity>());
                return;
            }
            if (other.TryGetComponent<Item>(out Item item))
            {
                if (item is ITM_NanaPeel) return;
                if (_pm.GetValue(item) != null)
                {
                    Pop(((PlayerManager)_pm.GetValue(item)).GetComponent<Entity>());
                }
            }
        }

        public void EntityTriggerStay(Collider other)
        {
            
        }

        public void EntityTriggerExit(Collider other)
        {
            
        }
    }
}

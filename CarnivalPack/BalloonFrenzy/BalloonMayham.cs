using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CarnivalPack
{
    public class ComboFrenzyCounter : FrenzyCounter
    {

        public float timescaleMultipler = 1f;

        public override void OnPlayerTimeFail()
        {
            // do nothing
        }

        public override float Timescale()
        {
            return myPlayer.ec.EnvironmentTimeScale * timescaleMultipler;
        }

        public override void OnDetention()
        {
            // nope
        }

        public override void OnBalloonPopped()
        {
            ((BalloonMayhamManager)Singleton<BaseGameManager>.Instance).OnBalloonPopped();
        }
    }

    public class BalloonMayhamEvent : BalloonFrenzy
    {
        public override FrenzyCounter CreatePlayerCounter(PlayerManager pm)
        {
            ComboFrenzyCounter cfc = pm.gameObject.AddComponent<ComboFrenzyCounter>();
            cfc.timeRemaining = 10f;
            return cfc;
        }

        protected override int CalculateBalloonCountForRoom(RoomController rc, int max)
        {
            if (rc.type == RoomType.Hall)
            {
                return Mathf.CeilToInt(max / 1.75f);
            }
            if (rc.category == RoomCategory.Special)
            {
                return Mathf.Clamp(max / 10,3,max);
            }
            return base.CalculateBalloonCountForRoom(rc, max);
        }
    }


    public class BalloonMayhamManager : MainGameManager
    {

        public BalloonFrenzy eventPrefab;
        public BalloonFrenzy myEvent;
        public ComboFrenzyCounter myCounter;
        bool inCrisis = false;

        public BalloonFrenzyUI UI
        {
            get
            {
                return Singleton<CoreGameManager>.Instance.GetHud(0).GetComponent<BalloonFrenzyUI>();
            }
        }

        public float comboTime
        { 
            get
            {
                return myCounter.timeRemaining;
            }
            set
            {
                myCounter.timeRemaining = value;
            }
        }


        public float timeForCombo = 0f;
        public int currentCombo = 0;
        public float overrideReset = 0f;

        float crisisOverTime = 0f;

        protected override void VirtualUpdate()
        {
            base.VirtualUpdate();
            if (inCrisis)
            {
                Singleton<MusicManager>.Instance.SetSpeed(0.1f);
                if (comboTime >= 5f)
                {
                    inCrisis = false;
                    crisisOverTime = 0f;
                }
                else
                {
                    if (crisisOverTime > 0f)
                    {
                        crisisOverTime -= Time.deltaTime * ec.EnvironmentTimeScale;
                        if (crisisOverTime <= 0f)
                        {
                            crisisOverTime = 0f;
                            inCrisis = false;
                        }
                    }
                }
            }
            else
            {
                Singleton<MusicManager>.Instance.SetSpeed(0.5f + (((Mathf.Max(45f - comboTime, 0f)) / 45f) * 1.5f));
            }
            if (comboTime <= 0f)
            {
                OnTimeExpire();
                if (ec.GetBaldi().looker.PlayerInSight())
                {
                    inCrisis = true;
                    crisisOverTime = 6f;
                }
            }
            if (overrideReset > 0f)
            {
                overrideReset -= Time.deltaTime;
                if (overrideReset <= 0f)
                {
                    UI.textOverride = "";
                    UI.text.color = Color.white;
                    overrideReset = 0f;
                }
            }
            if (timeForCombo > 0f)
            {
                timeForCombo -= Time.deltaTime;
                if (timeForCombo <= 0f)
                {
                    myCounter.frozen = false;
                    int additionalTime = Mathf.CeilToInt(currentCombo * (currentCombo / 3f));
                    comboTime += additionalTime;
                    UI.textOverride = "+" + additionalTime;
                    overrideReset = 0.5f;
                    currentCombo = 0;
                    UI.text.color = Color.green;
                }
            }
        }

        public virtual void OnBalloonPopped()
        {
            comboTime = Mathf.Max(comboTime, 0.1f);
            myCounter.frozen = true;
            timeForCombo = 0.5f;
            currentCombo += 1;
            UI.textOverride = "x" + currentCombo;
            UI.text.color = new Color(1f,1f,0f);
            overrideReset = 0f;
            OnTimeGained();
        }

        public bool timeExpired;

        public float baldiAngerAmount = 25f;

        public virtual void OnTimeExpire()
        {
            if (timeExpired) return;
            timeExpired = true;
            ec.GetBaldi().GetAngry(baldiAngerAmount);
            ec.MakeNoise(myCounter.transform.position, 100);
            
            // this PROPERLY forces baldi to slap instantly without delay.
            // this fixes an issue where baldi would make a sudden leap to catch up with the old delay.
            ec.GetBaldi().RestoreRuler();
            ec.GetBaldi().Slap();
        }

        public virtual void OnTimeGained()
        {
            if (!timeExpired) return;
            timeExpired = false;
            ec.GetBaldi().GetAngry(-baldiAngerAmount);
            Singleton<MusicManager>.Instance.SetSpeed(1f);
        }

        public override void Initialize()
        {
            base.Initialize();
            GameObject.Destroy(GameObject.FindObjectOfType<HappyBaldi>().gameObject);
            spawnImmediately = true;
            notebookAngerVal = 0.25f;
        }

        public override void BeginPlay()
        {
            base.BeginPlay();
            myEvent = GameObject.Instantiate<BalloonFrenzy>(eventPrefab);
            myEvent.Initialize(ec, new System.Random(Singleton<CoreGameManager>.Instance.Seed()));
            myEvent.Begin();
            myCounter = (ComboFrenzyCounter)myEvent.createdCounters[0];
            Singleton<MusicManager>.Instance.PlayMidi(CarnivalPackBasePlugin.balloonMayhamMidi, true);
        }

        protected override void AllNotebooks()
        {
            if (!allNotebooksFound)
            {
                //UI.textShaking = true;
                //myCounter.timescaleMultipler = 1.25f;
                allNotebooksFound = true;
                ec.SetElevators(true);
                elevatorsToClose = ec.elevators.Count - 1;
                foreach (Elevator elevator in ec.elevators)
                {
                    if (ec.elevators.Count > 1)
                    {
                        elevator.PrepareToClose();
                    }
                    StartCoroutine(ReturnSpawnFinal(elevator));
                }
                foreach (Activity activity in ec.activities)
                {
                    if (activity != lastActivity)
                    {
                        activity.Corrupt(false);
                        activity.SetBonusMode(true);
                    }
                }
            }
        }

        protected override void LoadSceneObject(SceneObject sceneObject, bool restarting)
        {
            bool isFinal = levelObject.finalLevel;
            levelObject.finalLevel = true;
            base.LoadSceneObject(sceneObject, false);
            levelObject.finalLevel = isFinal;
        }
    }
}

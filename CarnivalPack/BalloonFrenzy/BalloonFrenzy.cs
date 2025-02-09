﻿using HarmonyLib;
using MTM101BaldAPI;
using MTM101BaldAPI.Registers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace CarnivalPack
{

    public class NavigationState_Balloon : NavigationState_TargetPosition
    {
        public NavigationState_Balloon(NPC npc, int priority, Vector3 position) : base(npc, priority, position, true)
        {
        }

        public override void DestinationEmpty()
        {
            base.DestinationEmpty();
            priority = 0;
            npc.behaviorStateMachine.RestoreNavigationState();
        }
    }

    public class FrenzyCounter : MonoBehaviour
    {
        public PlayerManager myPlayer;
        public NPC myNPC;
        public BalloonFrenzy frenzy;
        public float timeRemaining = 5f;

        bool controllingNPC = false;
        FrenzyBalloon currentBalloon;
        NavigationState currentState;

        static MethodInfo _SetGuilt = AccessTools.Method(typeof(NPC), "SetGuilt");


        void OnDestroy()
        {
            if (controllingNPC)
            {
                if (currentState != null)
                {
                    currentState.priority = 0;
                }
                Debug.Log("Removing navigation state for: " + myNPC.name);
                myNPC.behaviorStateMachine.RestoreNavigationState(); //go back to normal
            }
        }

        public void OnBalloonPopped()
        {
            timeRemaining = Mathf.Max(5f,timeRemaining);
            if (controllingNPC)
            {
                controllingNPC = false;
                currentBalloon = null;
                if (currentState != null)
                {
                    currentState.priority = 0;
                }
                currentState = null;
                myNPC.behaviorStateMachine.RestoreNavigationState();
            }
            if (myPlayer)
            {
                if (myPlayer.ruleBreak == "FrenzyBalloonNoPop")
                {
                    myPlayer.ClearGuilt();
                }
            }
            else
            {
                if (myNPC.BrokenRule == "FrenzyBalloonNoPop")
                {
                    myNPC.ClearGuilt();
                }
            }
        }

        void Update()
        {
            if (myPlayer)
            {
                timeRemaining -= Time.deltaTime * myPlayer.PlayerTimeScale;
                if (timeRemaining <= 0f)
                {
                    timeRemaining = 0f;
                    myPlayer.RuleBreak("FrenzyBalloonNoPop", 0.1f);
                }
                return;
            }
            timeRemaining -= Time.deltaTime * myNPC.TimeScale;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                _SetGuilt.Invoke(myNPC, new object[] { 0.1f, "FrenzyBalloonNoPop" });
                controllingNPC = true;
                if (currentBalloon == null)
                {
                    float closestDist = float.MaxValue;
                    for (int i = 0; i < frenzy.spawnedBalloons.Count; i++)
                    {
                        float dist = Vector3.Distance(myNPC.transform.position, frenzy.spawnedBalloons[i].transform.position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            currentBalloon = frenzy.spawnedBalloons[i];
                        }
                    }

                    currentState = new NavigationState_Balloon(myNPC, 128, currentBalloon.transform.position);
                    myNPC.navigationStateMachine.ChangeState(currentState);
                }
                else
                {
                    if (currentState != null)
                    {
                        currentState.UpdatePosition(currentBalloon.transform.position);
                    }
                }
            }
        }
    }

    public class BalloonFrenzy : RandomEvent
    {
        public List<WeightedSelection<FrenzyBalloon>> standardBalloons = new List<WeightedSelection<FrenzyBalloon>>();
        public List<FrenzyBalloon> spawnedBalloons = new List<FrenzyBalloon>();
        protected List<FrenzyCounter> createdCounters = new List<FrenzyCounter>();

        public override void Begin()
        {
            base.Begin();
            for (int i = 0; i < ec.rooms.Count; i++)
            {
                StartCoroutine(PopulateRoomWithBalloons(ec.rooms[i]));
            }
            StartCoroutine(PopulateRoomWithBalloons(ec.mainHall));
            for (int i = 0; i < ec.Npcs.Count; i++)
            {
                if (ec.Npcs[i].GetMeta().tags.Contains("no_balloon_frenzy")) continue;
                FrenzyCounter npcCounter = ec.Npcs[i].gameObject.AddComponent<FrenzyCounter>();
                npcCounter.frenzy = this;
                npcCounter.myNPC = ec.Npcs[i];
                createdCounters.Add(npcCounter);
            }
            for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
            {
                FrenzyCounter playerCounter = Singleton<CoreGameManager>.Instance.GetPlayer(i).gameObject.AddComponent<FrenzyCounter>();
                playerCounter.myPlayer = Singleton<CoreGameManager>.Instance.GetPlayer(i);
                playerCounter.frenzy = this;
                createdCounters.Add(playerCounter);
                BalloonFrenzyUI frUI = Singleton<CoreGameManager>.Instance.GetHud(i).GetComponent<BalloonFrenzyUI>();
                frUI.counterToTrack = playerCounter;
                frUI.SetState(true);
            }
        }

        void OnDestroy()
        {
            if (Singleton<CoreGameManager>.Instance == null) return;
            for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
            {
                BalloonFrenzyUI frUI = Singleton<CoreGameManager>.Instance.GetHud(i).GetComponent<BalloonFrenzyUI>();
                frUI.SetState(false);
            }
        }

        public override void End()
        {
            base.End();
            for (int i = 0; i < createdCounters.Count; i++)
            {
                Destroy(createdCounters[i]);
            }
            createdCounters.Clear();
            StartCoroutine(PopBalloons());
            for (int i = 0; i < Singleton<CoreGameManager>.Instance.setPlayers; i++)
            {
                BalloonFrenzyUI frUI = Singleton<CoreGameManager>.Instance.GetHud(i).GetComponent<BalloonFrenzyUI>();
                frUI.SetState(false);
            }
        }

        IEnumerator PopBalloons()
        {
            while (spawnedBalloons.Count > 0)
            {
                yield return new WaitForSecondsEnvironmentTimescale(ec, ((float)crng.NextDouble()) / 15f);
                FrenzyBalloon chosenBalloon = spawnedBalloons[crng.Next(0, spawnedBalloons.Count)];
                spawnedBalloons.Remove(chosenBalloon);
                chosenBalloon.Pop(null);
            }
        }

        IEnumerator PopulateRoomWithBalloons(RoomController rc)
        {
            List<Cell> safeCells = rc.AllEntitySafeCellsNoGarbage();
            if (safeCells.Count == 0)
            {
                safeCells = rc.AllTilesNoGarbage(false, true);
            }
            if (safeCells.Count == 0) yield break;
            int balloonCount = crng.Next(Mathf.RoundToInt(safeCells.Count / 2f), safeCells.Count);
            for (int i = 0; i < balloonCount; i++)
            {
                if (safeCells.Count == 0) yield break;
                Cell chosenCell = safeCells[crng.Next(0, safeCells.Count)];
                safeCells.Remove(chosenCell);
                FrenzyBalloon spawned = GameObject.Instantiate<FrenzyBalloon>(WeightedSelection<FrenzyBalloon>.ControlledRandomSelectionList(standardBalloons,crng), this.transform);
                spawned.Initialize(chosenCell.room);
                spawned.frenzy = this;
                spawned.myBalloon.Entity.Teleport(chosenCell.FloorWorldPosition);
                spawnedBalloons.Add(spawned);
                yield return new WaitForSecondsEnvironmentTimescale(ec, ((float)crng.NextDouble()) / 10f);
            }
        }
    }
}

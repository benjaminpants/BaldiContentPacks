using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    internal class NPCGiftState : NpcState
    {
        public NpcState previousState;
        public float time;
        MovementModifier stopMoving = new MovementModifier(Vector3.zero, 0f);
        public NPCGiftState(NPC npc, NpcState previousState, float time) : base(npc)
        {
            this.previousState = previousState;
            this.time = time;
        }

        public override void Enter()
        {
            base.Enter();
            npc.GetComponent<Entity>().ExternalActivity.moveMods.Add(stopMoving);
        }

        public override void Exit()
        {
            base.Exit();
            npc.GetComponent<Entity>().ExternalActivity.moveMods.Remove(stopMoving);
        }

        public override void Update()
        {
            base.Update();
            time -= Time.deltaTime * npc.TimeScale;
            if (time <= 0f)
            {
                npc.behaviorStateMachine.ChangeState(previousState);
            }
        }
    }
}

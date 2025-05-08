using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PiratePack
{
    public class NavigationState_TargetPositionRevert : NavigationState_TargetPosition
    {
        public NavigationState_TargetPositionRevert(NPC npc, int priority, Vector3 position, bool holdPriority) : base(npc, priority, position, holdPriority)
        {
        }

        public override void DestinationEmpty()
        {
            npc.behaviorStateMachine.currentState.RestoreNavigationState();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CarnivalPack
{
    public class FrenzyBalloonPoints : FrenzyBalloon
    {
        public override void Pop(Entity popperResponsible)
        {
            base.Pop(popperResponsible);
            if (popperResponsible == null) return;
            if (popperResponsible.TryGetComponent<PlayerManager>(out PlayerManager pm))
            {
                Singleton<CoreGameManager>.Instance.AddPoints(25, pm.playerNumber, true);
            }
        }
    }
    public class FrenzyBalloonExplosion : FrenzyBalloon
    {
        public override void Pop(Entity popperResponsible)
        {
            base.Pop(popperResponsible);
            if (popperResponsible == null) return;
            popperResponsible.AddForce(new Force((popperResponsible.transform.position - transform.position).normalized, 40f, -15f));
        }
    }

    public class FrenzyBalloonSquish : FrenzyBalloon
    {
        public override void Pop(Entity popperResponsible)
        {
            base.Pop(popperResponsible);
            if (popperResponsible == null) return;
            popperResponsible.Squish(5f);
        }
    }

    public class FrenzyBalloonSpeedboost : FrenzyBalloon
    {

        public class BalloonSpeedboostManager : MonoBehaviour
        {
            public ActivityModifier am;
            public MovementModifier mm;
            public EnvironmentController ec;

            float timePassed = 0f;
            void Update()
            {
                timePassed += Time.deltaTime * ec.EnvironmentTimeScale;
                if (timePassed >= 7f)
                {
                    am.moveMods.Remove(mm);
                    Destroy(this);
                }
            }
        }

        public override void Pop(Entity popperResponsible)
        {
            base.Pop(popperResponsible);
            if (popperResponsible == null) return;
            BalloonSpeedboostManager spb = popperResponsible.gameObject.AddComponent<BalloonSpeedboostManager>();
            spb.ec = Singleton<BaseGameManager>.Instance.Ec;
            spb.am = popperResponsible.ExternalActivity;
            MovementModifier mm = new MovementModifier(Vector3.zero, 1.25f);
            popperResponsible.ExternalActivity.moveMods.Add(mm);
            spb.mm = mm;
        }
    }
}

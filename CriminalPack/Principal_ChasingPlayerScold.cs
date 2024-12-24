using System;
using UnityEngine;

namespace CriminalPack
{
    public class Principal_ChasingPlayerScold : Principal_ChasingPlayer
    {
        string toScold;

        bool hasScolded = false;

        public Principal_ChasingPlayerScold(Principal principal, PlayerManager player, string toScold) : base(principal, player)
        {
            this.toScold = toScold;
        }

        public override void PlayerSighted(PlayerManager player)
        {
            base.PlayerSighted(player);
            if (hasScolded) return;
            hasScolded = true;
            principal.Scold(toScold);
        }
    }
}

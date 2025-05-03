using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace CriminalPack
{
    public class ITM_Keycard : Item
    {
        public int myValue;

        public override bool Use(PlayerManager pm)
        {
            pm.ec.GetComponent<KeycardManager>().AcquireKeycard(myValue);
            GameObject.Destroy(this.gameObject);
            return true;
        }
    }
}

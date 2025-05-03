using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{

    public class KeycardHud : MonoBehaviour
    {
        public RawImage[] renderers = new RawImage[3];

        public void ReInit()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }

        public void GiveCard(int cardNumber)
        {
            renderers[cardNumber].enabled = true;
        }
    }
}

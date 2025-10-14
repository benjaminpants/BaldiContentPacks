using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{

    public class KeycardHud : MonoBehaviour
    {
        public RawImage[] renderers = new RawImage[4];

        public void ReInit()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].enabled = false;
            }
        }

        public void GiveCard(int cardNumber)
        {
            if (renderers[3].enabled) return;
            if (cardNumber == 3)
            {
                for (int i = 0; i < renderers.Length; i++)
                {
                    renderers[i].enabled = false;
                }
            }
            renderers[cardNumber].enabled = true;
        }
    }
}

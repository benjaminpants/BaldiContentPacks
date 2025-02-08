﻿using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace CriminalPack
{
    public class ScannerItemSpriteFade : MonoBehaviour
    {
        Image mySprite;

        public Color myColor = Color.green;
        public float time = 1f;

        float startTime = 0f;

        const float step = 0.2f;

        void Awake()
        {
            mySprite = GetComponent<Image>();
        }

        void Start()
        {
            mySprite.color = myColor;
            startTime = time;
        }

        void Update()
        {
            time = Mathf.Max(time - Time.deltaTime, 0f);
            if (time == 0f)
            {
                mySprite.color = Color.white;
                Destroy(this);
                return;
            }
            mySprite.color = Color.Lerp(myColor, Color.white, Mathf.Floor((1f - (time / startTime)) / step) * step);
        }
    }
}

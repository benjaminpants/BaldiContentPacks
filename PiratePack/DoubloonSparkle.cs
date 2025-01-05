using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PiratePack
{
    public class DoubloonSparkle : MonoBehaviour
    {
        public SpriteRenderer renderer;
        public float timeAlive = 0f;
        public Sprite[] frames;
        public EnvironmentController ec;


        void Start()
        {
            renderer.sprite = frames[0];
        }

        void Update()
        {
            timeAlive += Time.deltaTime * ec.EnvironmentTimeScale;
            renderer.sprite = frames[Mathf.RoundToInt(Mathf.Min(timeAlive * (frames.Length - 1), (frames.Length - 1)))];
            if (timeAlive >= 1)
            {
                Destroy(gameObject);
            }
        }
    }
}

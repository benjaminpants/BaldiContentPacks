using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CarnivalPack
{
    public class BalloonFrenzyUI : MonoBehaviour
    {
        public Image balloonImage;
        public TextMeshProUGUI text;
        public FrenzyCounter counterToTrack;
        bool currentlyActive = false;

        Vector2 activePosition = new Vector2(-50f, -225f);
        Vector2 offPosition = new Vector2(50f, -225f);
        IEnumerator currentTransition = null;
        float totalTime = 0f;
        public void SetState(bool state)
        {
            if (currentlyActive != state)
            {
                if (currentTransition != null)
                {
                    StopCoroutine(currentTransition);
                }
                currentTransition = LerpToPosition(state ? activePosition : offPosition, 1f);
                StartCoroutine(currentTransition);
            }    
            currentlyActive = state;
        }

        void Update()
        {
            totalTime += Time.deltaTime;
            if ((currentTransition == null) && currentlyActive)
            {
                balloonImage.rectTransform.anchoredPosition = activePosition + (Vector2.up * CalculateBob());
            }
            if (counterToTrack != null)
            {
                int minutesLeft = Mathf.FloorToInt(counterToTrack.timeRemaining / 60f);
                int secondsLeft = Mathf.CeilToInt(counterToTrack.timeRemaining) % 60;
                text.text = minutesLeft + ":" + secondsLeft.ToString("D2");
            }
        }

        public float CalculateBob()
        {
            return Mathf.Round(Mathf.Sin(totalTime * 1.5f) * 4f);
        }

        IEnumerator LerpToPosition(Vector2 pos, float time)
        {
            Vector2 startPos = balloonImage.rectTransform.anchoredPosition;
            float passedTime = 0f;
            while (passedTime < time)
            {
                passedTime += Time.deltaTime;
                Vector2 lerpedPos = Vector2.Lerp(startPos, pos, passedTime / time) + Vector2.up * CalculateBob();
                balloonImage.rectTransform.anchoredPosition = new Vector2(Mathf.Round(lerpedPos.x), lerpedPos.y);
                yield return null;
            }
            balloonImage.rectTransform.anchoredPosition = pos + (Vector2.up * CalculateBob());
            currentTransition = null;
        }

    }
}

//Ommy
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Ommy.FadeSystem
{
    public class ScreenFader : MonoBehaviour
    {
        public static ScreenFader Instance;
        private void Awake() 
        {
            if(Instance==null)
            Instance = this;
        }
        public Image fadeOverlay;
        public float fadeDuration = 1.0f;
        public float fadeOutDely=1;
        private  Coroutine currentFadeCoroutine;
        private void Start()
        {
            if (fadeOverlay == null)
            {
                Debug.LogError("Fade Overlay Image not assigned. Please assign it in the inspector.");
            }
            else
            {
                FadeIn();
            }
        }
        public  void FadeIn()
        {
            StartFadeCoroutine(1.0f, 0.0f);
        }
        public  void FadeOut()
        {
            StartFadeCoroutine(0.0f, 1.0f);
        }

        public  void FadeInOut()
        {
            StartFadeCoroutine(0.0f, 1.0f);
            StartCoroutine(WaitAndStartFadeOut());
        }

        private void StartFadeCoroutine(float startAlpha, float targetAlpha)
        {
            if (currentFadeCoroutine != null)
            {
                StopCoroutine(currentFadeCoroutine);
            }

            currentFadeCoroutine = StartCoroutine(Fade(startAlpha, targetAlpha));
        }

        IEnumerator Fade(float startAlpha, float targetAlpha)
        {
            Color currentColor = fadeOverlay.color;
            float timer = 0.0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / fadeDuration);
                currentColor.a = alpha;
                fadeOverlay.color = currentColor;
                yield return null;
            }
        }

        IEnumerator WaitAndStartFadeOut()
        {
            yield return currentFadeCoroutine;
            yield return new WaitForSeconds(fadeOutDely);
            StartFadeCoroutine(1.0f, 0.0f);
        }
    }
}
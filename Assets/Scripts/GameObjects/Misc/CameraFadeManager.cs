using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace GameObjects.Misc
{
    /// <summary>
    /// Handles fading the screen from black to clear, often used at the start of a scene.
    /// Requires a Canvas with a black Image component as a child.
    /// </summary>
    public class CameraFadeManager : MonoBehaviour
    {
        [Header("Fade Settings")]
        [Tooltip("Czas trwania efektu przechodzenia od czarnego ekranu do sceny.")]
        public float fadeDuration = 2.0f;

        [Tooltip("Panel UI (Image), który bêdzie u¿yty do zakrycia ekranu. Powinien byæ czarny.")]
        public Image fadePanel;

        private void Start()
        {
            // SprawdŸ, czy panel UI zosta³ przypisany
            if (fadePanel == null)
            {
                Debug.LogError("Brak przypisanego 'Fade Panel' (Image) w CameraFadeManager. Skrypt nie zadzia³a.");
                return;
            }

            // Ustaw panel na pe³n¹ czern i rozpocznij efekt fade-in.
            Color startColor = Color.black;
            startColor.a = 1f; // Pe³na nieprzezroczystoœæ (czarny)
            fadePanel.color = startColor;

            // Upewnij siê, ¿e panel jest aktywny na pocz¹tku
            fadePanel.gameObject.SetActive(true);

            // Rozpocznij korutynê do p³ynnego przejœcia
            StartCoroutine(FadeInRoutine());
        }

        /// <summary>
        /// Korutyna, która p³ynnie zmniejsza przezroczystoœæ panelu.
        /// </summary>
        private IEnumerator FadeInRoutine()
        {
            float timer = 0f;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;

                // Oblicz now¹ wartoœæ alfa (przezroczystoœci)
                // Zaczynamy od 1 (czarny) i idziemy do 0 (przezroczysty)
                float newAlpha = 1f - (timer / fadeDuration);

                Color currentColor = fadePanel.color;
                currentColor.a = newAlpha;
                fadePanel.color = currentColor;

                yield return null; // Poczekaj na nastêpn¹ klatkê
            }

            // Upewnij siê, ¿e na koniec panel jest ca³kowicie przezroczysty i mo¿na go wy³¹czyæ.
            Color finalColor = fadePanel.color;
            finalColor.a = 0f;
            fadePanel.color = finalColor;

            // Opcjonalnie: Wy³¹cz obiekt Panelu, aby nie by³ renderowany, 
            // ale mo¿na go pozostawiæ aktywnego dla ³atwiejszego debugowania.
            // fadePanel.gameObject.SetActive(false); 

            Debug.Log("Efekt Fade-In zakoñczony. Scena jest widoczna.");
        }
    }
}
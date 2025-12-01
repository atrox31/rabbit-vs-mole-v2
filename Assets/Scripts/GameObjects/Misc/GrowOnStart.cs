using System.Collections;
using UnityEngine;

namespace GameObjects.Misc
{
    public class GrowOnStart : MonoBehaviour
    {
        // The duration of the scaling animation in seconds.
        [SerializeField] private float scaleUpDuration = 1.0f;
        [SerializeField] private float timeRandomnes = 0.2f;

        private float _scaleUpDurationAfterRandom;

        // The original scale of the object, which we will scale up to.
        private Vector3 originalScale;

        private void Awake()
        {
            // Save the original scale of the object before starting the animation.
            originalScale = transform.localScale;

            // Set the initial scale to zero, so the animation can begin from nothing.
            transform.localScale = Vector3.zero;

            _scaleUpDurationAfterRandom = scaleUpDuration + Random.Range(-timeRandomnes, timeRandomnes);
        }

        private void Start()
        {
            // Start the coroutine to handle the scaling animation over time.
            StartCoroutine(ScaleOverTime());
        }

        private IEnumerator ScaleOverTime()
        {
            float elapsedTime = 0f;

            while (elapsedTime < _scaleUpDurationAfterRandom)
            {
                // Calculate the current progress of the animation (from 0 to 1).
                float scaleProgress = elapsedTime / _scaleUpDurationAfterRandom;

                // Use Mathf.Lerp to smoothly interpolate the scale from zero to the original scale.
                transform.localScale = Vector3.Lerp(Vector3.zero, originalScale, scaleProgress);

                // Increase the elapsed time by the time passed since the last frame.
                elapsedTime += Time.deltaTime;

                yield return null; // Wait for the next frame.
            }

            // Ensure the final scale is exactly the original scale to prevent rounding errors.
            transform.localScale = originalScale;
        }
    }
}
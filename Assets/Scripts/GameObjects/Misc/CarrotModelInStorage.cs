using System.Collections;
using UnityEngine;

namespace GameObjects.Misc
{
    public class CarrotModelInStorage : MonoBehaviour
    {
        private Rigidbody m_rigBody;
        private bool m_hasPhysics = true;
        private bool m_isBeingDeleted = false;
        [SerializeField] private float m_activeTime = 5.0f;
        private float m_currendActiveTime = 0.0f;
        [SerializeField] private const float forceMagnitude = 1.0f; // Adjust this value to change the force applied


        public bool Delete()
        {
            if(m_isBeingDeleted) return false;
            // Start the shrinking coroutine
            StartCoroutine(ShrinkAndDestroy());
            m_isBeingDeleted = true;
            return true;
        }

        private void Awake()
        {
            m_rigBody = GetComponent<Rigidbody>();

        }

        private void Start()
        {
            // Generate a random direction vector for the force
            Vector3 randomDirection = new Vector3(
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f)
            ).normalized;

            // Apply the random force
            m_rigBody.AddForce(randomDirection * forceMagnitude, ForceMode.Impulse);


            // Generate a random torque
            Vector3 randomTorque = new Vector3(
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f),
                Random.Range(-1.0f, 1.0f)
            );

            // Apply the random torque to make the object spin
            m_rigBody.AddTorque(randomTorque * forceMagnitude, ForceMode.Impulse);
            StartCoroutine(PsyhixCheck());
        }

        private IEnumerator PsyhixCheck()
        {
            yield return new WaitForFixedUpdate();
            while (m_hasPhysics && (m_currendActiveTime < m_activeTime) && !m_isBeingDeleted)
            {
                Debug.Log(m_rigBody.linearVelocity.sqrMagnitude);
                m_currendActiveTime += Time.deltaTime;
                // Check if velocity is nearly zero
                if (m_rigBody.linearVelocity.sqrMagnitude < 0.01f)
                {
                    // Deactivate physics by setting the rigidbody to kinematic
                    m_rigBody.isKinematic = true;
                    // Deactivate the "hasPhysics" flag to avoid repeated checks
                    m_hasPhysics = false;
                }
                yield return null;
            }
        }

        private IEnumerator ShrinkAndDestroy()
        {
            // Shrink duration
            float duration = 0.333f;
            float timer = 0f;
            Vector3 initialScale = transform.localScale;

            // Animate the scale over time
            while (timer < duration)
            {
                timer += Time.deltaTime;
                transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, timer / duration);
                yield return null;
            }

            // Ensure scale is exactly zero
            transform.localScale = Vector3.zero;

            // Destroy the game object
            Destroy(gameObject);
        }
    }
}
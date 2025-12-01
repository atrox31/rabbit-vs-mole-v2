using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WaspController : MonoBehaviour
{
    // Configuration
    [Header("Movement")]
    public float moveSpeed = 5f;
    public float rotationSpeed = 3f;
    public float waspHeightOnTerrain = 1f;

    [Header("Circling")]
    public float circlingRadius = 0.5f;
    public float circlingSpeed = 2f; // Angular speed (radians/sec)
    public float circlingTime = 8f;
    public float spiralInSpeed = 1.5f; // Speed to Lerp radius

    [Header("Behavior")]
    public float restDuration = 5f;
    public float targetRandomness = 5.0f;

    [Header("Visuals (Wings)")]
    [SerializeField] private List<Transform> _wings = new List<Transform>();
    public float wingFlapSpeed = 30f;
    public float wingFlapAmplitude = 45f;

    // State
    private Transform _currentTarget;
    private Flower _currentFlower;
    private enum WaspState { FindTarget, GoToTarget, CircleFlower, LandOnFlower, Rest }
    private WaspState _currentState = WaspState.FindTarget; 
    private Flower _lastVisitedFlower;

    // Internal
    private int _terrainLayerMask;
    private bool _isFlying = false;

    void Start()
    {
        _terrainLayerMask = LayerMask.GetMask("Terrain");
        StartCoroutine(WaspBehaviorRoutine());
    }

    private void Update()
    {
        HandleWingFlapping();
    }

    /// <summary>
    /// Handles the visual flapping of wings using a smooth Sin wave.
    /// </summary>
    private void HandleWingFlapping()
    {
        if (!_wings.Any()) return;

        // Calculate the target angle based on state
        float targetAngle;
        if (_isFlying)
        {
            // Use Sin wave for smooth flapping
            // 90 degrees is the neutral (mid-point)
            targetAngle = 90.0f + Mathf.Sin(Time.time * wingFlapSpeed) * wingFlapAmplitude;
        }
        else
        {
            // Resting angle
            targetAngle = 90.0f;
        }

        // Apply rotation to all wings
        foreach (var wing in _wings)
        {
            // Use localRotation to rotate relative to the wasp's body
            wing.localRotation = Quaternion.Euler(0.0f, 0.0f, targetAngle);
        }
    }

    /// <summary>
    /// The main state machine for the wasp's behavior.
    /// </summary>
    IEnumerator WaspBehaviorRoutine()
    {
        while (true)
        {
            switch (_currentState)
            {
                case WaspState.FindTarget:
                    _isFlying = false; // Technically should be true, but this stops wings

                    // Find a new target
                    if ((_currentFlower = FindNearestFlower()) != null)
                    {
                        _currentTarget = _currentFlower.transform;
                        _currentFlower.Occupy();
                        _currentState = WaspState.GoToTarget;
                    }
                    else
                    {
                        // Wait before trying again if no flower was found
                        yield return new WaitForSeconds(1f);
                    }
                    break;

                case WaspState.GoToTarget:
                    _isFlying = true;
                    yield return StartCoroutine(GoToTargetRoutine());
                    _currentState = WaspState.CircleFlower;
                    break;

                case WaspState.CircleFlower:
                    _isFlying = true;
                    yield return StartCoroutine(CircleFlowerRoutine(circlingTime));
                    _currentState = WaspState.LandOnFlower;
                    break;

                case WaspState.LandOnFlower:
                    _isFlying = false; // Slowing down for landing
                    yield return StartCoroutine(LandOnFlowerRoutine());
                    _currentState = WaspState.Rest;
                    break;

                case WaspState.Rest:
                    _isFlying = false;
                    yield return new WaitForSeconds(restDuration);
                    if (_currentFlower != null)
                    {
                        _lastVisitedFlower = _currentFlower;
                        _currentFlower.Free();
                        _currentFlower = null;
                        _currentTarget = null;
                    }
                    _currentState = WaspState.FindTarget;
                    break;
            }
            yield return null;
        }
    }

    /// <summary>
    /// Correctly sets the Y position of the transform.
    /// </summary>
    private void SetY(float y)
    {
        transform.position = new Vector3(transform.position.x, y, transform.position.z);
    }

    // --- State-Specific Routines ---

    /// <summary>
    /// Finds the nearest available flower, adding a random factor to the distance 
    /// to encourage choosing different nearby flowers.
    /// </summary>
    Flower FindNearestFlower()
    {
        Flower target = null;
        float minWeightedDist = Mathf.Infinity; // Changed to weighted distance

        // Iterate the static list directly to avoid LINQ allocation
        foreach (var flower in Flower.InstnaceList)
        {
            // Skip if this is our current flower or if it's not free
            if (flower == _currentFlower || !flower.IsFree || flower == _lastVisitedFlower)
            {
                continue;
            }

            float dist = Vector3.Distance(transform.position, flower.transform.position);

            // --- ADD RANDOMNESS TO DISTANCE ---
            // Add a random offset to the distance. This makes nearby flowers look "further" 
            // sometimes, encouraging the wasp to choose a different destination.
            float weightedDist = dist + Random.Range(0f, targetRandomness);

            if (weightedDist < minWeightedDist)
            {
                minWeightedDist = weightedDist;
                target = flower;
            }
        }
        return target;
    }

    /// <summary>
    /// Moves towards the target flower, maintaining a constant height over terrain.
    /// </summary>
    IEnumerator GoToTargetRoutine()
    {
        float closeEnoughDistance = circlingRadius * 1.5f;

        while (_currentTarget != null)
        {
            // 1. Calculate target position on the XZ plane
            Vector3 targetFlowerPos = _currentTarget.position;
            Vector3 targetPosXZ = new Vector3(targetFlowerPos.x, transform.position.y, targetFlowerPos.z);

            // 2. Check distance only on the XZ plane
            float distanceXZ = Vector3.Distance(new Vector2(transform.position.x, transform.position.z),
                                                new Vector2(targetFlowerPos.x, targetFlowerPos.z));

            if (distanceXZ <= closeEnoughDistance)
            {
                break; // We are close enough to start circling
            }

            // 3. Move towards the target (XZ only)
            transform.position = Vector3.MoveTowards(transform.position, targetPosXZ, moveSpeed * Time.deltaTime);

            // 4. Rotate to face the target
            Vector3 direction = (targetFlowerPos - transform.position).normalized;
            if (direction.magnitude > 0.01f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }

            // 5. Adjust height based on terrain
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 10.0f, _terrainLayerMask))
            {
                // Use the corrected SetY function
                SetY(hit.point.y + waspHeightOnTerrain);
            }
            yield return null;
        }
    }

    /// <summary>
    /// Circles the flower in a smooth spiral-in motion.
    /// </summary>
    IEnumerator CircleFlowerRoutine(float duration)
    {
        float timer = 0f;

        // 1. Define the center point of the orbit
        Vector3 centerPoint = _currentTarget.position;
        centerPoint.y += _currentFlower.GetSlotHeight;

        // 2. Calculate the *starting* angle and radius based on our current position
        Vector3 startOffset = transform.position - centerPoint;
        startOffset.y = 0; // Project onto the XZ plane

        float currentAngle = Mathf.Atan2(startOffset.z, startOffset.x);
        float currentRadius = startOffset.magnitude;

        Vector3 previousPosition = transform.position;

        while (timer < duration && _currentTarget != null)
        {
            // 3. Smoothly Lerp the radius from its starting value to the target circlingRadius
            currentRadius = Mathf.Lerp(currentRadius, circlingRadius, Time.deltaTime * spiralInSpeed);

            // 4. Increment the angle
            currentAngle += Time.deltaTime * circlingSpeed;

            // 5. Calculate the next position on the (shrinking) spiral
            float x = centerPoint.x + Mathf.Cos(currentAngle) * currentRadius;
            float z = centerPoint.z + Mathf.Sin(currentAngle) * currentRadius;
            Vector3 nextPosition = new Vector3(x, centerPoint.y, z);

            // 6. Move towards the next position (MoveTowards is smoother than Lerp here)
            transform.position = Vector3.MoveTowards(transform.position, nextPosition, moveSpeed * 0.5f * Time.deltaTime);

            // 7. Rotate to look "forward" (in the direction of movement)
            Vector3 movementDirection = transform.position - previousPosition;
            if (movementDirection.magnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(movementDirection.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, rotationSpeed * Time.deltaTime);
            }

            previousPosition = transform.position;
            timer += Time.deltaTime;
            yield return null;
        }
    }

    /// <summary>
    /// Lands the wasp on the flower.
    /// </summary>
    IEnumerator LandOnFlowerRoutine()
    {
        if (_currentTarget == null) yield break;

        Vector3 targetPosition = _currentTarget.position;
        targetPosition.y += _currentFlower.GetSlotHeight;

        // Use a loop with a small threshold for smooth landing
        while (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * moveSpeed * 0.5f);
            yield return null;
        }

        // Snap to final position
        transform.position = targetPosition;
    }
}
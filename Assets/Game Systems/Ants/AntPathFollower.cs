using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AntPathFollower : MonoBehaviour
{
    [SerializeField] public float AntSpeedPerUnit = 0.1f;
    [SerializeField] public int pathPrecalculationPointsQuality = 10;

    private float calculatedPathDuration;
    private Vector3[] _pathPoints;

    private ParticleSystem antParticleSystem;
    private ParticleSystem.Particle[] ants;
    private int numAliveParticles;

    private int pathPrecalculationPoints;
    private Vector3[] precalculatedPath;

    void Start()
    {
        antParticleSystem = GetComponent<ParticleSystem>();
        if (antParticleSystem == null)
        {
            Debug.LogWarning("AntPathFollower: Start() -> Particle system not found.");
            Demolish();
            return;
        }

        List<Transform> _pathPointsTransfrom = GetComponentsInChildren<Transform>().Skip(1).ToList();
        if (!_pathPointsTransfrom.Any())
        {
            Debug.LogWarning("AntPathFollower: Start() -> No child objects found to define the path.");
            Demolish();
            return;
        }

        _pathPoints = _pathPointsTransfrom.Select(t => t.position).ToArray();
        if (_pathPoints.Length < 2)
        {
            Debug.LogWarning("AntPathFollower: Start() -> Path requires at least 2 points.");
            Demolish();
            return;
        }

        CalculatePathDuration(); 

        if (pathPrecalculationPoints <= 0)
            pathPrecalculationPoints = (int)calculatedPathDuration * pathPrecalculationPointsQuality;
        
        if (pathPrecalculationPoints < 2)
            pathPrecalculationPoints = 2;
        
        PrecalculateFull3DPath();

        ants = new ParticleSystem.Particle[antParticleSystem.main.maxParticles];
        var main = antParticleSystem.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.01f, calculatedPathDuration);
    }
    private void Demolish()
    {
        enabled = false;
        Destroy(gameObject, 0.1f);
    }

    private void CalculatePathDuration()
    {
        float totalLength = 0f;
        if (_pathPoints.Length < 2)
        {
            calculatedPathDuration = 10f;
            return;
        }

        for (int i = 0; i < _pathPoints.Length - 1; i++)
        {
            totalLength += Vector3.Distance(_pathPoints[i], _pathPoints[i + 1]);
        }

        if (AntSpeedPerUnit > 0)
        {
            calculatedPathDuration = totalLength / AntSpeedPerUnit;
        }
        else
        {
            calculatedPathDuration = 100f;
        }
    }

    private void PrecalculateFull3DPath()
    {
        if (_pathPoints == null || _pathPoints.Length < 2) return;

        precalculatedPath = new Vector3[pathPrecalculationPoints];

        for (int i = 0; i < pathPrecalculationPoints; i++)
        {
            float t = (float)i / (pathPrecalculationPoints - 1);

            int numSegments = _pathPoints.Length - 1;
            int p1Index = Mathf.FloorToInt(t * numSegments);
            p1Index = Mathf.Clamp(p1Index, 0, numSegments - 1);
            int p2Index = p1Index + 1;
            int p0Index = Mathf.Max(0, p1Index - 1);
            int p3Index = Mathf.Min(_pathPoints.Length - 1, p2Index + 1);
            float segmentLength = 1f / numSegments;
            float segmentT = (t - (p1Index * segmentLength)) / segmentLength;

            Vector3 p0 = _pathPoints[p0Index];
            Vector3 p1 = _pathPoints[p1Index];
            Vector3 p2 = _pathPoints[p2Index];
            Vector3 p3 = _pathPoints[p3Index];

            Vector3 flatPathPosition = CatmullRom(p0, p1, p2, p3, segmentT);

            Vector3 rayOrigin = flatPathPosition + Vector3.up * 50f;
            const float maxDistance = 100f;

            if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, maxDistance))
            {
                flatPathPosition.y = hit.point.y + 0.05f;
            }
            else
            {
                flatPathPosition.y = 0;
            }

            precalculatedPath[i] = flatPathPosition;
        }
    }

    void LateUpdate()
    {
        numAliveParticles = antParticleSystem.GetParticles(ants);
        if (numAliveParticles == 0) return; // Early exit if no particles
        
        // Cache Time.time once per frame instead of accessing it in the loop
        float currentTime = Time.time;
        float timeOverDuration = currentTime / calculatedPathDuration;
        
        for (int i = 0; i < numAliveParticles; i++)
        {
            ParticleSystem.Particle p = ants[i];

            float t = 1f - (p.remainingLifetime / p.startLifetime);

            // Use the calculatedPathDuration for wrapping the progress over time
            float pathProgress = timeOverDuration + t;
            pathProgress %= 1f;

            Vector3 newPos = GetPathPosition(pathProgress);

            p.position = newPos;

            ants[i] = p;
        }

        antParticleSystem.SetParticles(ants, numAliveParticles);
    }

    private Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        return 0.5f * (a + (b * t) + (c * t2) + (d * t3));
    }

    private Vector3 GetPathPosition(float t)
    {
        if (precalculatedPath == null || precalculatedPath.Length < 2)
            return Vector3.zero;

        float indexF = t * (precalculatedPath.Length - 1);
        int index1 = (int)indexF;
        int index2 = index1 + 1;
        float localT = indexF - index1;

        return Vector3.Lerp(precalculatedPath[index1], precalculatedPath[index2], localT);
    }
}
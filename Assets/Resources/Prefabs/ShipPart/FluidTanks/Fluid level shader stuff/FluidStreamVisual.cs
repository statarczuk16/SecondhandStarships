using System.Collections.Generic;
using UnityEngine;

public class FluidStreamResult
{
    public readonly List<Vector3> Points = new();

    public RaycastHit Hit;

    public Component_FluidReceiver Receiver;

    public bool HitSomething;
}


public static class FluidStreamSimulator
{
    public static FluidStreamResult Simulate(
        Vector3 start,
        Vector3 velocity,
        Vector3 gravity,
        int sampleCount,
        float sampleStep)
    {
        FluidStreamResult result = new();

        result.Points.Add(start);

        Vector3 previous = start;

        for (int i = 1; i < sampleCount; i++)
        {
            float t = i * sampleStep;

            Vector3 point =
                start +
                velocity * t +
                0.5f * gravity * t * t;

            if (Physics.Linecast(previous, point, out RaycastHit hit))
            {
                result.Points.Add(hit.point);

                result.Hit = hit;
                result.HitSomething = true;

                result.Receiver = ShipPartUtilities.FindComponentWithinPrefab<Component_FluidReceiver>(hit.collider.transform);

                break;
            }

            result.Points.Add(point);

            previous = point;
        }

        return result;
    }
}



[RequireComponent(typeof(LineRenderer))]
public class FluidStreamVisual : MonoBehaviour
{
    [SerializeField] private LineRenderer line;
    [SerializeField] private float textureScrollSpeed = 4f;

    [Header("Particle Stream")]
    [SerializeField] private ParticleSystem m_streamParticlesPrefab;
    [SerializeField] private ParticleSystem m_streamParticles;
    [SerializeField] private float m_particleSpeedAlongPath = 3f;
    [SerializeField] private float m_particleLifetime = 0.4f;
    [SerializeField] private float m_particleSpread = 0.05f;

    


    private void Awake()
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        if (m_streamParticlesPrefab != null)
        {
            m_streamParticles = Instantiate(m_streamParticlesPrefab, transform);
        }
        m_streamParticles.Play();


    }

    public void SetPoints(FluidStreamResult result)
    {
        line.positionCount = result.Points.Count;
        line.SetPositions(result.Points.ToArray());

        EmitAlongPath(result);

        
    }

    private void EmitAlongPath(FluidStreamResult result)
    {
        if (m_streamParticles == null)
            return;

        List<Vector3> points = result.Points;
        for (int i = 0; i < points.Count; i++)
        {
            Vector3 position = points[i];

            // Tangent: direction toward the next point (or from the previous, at the tail).
            Vector3 tangent;
            if (i < points.Count - 1)
                tangent = (points[i + 1] - position).normalized;
            else if (i > 0)
                tangent = (position - points[i - 1]).normalized;
            else
                tangent = transform.forward;

            Vector3 jitter = Random.insideUnitSphere * m_particleSpread;

            ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams
            {
                position = position + jitter,
                velocity = tangent * m_particleSpeedAlongPath,
                startLifetime = m_particleLifetime,
                applyShapeToPosition = false
            };

            m_streamParticles.Emit(emitParams, 1);
        }
    }

   

    public void Hide()
    {
        line.positionCount = 0;
        // Particle systems aren't cleared here on purpose - letting in-flight
        // particles/splash finish their own lifetime looks better than a hard cut.
    }

    private void Update()
    {
        if (line.material == null)
            return;

        Vector2 offset = line.material.mainTextureOffset;
        offset.x -= textureScrollSpeed * Time.deltaTime;
        line.material.mainTextureOffset = offset;
    }
}
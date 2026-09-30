using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Centralized system responsible for processing buoyancy
/// and hydrodynamic forces for all active BuoyantBody instances.
/// </summary>
public class BuoyancySystem : MonoBehaviour
{
    public static BuoyancySystem Instance { get; private set; }

    private readonly Dictionary<BuoyantBody, BodySimulationData> activeBodies = new();

    [Header("Debug")]
    [SerializeField] private bool drawSubmergedTriangles = true;
    [SerializeField] private bool drawNormals = true;
    [SerializeField] private bool drawForces = true;

    [SerializeField, Min(0f)]
    private float normalDebugLength = 0.4f;

    [SerializeField, Min(0f)]
    private float forceDebugScale = 0.005f;

    [SerializeField, Min(0f)]
    private float maxForceDebugLength = 1.0f;


    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
            return;

        BuoyancySystem existing = FindFirstObjectByType<BuoyancySystem>();

        if (existing != null)
        {
            Instance = existing;
            return;
        }

        GameObject go = new GameObject("[BuoyancySystem]");

        Instance = go.AddComponent<BuoyancySystem>();

        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        DontDestroyOnLoad(gameObject);
    }


    internal void RegisterBuoyantBody(
        BuoyantBody body,
        FluidVolume fluidVolume)
    {
        if (!activeBodies.TryGetValue(
                body,
                out BodySimulationData simulationData))
        {
            simulationData =
                new BodySimulationData(body.Vertices.Length);

            activeBodies.Add(body, simulationData);
        }

        simulationData.Volumes.Add(fluidVolume);
    }

    internal void UnregisterBuoyantBody(
        BuoyantBody body,
        FluidVolume fluidVolume)
    {
        if (!activeBodies.TryGetValue(
                body,
                out BodySimulationData simulationData))
        {
            Debug.LogWarning(
                $"Tried to unregister unregistered body: {body.name}");

            return;
        }

        if (!simulationData.Volumes.Remove(fluidVolume))
        {
            Debug.LogWarning(
                $"Body {body.name} was not registered in volume {fluidVolume.name}");

            return;
        }

        if (simulationData.Volumes.Count == 0)
        {
            activeBodies.Remove(body);
        }
    }


    private void FixedUpdate()
    {
        foreach (var pairBodyVolume in activeBodies)
        {
            BuoyantBody body = pairBodyVolume.Key;
            BodySimulationData simulationData = pairBodyVolume.Value;

            simulationData.DebugTriangles.Clear();

            // Transform each mesh vertex only once per physics tick.
            for (int i = 0; i < body.Vertices.Length; i++)
            {
                simulationData.WorldVertices[i] = body.transform.TransformPoint(body.Vertices[i]);
            }

            // Process every fluid volume this body currently intersects.
            foreach (FluidVolume fluidVolume in simulationData.Volumes)
            {
                Vector3 localMin = fluidVolume.LocalMin;
                Vector3 localMax = fluidVolume.LocalMax;

                // Process mesh triangles.
                for (int i = 0; i < body.Triangles.Length; i += 3)
                {
                    int ia = body.Triangles[i];
                    int ib = body.Triangles[i + 1];
                    int ic = body.Triangles[i + 2];

                    Vector3 worldA = simulationData.WorldVertices[ia];
                    Vector3 worldB = simulationData.WorldVertices[ib];
                    Vector3 worldC = simulationData.WorldVertices[ic];

                    // Perform the clipping in FluidVolume local space.
                    Vector3 localA = fluidVolume.transform.InverseTransformPoint(worldA);
                    Vector3 localB = fluidVolume.transform.InverseTransformPoint(worldB);
                    Vector3 localC = fluidVolume.transform.InverseTransformPoint(worldC);

                    int clippedCount = GeometryUtils.ClipTriangleAgainstBox(localA, localB, localC, localMin, localMax, simulationData.ClipBufferA, simulationData.ClipBufferB);

                    if (clippedCount < 3)
                    {
                        continue;
                    }

                    // The clipping result is stored in ClipBufferA.
                    // Convert p0 once since every fan triangle shares it.
                    Vector3 worldP0 = fluidVolume.transform.TransformPoint(simulationData.ClipBufferA[0]);

                    // Fan triangulation:
                    // p0, p1, p2
                    // p0, p2, p3
                    // p0, p3, p4
                    // ...
                    for (int j = 1; j < clippedCount - 1; j++)
                    {
                        Vector3 worldP1 = fluidVolume.transform.TransformPoint(simulationData.ClipBufferA[j]);
                        Vector3 worldP2 = fluidVolume.transform.TransformPoint(simulationData.ClipBufferA[j + 1]);

                        ProcessSubmergedTriangle(body, fluidVolume, worldP0, worldP1, worldP2, simulationData);
                    }
                }
            }
        }
    }


    private void ProcessSubmergedTriangle(BuoyantBody body, FluidVolume fluid, Vector3 a, Vector3 b, Vector3 c, BodySimulationData simulationData)
    {
        GeometryUtils.GetTriangleData(a, b, c, out float area, out Vector3 centroid, out Vector3 normal);

        if (area <= Mathf.Epsilon)
        {
            return;
        }

        float depth = fluid.GetDepth(centroid);

        if (depth <= 0f)
        {
            return;
        }


        // Hydrostatic pressure
        float pressure = fluid.Density * Physics.gravity.magnitude * depth;
        Vector3 hydrostaticForce = -normal * pressure * area;

        // Relative velocity
        // Fluid is currently considered static.
        Vector3 relativeVelocity = body.Rigidbody.GetPointVelocity(centroid);


        // Normal drag
        float normalSpeed = Vector3.Dot(relativeVelocity, normal);
        Vector3 normalDrag = Vector3.zero;

        if (normalSpeed > 0f)
        {
            float dragMagnitude = 0.5f * fluid.Density * 1f * area * normalSpeed * normalSpeed;
            normalDrag = -normal * dragMagnitude;
        }


        // Tangential drag
        Vector3 normalVelocity = normal * normalSpeed;
        Vector3 tangentialVelocity = relativeVelocity - normalVelocity;
        Vector3 tangentialDrag = Vector3.zero;

        float tangentialSpeed = tangentialVelocity.magnitude;

        if (tangentialSpeed > Mathf.Epsilon)
        {
            float dragMagnitude = 0.5f * fluid.Density * 1f * area * tangentialSpeed * tangentialSpeed;
            tangentialDrag = -tangentialVelocity.normalized * dragMagnitude;
        }


        // Total force
        Vector3 totalForce = hydrostaticForce + normalDrag + tangentialDrag;
        body.Rigidbody.AddForceAtPosition(totalForce, centroid, ForceMode.Force);


        // Debug
        simulationData.DebugTriangles.Add(
            new BodySimulationData.TriangleDebugData
            {
                A = a,
                B = b,
                C = c,

                Centroid = centroid,
                Normal = normal,

                // Actual force being applied.
                Force = totalForce,

                Depth = depth,
                Area = area,
                Pressure = pressure
            }
        );
    }


    private void OnDrawGizmos()
    {
        foreach (BodySimulationData simulationData in activeBodies.Values)
        {
            foreach (BodySimulationData.TriangleDebugData debug
                     in simulationData.DebugTriangles)
            {
                // Submerged triangle + centroid
                if (drawSubmergedTriangles)
                {
                    Gizmos.color = Color.cyan;

                    Gizmos.DrawLine(debug.A, debug.B);
                    Gizmos.DrawLine(debug.B, debug.C);
                    Gizmos.DrawLine(debug.C, debug.A);

                    Gizmos.DrawSphere(
                        debug.Centroid,
                        0.025f
                    );
                }

                // Surface normal
                if (drawNormals)
                {
                    Gizmos.color = Color.yellow;

                    Vector3 normalEnd =
                        debug.Centroid +
                        debug.Normal.normalized *
                        normalDebugLength;

                    Gizmos.DrawLine(
                        debug.Centroid,
                        normalEnd
                    );
                }

                // Resulting force
                if (drawForces &&
                    debug.Force.sqrMagnitude > Mathf.Epsilon)
                {
                    Gizmos.color = Color.green;

                    float forceLength =
                        Mathf.Min(
                            debug.Force.magnitude *
                            forceDebugScale,
                            maxForceDebugLength
                        );

                    Vector3 forceEnd =
                        debug.Centroid +
                        debug.Force.normalized *
                        forceLength;

                    Gizmos.DrawLine(
                        debug.Centroid,
                        forceEnd
                    );
                }
            }
        }
    }
}
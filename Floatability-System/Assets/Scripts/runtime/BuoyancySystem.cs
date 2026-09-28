using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine.PlayerLoop;

/// <summary>
/// 
/// </summary>
public class BuoyancySystem : MonoBehaviour // <- Scene instance
{
    // Private properties
    public static BuoyancySystem Instance { get; private set; }
    private readonly Dictionary<BuoyantBody, BodySimulationData> activeBodies = new();
    
    //
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (Instance != null)
        {
            return;
        }

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
    }
    
    internal void RegisterBuoyantBody(BuoyantBody body, FluidVolume fluidVolume)
    {
        if (!activeBodies.TryGetValue(body, out BodySimulationData simulationData))
        {
            simulationData = new BodySimulationData(body.Vertices.Length);
            activeBodies.Add(body, simulationData);
        }

        simulationData.Volumes.Add(fluidVolume);
    }

    internal void UnregisterBuoyantBody(BuoyantBody body, FluidVolume fluidVolume)
    {
        if (!activeBodies.TryGetValue(body, out BodySimulationData simulationData))
        {
            Debug.LogWarning($"Tried to unregister unregistered body: {body.name}");
            return;
        }

        if (!simulationData.Volumes.Remove(fluidVolume))
        {
            Debug.LogWarning($"Body {body.name} was not registered in volume {fluidVolume.name}");
            return;
        }


        if (simulationData.Volumes.Count == 0)
        {
            activeBodies.Remove(body);
        }
    }
    
    void ProcessSubmergedTriangle(BuoyantBody body, BodySimulationData simulationData, FluidVolume fluid, Vector3 a, Vector3 b, Vector3 c)
    {
        GeometryUtils.GetTriangleData(a, b, c, out float area, out Vector3 centroid, out Vector3 normal);

        if (area <= Mathf.Epsilon)
            return;

        float depth = fluid.GetDepth(centroid);
        if (depth <= 0.0f)
            return;

        // Hydrostatic force
        float pressure = fluid.Density * Physics.gravity.magnitude * depth;
        Vector3 hydrostaticForce = -normal * pressure * area;
        
        // Drag forces
        Vector3 relativeVelocity = body.Rigidbody.GetPointVelocity(centroid);
        float normalSpeed = Vector3.Dot(relativeVelocity, normal);
        Vector3 normalVelocity = normal * normalSpeed;
        
        // Tangencial drag
        Vector3 tangentialVelocity = relativeVelocity - normalVelocity;
        Vector3 tangentialDrag = Vector3.zero;
        float tangentialSpeed = tangentialVelocity.magnitude;
        
        if (tangentialSpeed > Mathf.Epsilon)
        {
            float dragMagnitude =
                0.5f *
                fluid.Density *
                1f *
                area *
                tangentialSpeed * tangentialSpeed;

            tangentialDrag = -tangentialVelocity.normalized * dragMagnitude;
        }
        
        Vector3 dragForce = Vector3.zero;
        
        if (normalSpeed > 0f)
        {
            float dragMagnitude =
                0.5f *
                fluid.Density *
                1f *
                area *
                normalSpeed * normalSpeed;

            dragForce = -normal * dragMagnitude;
        }
        
        // Total
        Vector3 totalForce = hydrostaticForce + dragForce + tangentialDrag;
        body.Rigidbody.AddForceAtPosition(totalForce, centroid,  ForceMode.Force);
        
        // DEBUG -> GIZMOS
        simulationData.DebugTriangles.Add(new BodySimulationData.TriangleDebugData
        {
            A = a,
            B = b,
            C = c,

            Centroid = centroid,
            Normal = normal,
            Force = hydrostaticForce,

            Depth = depth,
            Area = area,
            Pressure = pressure
        });
        // DEBUG -> GIZMOS
        
    }

    void FixedUpdate()
    {
        foreach (var pairBodyVolume in activeBodies)
        {
            BuoyantBody body = pairBodyVolume.Key;
            BodySimulationData simulationData = pairBodyVolume.Value;
            
            // DEBUG -> GIZMOS
            simulationData.DebugTriangles.Clear();
            // DEBUG -> GIZMOS

            // Transform each mesh vertex positions to be world space
            for (int i = 0; i < body.Vertices.Length; i++)
            {
                simulationData.WorldVertices[i] = body.transform.TransformPoint(body.Vertices[i]);
            }
            
            // Calculate the depth of each vertex respect to the fluid volume
            // so each vertex remains conceptually as follows:
            // depth > 0 -> inside || depth = 0 -> surface || depth < 0 -> outside
            foreach (var fluidVolume in simulationData.Volumes)
            {
                for (int i = 0; i < simulationData.WorldVertices.Length; i++)
                {
                    simulationData.Depths[i] = fluidVolume.GetDepth(simulationData.WorldVertices[i]);
                }
                
                for (int i = 0; i < body.Triangles.Length; i += 3)
                {
                    int ia = body.Triangles[i];
                    int ib = body.Triangles[i + 1];
                    int ic = body.Triangles[i + 2];

                    Vector3 vertexA = simulationData.WorldVertices[ia];
                    Vector3 vertexB = simulationData.WorldVertices[ib];
                    Vector3 vertexC = simulationData.WorldVertices[ic];

                    float depthA = simulationData.Depths[ia];
                    float depthB = simulationData.Depths[ib];
                    float depthC = simulationData.Depths[ic];

                    int clippedCount = GeometryUtils.ClipTriangle(
                        vertexA, vertexB, vertexC,
                        depthA, depthB, depthC,
                        simulationData.ClippedTriangle
                    );

                    switch (clippedCount)
                    {
                        case 3:
                        {
                            ProcessSubmergedTriangle(
                                body,
                                simulationData,
                                fluidVolume,
                                simulationData.ClippedTriangle[0],
                                simulationData.ClippedTriangle[1],
                                simulationData.ClippedTriangle[2]
                            );

                            break;
                        }

                        case 4:
                        {
                            ProcessSubmergedTriangle(
                                body,
                                simulationData,
                                fluidVolume,
                                simulationData.ClippedTriangle[0],
                                simulationData.ClippedTriangle[1],
                                simulationData.ClippedTriangle[2]
                            );

                            ProcessSubmergedTriangle(
                                body,
                                simulationData,
                                fluidVolume,
                                simulationData.ClippedTriangle[0],
                                simulationData.ClippedTriangle[2],
                                simulationData.ClippedTriangle[3]
                            );

                            break;
                        }
                    }
                }
            }
        }
    }

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
    
    private void OnDrawGizmos()
    {
        if (activeBodies == null)
            return;

        foreach (BodySimulationData simulationData in activeBodies.Values)
        {
            foreach (BodySimulationData.TriangleDebugData debug in simulationData.DebugTriangles)
            {
                if (drawSubmergedTriangles)
                {
                    Gizmos.color = Color.cyan;

                    Gizmos.DrawLine(debug.A, debug.B);
                    Gizmos.DrawLine(debug.B, debug.C);
                    Gizmos.DrawLine(debug.C, debug.A);

                    Gizmos.DrawSphere(debug.Centroid, 0.025f);
                }

                if (drawNormals)
                {
                    Gizmos.color = Color.yellow;

                    Vector3 normalEnd =
                        debug.Centroid +
                        debug.Normal.normalized * normalDebugLength;

                    Gizmos.DrawLine(
                        debug.Centroid,
                        normalEnd
                    );
                }

                if (drawForces && debug.Force.sqrMagnitude > Mathf.Epsilon)
                {
                    Gizmos.color = Color.green;

                    float forceLength = Mathf.Min(
                        debug.Force.magnitude * forceDebugScale,
                        maxForceDebugLength
                    );

                    Vector3 forceEnd =
                        debug.Centroid +
                        debug.Force.normalized * forceLength;

                    Gizmos.DrawLine(
                        debug.Centroid,
                        forceEnd
                    );
                }
            }
        }
    }
    
}

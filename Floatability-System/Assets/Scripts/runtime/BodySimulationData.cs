using UnityEngine;
using Unity.Collections;
using System.Collections.Generic;

internal class BodySimulationData
{
    internal readonly HashSet<FluidVolume> Volumes = new();

    internal Vector3[] WorldVertices;
    internal float[] Depths;
    internal readonly Vector3[] ClippedTriangle = new Vector3[4];

    internal BodySimulationData(int vertexCount)
    {
        WorldVertices = new Vector3[vertexCount];
        Depths = new float[vertexCount];
    }
    
    // DEBUG -> GIZMOS
    internal struct TriangleDebugData
    {
        internal Vector3 A;
        internal Vector3 B;
        internal Vector3 C;

        internal Vector3 Centroid;
        internal Vector3 Normal;
        internal Vector3 Force;

        internal float Depth;
        internal float Area;
        internal float Pressure;
    }

    internal readonly List<TriangleDebugData> DebugTriangles = new();
    // DEBUG -> GIZMOS
    
}
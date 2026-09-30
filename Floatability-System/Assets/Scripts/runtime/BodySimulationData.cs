using UnityEngine;
using System.Collections.Generic;

internal class BodySimulationData
{
    internal readonly HashSet<FluidVolume> Volumes = new();

    internal readonly Vector3[] WorldVertices;

    internal readonly Vector3[] ClipBufferA = new Vector3[12];
    internal readonly Vector3[] ClipBufferB = new Vector3[12];

    internal BodySimulationData(int vertexCount)
    {
        WorldVertices = new Vector3[vertexCount];
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
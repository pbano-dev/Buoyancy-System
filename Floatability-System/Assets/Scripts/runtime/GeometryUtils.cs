using UnityEngine;

public static class GeometryUtils
{
    /// <summary>
    /// 0 vértices -> triangle is outside
    /// 3 vértices -> triangle is inside
    /// 3 vértices -> triangle partially inside
    /// 4 vértices -> quad partially inside
    /// </summary>
    public static int ClipTriangle(Vector3 a, Vector3 b, Vector3 c, // Points of the triangle
                                    float da, float db, float dc,    // Depths of each point
                                    Vector3[] output)
    {
        Vector3[] vertices = { a, b, c };
        float[] depths = { da, db, dc };

        int outputCount = 0;

        for (int i = 0; i < 3; i++)
        {
            int next = (i + 1) % 3;

            Vector3 current = vertices[i];
            Vector3 nextVertex = vertices[next];

            float currentDepth = depths[i];
            float nextDepth = depths[next];

            bool currentInside = currentDepth >= 0f;
            bool nextInside = nextDepth >= 0f;

            if (currentInside)
                output[outputCount++] = current;

            if (currentInside != nextInside)
            {
                float t = currentDepth / (currentDepth - nextDepth);

                output[outputCount++] =
                    Vector3.Lerp(current, nextVertex, t);
            }
        }

        return outputCount;
    }
    
    public static void GetTriangleData(Vector3 a, Vector3 b, Vector3 c, 
        out float area,
        out Vector3 centroid,
        out Vector3 normal)
    {
        Vector3 cross = Vector3.Cross(b - a, c - a);

        area = cross.magnitude * 0.5f;
        normal = cross.normalized;
        centroid = (a + b + c) / 3f;
    }
    
}

using UnityEngine;

public static class GeometryUtils
{
    internal enum PlaneAxis
    {
        X,
        Y,
        Z
    }
    
    private static float GetAxisValue(Vector3 point, PlaneAxis axis)
    {
        switch (axis)
        {
            case PlaneAxis.X: return point.x;
            case PlaneAxis.Y: return point.y;
            case PlaneAxis.Z: return point.z;
            default: return 0f;
        }
    }
    
    /// <summary>
    /// 0 vértices -> triangle is outside
    /// 3 vértices -> triangle is inside
    /// 3 vértices -> triangle partially inside
    /// 4 vértices -> quad partially inside
    /// </summary>
    public static int ClipTriangle(Vector3 a, Vector3 b, Vector3 c, // Points of the triangle
                                    float da, float db, float dc,   // Depths of each point
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
    internal static int ClipTriangleAgainstBox(Vector3 a, Vector3 b, Vector3 c, Vector3 min, Vector3 max, Vector3[] bufferA, Vector3[] bufferB)
    {
        bufferA[0] = a;
        bufferA[1] = b;
        bufferA[2] = c;

        int count = 3;

        // X >= min.x
        count = ClipPolygonAgainstPlane(bufferA, count, bufferB, PlaneAxis.X, min.x, true);

        if (count == 0)
        {
            return 0;
        }

        // X <= max.x
        count = ClipPolygonAgainstPlane(bufferB, count, bufferA, PlaneAxis.X, max.x, false);

        if (count == 0)
        {
            return 0;
        }

        // Y >= min.y
        count = ClipPolygonAgainstPlane(bufferA, count, bufferB, PlaneAxis.Y, min.y, true);

        if (count == 0)
        {
            return 0;
        }

        // Y <= max.y
        count = ClipPolygonAgainstPlane(bufferB, count, bufferA, PlaneAxis.Y, max.y, false);

        if (count == 0)
        {
            return 0;
        }

        // Z >= min.z
        count = ClipPolygonAgainstPlane(bufferA, count, bufferB, PlaneAxis.Z, min.z, true);

        if (count == 0)
        {
            return 0;
        }

        // Z <= max.z
        count = ClipPolygonAgainstPlane(bufferB, count, bufferA, PlaneAxis.Z, max.z, false);
        return count;
    }
    
    private static Vector3 GetPlaneIntersection(Vector3 from, Vector3 to, float fromCoordinate, float toCoordinate, float planeValue)
    {
        float t = (planeValue - fromCoordinate) / (toCoordinate - fromCoordinate);
        return Vector3.LerpUnclamped(from, to, t);
    }
    
    internal static int ClipPolygonAgainstPlane(Vector3[] input, int inputCount, Vector3[] output, PlaneAxis axis, float planeValue, bool keepGreater)
    {
        if (inputCount == 0)
            return 0;

        int outputCount = 0;

        for (int i = 0; i < inputCount; i++)
        {
            Vector3 current = input[i];
            Vector3 next = input[(i + 1) % inputCount];

            float currentCoordinate = GetAxisValue(current, axis);
            float nextCoordinate = GetAxisValue(next, axis);

            bool currentInside = keepGreater
                ? currentCoordinate >= planeValue
                : currentCoordinate <= planeValue;

            bool nextInside = keepGreater
                ? nextCoordinate >= planeValue
                : nextCoordinate <= planeValue;

            if (currentInside && nextInside)
            {
                output[outputCount++] = next;
            }
            else if (currentInside && !nextInside)
            {
                output[outputCount++] = GetPlaneIntersection(
                    current,
                    next,
                    currentCoordinate,
                    nextCoordinate,
                    planeValue
                );
            }
            else if (!currentInside && nextInside)
            {
                output[outputCount++] = GetPlaneIntersection(
                    current,
                    next,
                    currentCoordinate,
                    nextCoordinate,
                    planeValue
                );

                output[outputCount++] = next;
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

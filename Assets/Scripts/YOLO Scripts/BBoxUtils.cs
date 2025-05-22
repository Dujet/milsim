using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public static class BBoxUtils 
{
    public static Rect GetScreenSpaceBoundingBox(GameObject obj, Camera camera)
    {
        // Get all mesh filters (including children)
        MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0) 
            return new Rect();

        // Collect all world-space vertices
        Vector3[] worldVertices = meshFilters
            .SelectMany(mf => mf.mesh.vertices.Select(v => mf.transform.TransformPoint(v)))
            .ToArray();

        // Project to screen space and filter valid points
        Vector3[] screenVertices = worldVertices
            .Select(v => camera.WorldToScreenPoint(v))
            .Where(v => v.z > 0 && IsOnScreen(v, camera))
            .ToArray();

        if (screenVertices.Length == 0)
            return new Rect();

        // Calculate bounds
        float xMin = screenVertices.Min(v => v.x);
        float xMax = screenVertices.Max(v => v.x);
        float yMin = screenVertices.Min(v => v.y);
        float yMax = screenVertices.Max(v => v.y);

        return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
    }

    private static bool IsOnScreen(Vector3 screenPos, Camera camera)
    {
        return screenPos.x >= 0 && screenPos.x <= camera.pixelWidth && 
               screenPos.y >= 0 && screenPos.y <= camera.pixelHeight;
    }
}

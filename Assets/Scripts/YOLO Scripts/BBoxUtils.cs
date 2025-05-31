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

    public static Rect ConvertYoloToUnityRect(float[] yoloBbox, int inputWidth, int inputHeight, int screenWidth, int screenHeight)
    {
        float x1 = yoloBbox[0];
        float y1 = yoloBbox[1];
        float x2 = yoloBbox[2];
        float y2 = yoloBbox[3];

        // Scale from model input to screen size
        float xScale = (float)screenWidth / inputWidth;
        float yScale = (float)screenHeight / inputHeight;

        float boxWidth = (x2 - x1) * xScale;
        float boxHeight = (y2 - y1) * yScale;

        float x = x1 * xScale;
        float y = screenHeight - (y2 * yScale); // Invert Y

        return new Rect(x, y, boxWidth, boxHeight);
    }

    public static void DrawBoundingBox(Rect bbox, Color color, Camera camera)
    {
        Vector3 topLeft = new Vector3(bbox.x, bbox.y, 5);
        Vector3 topRight = new Vector3(bbox.x + bbox.width, bbox.y, 5);
        Vector3 bottomLeft = new Vector3(bbox.x, bbox.y + bbox.height, 5);
        Vector3 bottomRight = new Vector3(bbox.x + bbox.width, bbox.y + bbox.height, 5);

        Debug.DrawLine(camera.ScreenToWorldPoint(topLeft), camera.ScreenToWorldPoint(topRight), color, 2f);
        Debug.DrawLine(camera.ScreenToWorldPoint(topRight), camera.ScreenToWorldPoint(bottomRight), color, 2f);
        Debug.DrawLine(camera.ScreenToWorldPoint(bottomRight), camera.ScreenToWorldPoint(bottomLeft), color, 2f);
        Debug.DrawLine(camera.ScreenToWorldPoint(bottomLeft), camera.ScreenToWorldPoint(topLeft), color, 2f);
    }

}

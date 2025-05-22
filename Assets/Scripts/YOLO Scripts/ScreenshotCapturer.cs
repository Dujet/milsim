using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ScreenshotCapturer : MonoBehaviour
{
    public Camera renderCamera;
    public int imageWidth = 640;
    public int imageHeight = 640;
    private RenderTexture renderTexture;
    public int frameCount = 0;

    private string imagesDir;

    // Start is called before the first frame update
    void Start()
    {
        renderTexture = new RenderTexture(imageWidth, imageHeight, 24);
        renderCamera.targetTexture = renderTexture;

        imagesDir = Path.Combine(Application.dataPath, "Dataset", "images");
        if (!Directory.Exists(imagesDir))
        {
            Directory.CreateDirectory(imagesDir);
            Debug.Log($"Created directory: {imagesDir}");
        }
    }

    IEnumerator CaptureRoutine() {
        yield return new WaitForEndOfFrame(); // Wait for all rendering to complete
        Texture2D screenshot = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.RGB24, false);
        RenderTexture.active = renderTexture;
        screenshot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
        RenderTexture.active = null;
        screenshot.Apply();

        string imagePath = Path.Combine(
            imagesDir,
            $"image_{frameCount:0000}.png");
        byte[] bytes = screenshot.EncodeToPNG(); // Or EncodeToJPG(quality)
        File.WriteAllBytes(imagePath, bytes);
        Destroy(screenshot);
        frameCount++;
    }

    public void CaptureImage() {
        StartCoroutine(CaptureRoutine());
    }
}

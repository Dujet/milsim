using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class Detection
{
    public int @class;
    public float confidence;
    public float[] bbox;
}

[Serializable]
public class DetectionResponse
{
    public Detection[] detections;
}

public class YoloFlaskClient : MonoBehaviour
{
    public string serverURL = "http://localhost:5000/detect";
    public Camera captureCamera;
    public Camera displayCamera;
    public int imageWidth = 640;
    public int imageHeight = 640;

    void Start()
    {
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(CaptureAndSendFrame());
        }
    }

    IEnumerator CaptureAndSendFrame()
    {
        // Create RenderTexture
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);
        captureCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

        // Render and read
        captureCamera.Render();
        RenderTexture.active = rt;
        screenShot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
        screenShot.Apply();
        captureCamera.targetTexture = null;
        RenderTexture.active = null;
        Destroy(rt);

        // Encode to JPG and base64
        byte[] imageBytes = screenShot.EncodeToJPG();
        string base64Image = Convert.ToBase64String(imageBytes);

        // Prepare JSON payload
        string jsonPayload = "{\"image\":\"" + base64Image + "\"}";
        byte[] jsonBytes = System.Text.Encoding.UTF8.GetBytes(jsonPayload);

        // Send POST request
        UnityWebRequest request = new UnityWebRequest(serverURL, "POST");
        request.uploadHandler = new UploadHandlerRaw(jsonBytes);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");

        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("Response: " + request.downloadHandler.text);
            DetectionResponse detections = JsonUtility.FromJson<DetectionResponse>(request.downloadHandler.text);

            foreach (var det in detections.detections)
            {
                Debug.Log($"Detected class {det.@class} with confidence {det.confidence} at bbox {string.Join(",", det.bbox)}");
                BBoxUtils.DrawBoundingBox(
                    BBoxUtils.ConvertYoloToUnityRect(det.bbox, imageWidth, imageHeight, Screen.width, Screen.height),
                    Color.red,
                    displayCamera
                );
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }

        Destroy(screenShot);
    }
}

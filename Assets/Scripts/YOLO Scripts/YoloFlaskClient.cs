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
    [SerializeField] private LayerMask layerMask;
    [SerializeField] private HUDMarkerController hudMarkerController;
    [SerializeField] private float detectionInterval = 1f;

    void Start()
    {
        if (layerMask == 0) Debug.LogWarning("LayerMask is not set.");

        StartCoroutine(StartDetectionCoroutine(detectionInterval));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(CaptureAndSendFrame());
        }
    }

    IEnumerator StartDetectionCoroutine(float interval)
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            yield return CaptureAndSendFrame();
        }
    }

    IEnumerator CaptureAndSendFrame()
    {
        // Create RenderTexture
        RenderTexture rt = new RenderTexture(imageWidth, imageHeight, 24);
        captureCamera.targetTexture = rt;
        Texture2D screenShot = new Texture2D(imageWidth, imageHeight, TextureFormat.RGB24, false);

        captureCamera.fieldOfView = displayCamera.fieldOfView; // Match FOV with display camera

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
            //Debug.Log("Response: " + request.downloadHandler.text);
            DetectionResponse detections = JsonUtility.FromJson<DetectionResponse>(request.downloadHandler.text);

            foreach (var det in detections.detections)
            {
                Debug.Log($"Detected class {det.@class} with confidence {det.confidence} at bbox {string.Join(",", det.bbox)}");
                Rect convertedRect = BBoxUtils.ConvertYoloToUnityRect(det.bbox, imageWidth, imageHeight, Screen.width, Screen.height);
                /* BBoxUtils.DrawBoundingBox(
                    convertedRect,
                    det.@class == 0 ? Color.red : Color.gray,
                    displayCamera
                ); */
                ConfirmObjectDetection(convertedRect, det.@class);
            }
        }
        else
        {
            Debug.LogError("Error: " + request.error);
        }

        Destroy(screenShot);
    }

    private void ConfirmObjectDetection(Rect bbox, int classId)
    {
        Vector3 screenPoint = new Vector3(bbox.center.x, bbox.center.y, 0f);
        Ray ray = displayCamera.ScreenPointToRay(screenPoint);
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 2f);
        if (Physics.Raycast(ray, out RaycastHit hit, layerMask))
        {
            HandleDetectedObject(hit.transform.root, classId);
            Debug.DrawLine(ray.origin, hit.point, Color.green, 2f);
        }
    }

    private void HandleDetectedObject(Transform detectedObject, int classId)
    {
        if (!(detectedObject.CompareTag("Tank") || detectedObject.CompareTag("Truck")))
        {
            Debug.LogWarning($"Raycast hit object {detectedObject.name} is not a tank or truck. Tag: {detectedObject.tag}");
            return;
        }

        switch (classId)
        {
            case 0:
                Debug.Log($"Raycast hit object of class 0: {detectedObject.name}");
                hudMarkerController.AddTargetMarker(detectedObject, Color.red);
                break;
            case 1:
                Debug.Log($"Raycast hit object of class 1: {detectedObject.name}");
                hudMarkerController.AddTargetMarker(detectedObject, Color.gray);
                break;
            default:
                Debug.Log($"Raycast hit object of unknown class {classId}: {detectedObject.name}");
                break;
        }
    }
}

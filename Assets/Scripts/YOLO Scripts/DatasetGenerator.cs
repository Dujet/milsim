using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class DatasetGenerator : MonoBehaviour
{

    public GameObject objectToSpawn;
    //private Transform turretTransform;
    //private Transform barrelTransform;
    private GameObject spawnedObject;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Transform sunTransform;
    public GameObject[] objectsToSpawn;
    List<GameObject> spawnedObjects = new();

    public int samples = 1000;
    public float delay = 0.1f;
    public string imagePrefix = "image_";
    //public string labelPrefix = "label_"; // should use the same prefix as image (oops)
    public string imageFormat = ".jpg";
    public int index = 0;

    public int Index { get => index; private set => index = value; }

    // The range of the random position
    public float minX = 0f;
    public float maxX = 1000f;
    public float minY = 0;
    public float maxY = 1000f;
    public float height = 150f;
    public float margin = 10f;
    public float maxRayDistance = 150f;
    public LayerMask layerMask;
    public ScreenshotCapturer screenshotCapturer;

    // Start is called before the first frame update
    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        if (screenshotCapturer == null) screenshotCapturer = GetComponent<ScreenshotCapturer>();
        if (!Directory.Exists(Path.Combine(Application.dataPath, "Dataset", "labels")))
        {
            Directory.CreateDirectory(Path.Combine(Application.dataPath, "Dataset", "labels"));
            Debug.Log($"Created directory: {Path.Combine(Application.dataPath, "Dataset", "labels")}");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            RandomizeScene(objectsToSpawn);
            screenshotCapturer.CaptureImage();
        }
        if (Input.GetKeyDown(KeyCode.T))
        {
            StartCoroutine(RandomizeSceneXTimes(samples, objectsToSpawn, delay));
        }
    }

    // TODO: convert this to handle multiple objects
    void RandomizeScene()
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
        Vector3 raycastOrigin = new Vector3(Random.Range(minX + margin, maxX - margin), height, Random.Range(minY + margin, maxY - margin));
        RaycastHit[] hits = Physics.RaycastAll(raycastOrigin, Vector3.down, maxRayDistance, layerMask);
        if (hits.Length == 0)
        {
            Debug.Log("Spawn Raycast from" + raycastOrigin + "did not hit any object.");
            Debug.DrawRay(raycastOrigin, Vector3.down * maxRayDistance, Color.red, 10f);
            return;
        }
        RaycastHit hit = hits.OrderBy(h => h.transform.position.y).First();
        if (hit.collider == null) return;
        Debug.Log("Spawn Raycast from" + raycastOrigin + "hit point " + hit.point);
        Debug.DrawLine(raycastOrigin, hit.point, Color.green, 10f);

        Vector3 spawnPosition = hit.point;
        Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);

        if (spawnedObject != null)
        {
            Destroy(spawnedObject);
        }
        spawnedObject = Instantiate(objectToSpawn, spawnPosition, spawnRotation);

        // Randomize the rotation of the spawned object
        Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
        spawnedObject.transform.rotation *= randomRotation;
        /*turretTransform = spawnedObject.transform.Find("Turret");
        if (turretTransform != null)
        {
            turretTransform.rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
        }
        barrelTransform = spawnedObject.transform.Find("Barrel");
        if (barrelTransform != null)
        {
            barrelTransform.rotation *= Quaternion.Euler(Random.Range(-5, 25), 0, 0);
        }
        */

        // Randomize the camera position and rotation
        mainCamera.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, spawnPosition.z);
        Vector3 cameraOffset = new Vector3(Random.Range(-30, 30), Random.Range(5, 30), Random.Range(-30, 30));
        mainCamera.transform.position += cameraOffset;
        mainCamera.transform.LookAt(spawnPosition);
        Quaternion cameraRandomRotation = Quaternion.Euler(Random.Range(-10, 10), Random.Range(-10, 10), 0);
        mainCamera.transform.rotation *= cameraRandomRotation;

        // Set the sun direction
        if (sunTransform != null)
        {
            sunTransform.rotation = Quaternion.Euler(new Vector3(Random.Range(10, 170), Random.Range(10, 170), 0));
        }

        bool visible = ObjectIsUnobscured(spawnedObject, mainCamera);
        Rect bbox = BBoxUtils.GetScreenSpaceBoundingBox(spawnedObject, mainCamera);
        BBoxUtils.DrawBoundingBox(bbox, visible ? Color.green : Color.red, mainCamera);
    }

    void RandomizeScene(GameObject[] objectsToSpawn)
    {
        Random.InitState((int)System.DateTime.Now.Ticks);
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        spawnedObjects.Clear();

        Vector3 raycastOriginSpherePosition = new Vector3(Random.Range(minX + margin, maxX - margin), height, Random.Range(minY + margin, maxY - margin));
        for (int i = 0; i < objectsToSpawn.Length; i++)
        {
            if (objectsToSpawn[i] == null) continue;

            bool overlapsOtherObjects = true;
            Vector3 spawnPosition = Vector3.zero;
            Quaternion spawnRotation = Quaternion.identity;
            while (overlapsOtherObjects)
            {
                Vector3 raycastOrigin = Random.onUnitSphere * Random.Range(5, 10) + raycastOriginSpherePosition;
                raycastOrigin.y = height;
                RaycastHit[] hits = Physics.RaycastAll(raycastOrigin, Vector3.down, maxRayDistance, layerMask);
                if (hits.Length == 0)
                {
                    Debug.Log("Spawn Raycast from" + raycastOrigin + "did not hit any object.");
                    //Debug.DrawRay(raycastOrigin, Vector3.down * maxRayDistance, Color.red, 10f);
                    return;
                }
                RaycastHit hit = hits.OrderBy(h => h.transform.position.y).First();
                if (hit.collider == null) return;
                //Debug.Log("Spawn Raycast from" + raycastOrigin + "hit point " + hit.point);
                //Debug.DrawLine(raycastOrigin, hit.point, Color.green, 10f);

                spawnPosition = hit.point;
                spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Quaternion randomRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                spawnRotation *= randomRotation;

                // Check if the new position overlaps with any existing objects
                Collider[] colliders = Physics.OverlapSphere(spawnPosition, 5f);
                overlapsOtherObjects = colliders.Any(c => c.gameObject != objectsToSpawn[i] && spawnedObjects.Contains(c.gameObject));
                if (overlapsOtherObjects)
                {
                    Debug.Log("Spawn position overlaps with another object. Trying again...");
                }
            }

            GameObject spawnedObject = Instantiate(objectsToSpawn[i], spawnPosition, spawnRotation);
            spawnedObjects.Add(spawnedObject);


            // Randomize the rotation of the spawned object
            Transform turretTransform = spawnedObject.transform.Find("Turret");
            if (turretTransform != null)
            {
                turretTransform.rotation *= Quaternion.Euler(0, Random.Range(0, 360), 0);
            }
            Transform barrelTransform = spawnedObject.transform.Find("Barrel");
            if (barrelTransform != null)
            {
                barrelTransform.rotation *= Quaternion.Euler(Random.Range(-5, 25), 0, 0);
            }
        }

        Vector3 avgSpawnPosition = Vector3.zero;
        foreach (GameObject obj in spawnedObjects)
        {
            if (obj != null)
            {
                avgSpawnPosition += obj.transform.position;
            }
        }
        avgSpawnPosition /= spawnedObjects.Count;

        // Randomize the camera position and rotation
        mainCamera.transform.position = new Vector3(avgSpawnPosition.x, avgSpawnPosition.y, avgSpawnPosition.z);
        Vector3 cameraOffset = new Vector3(Random.Range(-30, 30), Random.Range(5, 30), Random.Range(-30, 30));
        mainCamera.transform.position += cameraOffset;

        // check if camera is under the ground
        if (!Physics.Raycast(mainCamera.transform.position, Vector3.down, maxRayDistance, layerMask))
        {
            Debug.Log("Camera is under the ground. Moving camera up.");
            mainCamera.transform.position += new Vector3(0, Random.Range(10, 40), 0);
        }
        if (Vector3.Distance(mainCamera.transform.position, avgSpawnPosition) < 10)
        {
            Debug.Log("Camera is too close to the objects. Moving camera back.");
            Vector3 cameraDirection = (mainCamera.transform.position - avgSpawnPosition).normalized;
            mainCamera.transform.position += cameraDirection * Random.Range(10, 20);
        }

        mainCamera.transform.LookAt(avgSpawnPosition);
        Quaternion cameraRandomRotation = Quaternion.Euler(Random.Range(-10, 10), Random.Range(-10, 10), 0);
        mainCamera.transform.rotation *= cameraRandomRotation;

        // Set the sun direction
        if (sunTransform != null)
        {
            sunTransform.rotation = Quaternion.Euler(new Vector3(Random.Range(10, 170), Random.Range(10, 170), 0));
        }

        List<string> labels = new List<string>();
        // Draw bounding boxes for each spawned object
        foreach (GameObject obj in spawnedObjects)
        {
            bool visible = ObjectIsUnobscured(obj, mainCamera);
            Rect bbox = BBoxUtils.GetScreenSpaceBoundingBox(obj, mainCamera);
            BBoxUtils.DrawBoundingBox(bbox, visible ? Color.green : Color.red, mainCamera);

            if (visible)
            {
                string label = GenerateLabel(obj, bbox);
                labels.Add(label);
            }
        }

        // Save the label file
        SaveLabelFile(labels);
    }

    private string GenerateLabel(GameObject obj, Rect bbox)
    {
        // Convert to YOLO format (top-left origin)
        float xCenter = bbox.center.x / Screen.width;  // X is consistent
        float width = bbox.width / Screen.width;

        // Y-axis requires flipping min/max FIRST
        float yMin_YOLO = 1 - (bbox.yMax / Screen.height);
        float yMax_YOLO = 1 - (bbox.yMin / Screen.height);
        float yCenter = (yMin_YOLO + yMax_YOLO) / 2f;
        float height = (yMax_YOLO - yMin_YOLO);


        string className = null;
        switch (obj.tag)
        {
            case "Tank":
                className = "0";
                break;
            case "Truck":
                className = "1";
                break;
            default:
                Debug.LogWarning($"Unknown object tag: {obj.tag}");
                return null;
        }

        string label = $"{className} {xCenter} {yCenter} {width} {height}";
        Debug.Log($"Label for {obj.name}: {label}");
        return label;
    }

    private void SaveLabelFile(List<string> labels)
    {
        string labelFileName = $"{imagePrefix}{index:0000}.txt";
        string labelFilePath = Path.Combine(Application.dataPath, "Dataset", "labels", labelFileName);
        File.WriteAllLines(labelFilePath, labels);
        Debug.Log($"Label file saved: {labelFilePath}");
        index++;
    }

    private bool ObjectIsUnobscured(GameObject obj, Camera camera)
    {
        Transform[] objTransforms = obj.GetComponentsInChildren<Transform>();
        foreach (Transform objTransform in objTransforms)
        {
            Vector3 objectScreenPos = camera.WorldToScreenPoint(objTransform.position);
            Ray ray = camera.ScreenPointToRay(objectScreenPos);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxRayDistance))
            {
                Debug.DrawLine(ray.origin, hit.point, Color.red, 5f);
                if (hit.transform.IsChildOf(obj.transform)) return true;
            }
        }
        return false;
    }

    IEnumerator RandomizeSceneXTimes(int times, GameObject[] objectsToSpawn, float delay = 0.2f)
    {
        for (int i = 0; i < times; i++)
        {
            RandomizeScene(objectsToSpawn);
            screenshotCapturer.CaptureImage();
            yield return new WaitForSeconds(delay);
        }
    }

    
}

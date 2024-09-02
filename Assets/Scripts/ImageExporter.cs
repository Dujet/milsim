using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ImageExporter : MonoBehaviour
{
    private Canvas _canvasHUD;

    void Start()
    {
        _canvasHUD = GameObject.Find("DroneHUD").GetComponent<Canvas>();
    }


    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.K)) {
            StartCoroutine(TakeScreenshotAndSave());
        }
    }

    private IEnumerator TakeScreenshotAndSave() {
        yield return null;

        string path = Application.dataPath + "/Screenshots";
        if (!System.IO.Directory.Exists(path)) {
            System.IO.Directory.CreateDirectory(path);
        }

        string filename = path + "/screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".png";

        _canvasHUD.enabled = false;
        yield return new WaitForEndOfFrame();

        
        ScreenCapture.CaptureScreenshot(filename);
        _canvasHUD.enabled = true;
    }
}

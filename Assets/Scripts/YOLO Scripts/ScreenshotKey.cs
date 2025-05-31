using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenshotKey : MonoBehaviour
{
    [SerializeField] private ScreenshotCapturer screenshotCapturer;
    [SerializeField] private KeyCode screenshotKey = KeyCode.P;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(screenshotKey))
        {
            if (screenshotCapturer != null)
            {
                screenshotCapturer.CaptureImage();
                Debug.Log("Screenshot captured.");
            }
            else
            {
                Debug.LogWarning("ScreenshotCapturer is not assigned.");
            }
        }
    }
}

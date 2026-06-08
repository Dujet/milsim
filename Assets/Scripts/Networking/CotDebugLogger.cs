using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CotDebugLogger : MonoBehaviour
{
    public CotReceiver cotReceiver;

    void Awake()
    {
        cotReceiver.OnAnyEventReceived += evt =>
            Debug.Log($"[CotDebug] {evt}");
    }
}

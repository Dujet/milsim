using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitHighlight : MonoBehaviour
{
    private UnitSpotter _otherDroneCamera;

    // TODO: rename class to something more appropriate
    public void Init(Transform target, UnitSpotter otherDroneCamera) {
        _otherDroneCamera = otherDroneCamera;
    }

    private void OnDestroy()
    {
        _otherDroneCamera.EnableCamera();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UnitHighlight : MonoBehaviour
{
    [SerializeField] private RawImage _targetMarkerPrefab;
    [SerializeField] private Camera _camera;
    private Transform _target;
    private UnitSpotter _otherDroneCamera;
    private RawImage _targetMarker;
    [SerializeField] private Canvas _canvas;

    public void Init(Transform target, UnitSpotter otherDroneCamera) {
        _target = target;
        _otherDroneCamera = otherDroneCamera;

        return; // debug

        _targetMarker = Instantiate(_targetMarkerPrefab, _canvas.transform);
        _targetMarker.color = Color.red;
        
        Canvas markerCanvas = _targetMarker.GetComponent<Canvas>();
        if (markerCanvas == null) {
            markerCanvas = _targetMarker.gameObject.AddComponent<Canvas>();
        }
        markerCanvas.overrideSorting = true;
        markerCanvas.sortingOrder = 2;
    }

    void Awake() {
        if (_canvas == null) _canvas = FindObjectOfType<Canvas>();
    }

    void Update() {
        if (_target == null) return;

        return; // debug
        // Check if target is behind camera
        Vector3 direction = (_target.position - Camera.main.transform.position).normalized;
        bool isBehind = Vector3.Dot(direction, Camera.main.transform.forward) <= 0;
        _targetMarker.enabled = !isBehind;

        Vector3 screenPos = _camera.WorldToScreenPoint(_target.position);
        _targetMarker.transform.position = screenPos;
    }


    private void OnDisable()
    {
        _otherDroneCamera.EnableCamera();
    }

    private void OnDestroy()
    {
        _otherDroneCamera.EnableCamera();
        Destroy(_targetMarker.gameObject);
    }
}

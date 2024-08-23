using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitHighlight : MonoBehaviour
{
    [SerializeField] private Material _highlightMaterial;
    [SerializeField] private Camera _camera;
    private Material _defaultMaterial;
    private Renderer _renderer;
    private UnitSpotter _otherDroneCamera;

    void Update() {
        if (_renderer == null) return;
        //DrawSquare();
    }

    public void Init(Renderer renderer, UnitSpotter otherDroneCamera) {
        _renderer = renderer;
        _defaultMaterial = _renderer.material;
        _otherDroneCamera = otherDroneCamera;
        Highlight();
    }

    public void Highlight()
    {
        _defaultMaterial = _renderer.material;
        _renderer.material = _highlightMaterial;
    }

    public void RemoveHighlight()
    {
        if (_renderer == null) return;
        _renderer.material = _defaultMaterial;
    }

    private void OnDisable()
    {
        RemoveHighlight();
        _otherDroneCamera.EnableCamera();
    }

    private void OnDestroy()
    {
        RemoveHighlight();
        _otherDroneCamera.EnableCamera();
    }

    // draw a UI square around the target unit
    void OnDrawGizmos() {
        if (_renderer == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(_renderer.bounds.center, _renderer.bounds.size*2);
    }
}

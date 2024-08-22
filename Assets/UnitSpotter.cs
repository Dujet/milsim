using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitSpotter : MonoBehaviour
{
    [SerializeField] private FieldOfView _fieldOfView;
    private List<Transform> _visibleTargets = new List<Transform>();
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private Camera _camera;


    void Start()
    {
        _visibleTargets = _fieldOfView.visibleTargets;
    }

    // Update is called once per frame
    void Update()
    {
        _fieldOfView.viewAngle = _camera.fieldOfView;
    }

    void OnDrawGizmos() {
        if (!_showDebugGizmos) return;
        Gizmos.color = Color.red;
        foreach (Transform target in _visibleTargets) {
            Gizmos.DrawCube(target.position, Vector3.one*3);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitSpotter : MonoBehaviour
{
    [SerializeField] private FieldOfView _fieldOfView;
    private List<Transform> _visibleTargets = new List<Transform>();
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private Camera _camera;
    [SerializeField] private string _targetTag = "WARSAW";
    [SerializeField] private UnitHighlight _assaultDronePrefab;
    private DroneCamera _droneCamera;

    void Awake() {
        _droneCamera = GetComponent<DroneCamera>();
    }


    void Start()
    {
        _visibleTargets = _fieldOfView.visibleTargets;
    }

    // Update is called once per frame
    void Update()
    {
        _fieldOfView.viewAngle = _camera.fieldOfView;

        if (Input.GetMouseButtonDown(0)) {
            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.collider.CompareTag(_targetTag)) {
                    //Transform target = hit.collider.transform;
                    //if (_visibleTargets.Contains(target)) {
                        UnitHighlight unitHighlight = Instantiate(_assaultDronePrefab, transform.position - Vector3.up*5, Quaternion.identity);
                        unitHighlight.Init(hit.transform.GetComponentInChildren<Renderer>(), this);
                        _droneCamera.DisableCamera();
                        GetComponent<PlayerInput>().enabled = false;
                        GetComponent<Drone_Inputs>().enabled = false;
                    //}
                }
            }
        }
    }

    void OnDrawGizmos() {
        if (!_showDebugGizmos) return;
        Gizmos.color = Color.red;
        foreach (Transform target in _visibleTargets) {
            Gizmos.DrawCube(target.position, Vector3.one*3);
        }
    }

    public void EnableCamera() {
        _droneCamera.EnableCamera();
        GetComponent<PlayerInput>().enabled = true;
        GetComponent<Drone_Inputs>().enabled = true;
    }
}

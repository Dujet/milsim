using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitSpotter : MonoBehaviour
{
    [SerializeField] private FieldOfView _fieldOfView;
    private List<Transform> _visibleTargets = new List<Transform>();
    [SerializeField] private bool _showDebugGizmos = true;
    [SerializeField] private Camera _camera;
    [SerializeField] private Faction _faction = Faction.NATO;
    [SerializeField] private string _targetTag = "WARSAW";
    [SerializeField] private UnitHighlight _assaultDronePrefab;
    private DroneCamera _droneCamera;
    private List<Transform> _friendlyUnits;
    public static Action<Transform> OnTargetSelected;

    void Awake() {
        _droneCamera = GetComponent<DroneCamera>();
    }


    void Start()
    {
        _visibleTargets = _fieldOfView.visibleTargets;
        _friendlyUnits = new List<Transform>(GameObject.FindGameObjectsWithTag(_faction.ToString()).Select(go => go.transform));
    }

    // Update is called once per frame
    void Update()
    {
        _fieldOfView.viewAngle = _camera.fieldOfView;

        if (Input.GetKeyDown(KeyCode.Space)) {
            Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width/2, Screen.height/2, 0));
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit)) {
                if (hit.collider.CompareTag(_targetTag)) {
                    //Transform target = hit.collider.transform;
                    //if (_visibleTargets.Contains(target)) {
                        SpawnAssaultDrone(hit);
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
        _droneCamera.enabled = true;
        GetComponent<PlayerInput>().enabled = true;
        GetComponent<Drone_Inputs>().enabled = true;
    }

    private void SpawnAssaultDrone(RaycastHit hit) {
        Transform selectedFriendly = null;
        foreach (Transform friendly in _friendlyUnits) {
            if (Vector3.Distance(friendly.position, hit.point) < 500) {
                selectedFriendly = friendly;
                break;
            }
        }
        if (selectedFriendly == null) return;

        OnTargetSelected?.Invoke(hit.transform);

        UnitHighlight unitHighlight = Instantiate(_assaultDronePrefab, 
            selectedFriendly.position + Vector3.up*5, Quaternion.LookRotation(hit.point - selectedFriendly.position));
        unitHighlight.Init(hit.transform, this);
        _droneCamera.enabled = false;
        GetComponent<PlayerInput>().enabled = false;
        GetComponent<Drone_Inputs>().enabled = false;
    }
}

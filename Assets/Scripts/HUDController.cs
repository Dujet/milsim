using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Android;
using UnityEngine.UI;
using System.Linq;
using System;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _targetMarkerPrefab;
    [SerializeField] private FieldOfView _fieldOfView;
    private Dictionary<Transform, GameObject> _targetMarkers = new Dictionary<Transform, GameObject>();
    private Transform _selectedTarget;
    private Dictionary<Transform, GameObject> _friendlyMarkers;
    //private List<Transform> _targets;

    void Awake() {
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        _fieldOfView.OnVisibleTargetsChanged += UpdateTargetMarkers;
        _friendlyMarkers = GameObject.FindGameObjectsWithTag("NATO").Select(go => go.transform).ToDictionary(go => go, 
            go => {
            GameObject marker = Instantiate(_targetMarkerPrefab, _canvas.transform);
            marker.GetComponent<RawImage>().color = Color.green;
            return marker;
            });

        UpdateTargetMarkers();
    }

    void OnDestroy() {
        _fieldOfView.OnVisibleTargetsChanged -= UpdateTargetMarkers;
    }

    void OnEnable() {
        UnitSpotter.OnTargetSelected += ChangeMarkerColor;
    }

    void OnDisable() {
        UnitSpotter.OnTargetSelected -= ChangeMarkerColor;
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var targetMarker in _targetMarkers) {
            DisableIfBehindCamera(targetMarker.Key, targetMarker.Value);
            targetMarker.Value.transform.position = Camera.main.WorldToScreenPoint(targetMarker.Key.position);
        }

        foreach (var friendlyMarker in _friendlyMarkers) {
            DisableIfBehindCamera(friendlyMarker.Key, friendlyMarker.Value);
            friendlyMarker.Value.transform.position = Camera.main.WorldToScreenPoint(friendlyMarker.Key.position);
        }
    }

    // TODO: fix selected target marker dissapearing when out of FOV - create marker for selected target
    private void UpdateTargetMarkers() {
        if (_fieldOfView.visibleTargets == null) return;

        foreach (var targetMarker in _targetMarkers.Values) {
            Destroy(targetMarker);
        }
        _targetMarkers.Clear();

        //_fieldOfView.visibleTargets = new List<Transform>(_fieldOfView.visibleTargets);

        foreach (var target in _fieldOfView.visibleTargets) {
            if (target == null) continue;
            GameObject targetMarker = Instantiate(_targetMarkerPrefab, _canvas.transform);
            DisableIfBehindCamera(target, targetMarker);

            if (target == _selectedTarget) {
                targetMarker.GetComponent<RawImage>().color = Color.red;
            }

            // Update target marker position
            targetMarker.transform.position = Camera.main.WorldToScreenPoint(target.position);
            _targetMarkers[target] = targetMarker;
        }

        if (_selectedTarget != null && !_targetMarkers.ContainsKey(_selectedTarget) && !_selectedTarget.GetComponent<Health>().IsDead) {
            GameObject targetMarker = Instantiate(_targetMarkerPrefab, _canvas.transform);
            DisableIfBehindCamera(_selectedTarget, targetMarker);
            targetMarker.GetComponent<RawImage>().color = Color.red;
            targetMarker.transform.position = Camera.main.WorldToScreenPoint(_selectedTarget.position);
            _targetMarkers[_selectedTarget] = targetMarker;
        }
    }

    private void ChangeMarkerColor(Transform target)
    {
        if (_targetMarkers.TryGetValue(target, out GameObject targetMarker)) {
            _selectedTarget = target;
            RawImage image = targetMarker.GetComponent<RawImage>();
            image.color = Color.red;
        }
    }

    private void DisableIfBehindCamera(Transform target, GameObject targetMarker) {
        Vector3 direction = (target.position - Camera.main.transform.position).normalized;
            bool isBehind = Vector3.Dot(direction, Camera.main.transform.forward) <= 0;
            targetMarker.GetComponent<RawImage>().enabled = !isBehind;
    }
}

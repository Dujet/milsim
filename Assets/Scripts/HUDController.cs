using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _targetMarkerPrefab;
    [SerializeField] private FieldOfView _fieldOfView;
    [SerializeField] private Camera _camera;
    private List<GameObject> _targetMarkers = new List<GameObject>();
    private List<Transform> _targets;

    
    // Start is called before the first frame update
    void Start()
    {
        _fieldOfView.OnVisibleTargetsChanged += UpdateTargetMarkers;
        UpdateTargetMarkers();
    }

    void OnDestroy() {
        _fieldOfView.OnVisibleTargetsChanged -= UpdateTargetMarkers;
    }

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < _targets.Count; i++)
        {
            if (_targets[i] != null)
            {
                Vector3 screenPos = _camera.WorldToScreenPoint(_targets[i].position);
                _targetMarkers[i].transform.position = screenPos;
            }
        }
    }

    private void UpdateTargetMarkers() {
        foreach (var targetMarker in _targetMarkers) {
            Destroy(targetMarker);
        }
        _targetMarkers.Clear();

        _targets = new List<Transform>(_fieldOfView.visibleTargets);

        foreach (var target in _targets) {
            GameObject targetMarker = Instantiate(_targetMarkerPrefab, _canvas.transform);
            targetMarker.transform.position = _camera.WorldToScreenPoint(target.position);
            _targetMarkers.Add(targetMarker);
        }
    }
}

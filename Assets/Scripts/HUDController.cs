using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _targetMarkerPrefab;
    [SerializeField] private FieldOfView _fieldOfView;
    private List<GameObject> _targetMarkers = new List<GameObject>();
    //private List<Transform> _targets;

    
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
        for (int i = 0; i < _fieldOfView.visibleTargets.Count; i++)
        {
            if (_fieldOfView.visibleTargets[i] != null)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(_fieldOfView.visibleTargets[i].position);
                _targetMarkers[i].transform.position = screenPos;
            }
        }
    }

    private void UpdateTargetMarkers() {
        foreach (var targetMarker in _targetMarkers) {
            Destroy(targetMarker);
        }
        _targetMarkers.Clear();

        _fieldOfView.visibleTargets = new List<Transform>(_fieldOfView.visibleTargets);

        foreach (var target in _fieldOfView.visibleTargets) {
            GameObject targetMarker = Instantiate(_targetMarkerPrefab, _canvas.transform);

            // Check if target is behind camera
            Vector3 direction = (target.position - Camera.main.transform.position).normalized;
            bool isBehind = Vector3.Dot(direction, Camera.main.transform.forward) <= 0;
            targetMarker.GetComponent<RawImage>().enabled = !isBehind;

            // Update target marker position
            targetMarker.transform.position = Camera.main.WorldToScreenPoint(target.position);
            _targetMarkers.Add(targetMarker);
        }
    }
}

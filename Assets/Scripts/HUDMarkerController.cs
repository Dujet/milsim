using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDMarkerController : MonoBehaviour
{
    Dictionary<Transform, GameObject> _targetMarkers = new Dictionary<Transform, GameObject>();
    [SerializeField] private GameObject _canvas;
    [SerializeField] private GameObject _targetMarkerPrefab;

    // Start is called before the first frame update
    void Start()
    {
        if (_canvas == null)
        {
            _canvas = GameObject.FindGameObjectWithTag("HUDCanvas");
            if (_canvas == null)
            {
                Debug.LogError("HUD Canvas not found! Please assign it in the inspector.");
            }
        }

        if (_targetMarkerPrefab == null)
        {
            Debug.LogError("Target Marker Prefab not assigned! Please assign it in the inspector.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        foreach (var targetMarker in _targetMarkers)
        {
            HUDController.DisableIfBehindCamera(targetMarker.Key, targetMarker.Value);
            UpdateMarkerPosition(targetMarker.Key, targetMarker.Value);
        }
    }

    public void AddTargetMarker(Transform target, Color color)
    {
        if (!_targetMarkers.ContainsKey(target))
        {
            GameObject marker = Instantiate(_targetMarkerPrefab, _canvas.transform);
            marker.GetComponent<RawImage>().color = color;
            _targetMarkers[target] = marker;
        }
    }

    public void RemoveTargetMarker(Transform target)
    {
        if (_targetMarkers.ContainsKey(target))
        {
            Destroy(_targetMarkers[target]);
            _targetMarkers.Remove(target);
        }
    }

    public void ClearAllMarkers()
    {
        foreach (var marker in _targetMarkers.Values)
        {
            Destroy(marker);
        }
        _targetMarkers.Clear();
    }

    private void UpdateMarkerPosition(Transform target, GameObject marker)
    {
        if (target == null || marker == null)
        {
            Debug.LogWarning("Target or marker is null. Cannot update marker position.");
            return;
        }

        Vector3 screenPosition = Camera.main.WorldToScreenPoint(target.position);
        marker.transform.position = screenPosition;
    }

    public void SetMarkerColor(Transform target, Color color)
    {
        if (_targetMarkers.ContainsKey(target))
        {
            _targetMarkers[target].GetComponent<RawImage>().color = color;
        }
        else
        {
            Debug.LogWarning($"No marker found for target {target.name}. Cannot set color.");
        }

    }
}

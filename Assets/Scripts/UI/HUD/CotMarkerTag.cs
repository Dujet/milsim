using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;

public class CotMarkerTag : MonoBehaviour
{
    private CotKind _kind;
    private string _callsign;
    private Texture2D _markerTexture;

    private GameObject _hudMarker;

    [SerializeField] private GameObject _friendlyMarkerPrefab;
    [SerializeField] private GameObject _hostileMarkerPrefab;
    [SerializeField] private GameObject _neutralMarkerPrefab;
    [SerializeField] private GameObject _unknownMarkerPrefab;

    private TextMeshProUGUI _markerCallsign;
    private TextMeshProUGUI _markerDistance;

    void Update() // TODO: make the marker HUD element stick to screen edges if the marker is off-screen 
    {
        if (_hudMarker == null) return;

        // Update HUD marker position based on the world position of the Cot marker
        float distance = Vector3.Distance(Camera.main.transform.position, transform.position);
        Vector3 adjustedPos = transform.position + (Vector3.up * (float)Math.Min(distance * 0.2, 50));
        Vector3 screenPos = Camera.main.WorldToScreenPoint(adjustedPos);

        float clampedX = Mathf.Clamp(screenPos.x, 40, Screen.width - 40);
        float clampedY = Mathf.Clamp(screenPos.y, 40, Screen.height - 40);
        screenPos = new Vector3(clampedX, clampedY, screenPos.z);

        _hudMarker.transform.position = screenPos;
        // Optionally, update distance text if the marker has a callsign
        if (_markerDistance != null)
        {
            _markerDistance.text = $"{distance:F0} m";
        }

        // Optionally, hide the HUD marker if the Cot marker is behind the camera
        if (screenPos.z < 0)
        {
            _hudMarker.SetActive(false);
        }
        else
        {
            _hudMarker.SetActive(true);
        }
    }

    public void Populate(CotInboundEvent evt)
    {
        _kind = evt.Kind;
        _callsign = evt.Callsign;

        //Debug.Log($"[CotMarkerTag] Populating marker tag for '{_callsign}' of kind '{_kind}'");

        if (_hudMarker == null)
        {
            SpawnHUDMarker(_callsign);
        }
    }

    public void SpawnHUDMarker(string callsign)
    {
        if (_hudMarker != null) return; // Already spawned

        InstantiateMarkerPrefab();
        _hudMarker.transform.SetParent(HUDController.Instance.Canvas.transform, false);
        _hudMarker.transform.position = Camera.main.WorldToScreenPoint(transform.position);
    }

    private void InstantiateMarkerPrefab()
    {
        switch (_kind)
        {
            case CotKind.FriendlyAtom:
                _hudMarker = Instantiate(_friendlyMarkerPrefab);
                break;
            case CotKind.HostileAtom:
                _hudMarker = Instantiate(_hostileMarkerPrefab);
                break;
            case CotKind.NeutralAtom:
                _hudMarker = Instantiate(_neutralMarkerPrefab);
                break;
            default:
                _hudMarker = Instantiate(_unknownMarkerPrefab);
                break;
        }


        var allTexts = _hudMarker.GetComponentsInChildren<TextMeshProUGUI>();
        _markerCallsign = allTexts.FirstOrDefault(t => t.gameObject.name.Contains("Callsign"));
        _markerDistance = allTexts.FirstOrDefault(t => t.gameObject.name.Contains("Distance"));
        if (_markerCallsign != null)
        {
            _markerCallsign.text = _callsign;
        }
    }


}

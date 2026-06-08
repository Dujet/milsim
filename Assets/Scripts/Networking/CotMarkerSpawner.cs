using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Example consumer for <see cref="CotReceiver"/>.
///
/// Attach this to the same GameObject as <see cref="CotReceiver"/> (or any
/// GameObject that has a reference to one).  Assign a prefab to
/// <see cref="markerPrefab"/> in the Inspector.  When an ATAK/CIV-TAK
/// operator places or moves a marker, a corresponding GameObject will be
/// spawned or repositioned in the Unity scene at the correct WGS-84-derived
/// world position.
///
/// This is intentionally minimal — extend it to play sounds, show UI
/// tooltips, trigger mission logic, etc.
/// </summary>
public class CotMarkerSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The CotReceiver component to subscribe to.")]
    public CotReceiver cotReceiver;

    [Header("Spawning")]
    [Tooltip("Prefab instantiated for every new operator marker. "
           + "Needs a CotMarkerTag component (or you can use any prefab and "
           + "customise HandleAdd below).")]
    public GameObject markerPrefab;

    [Tooltip("Optional: only spawn markers of these CoT kinds. "
           + "Leave empty to accept all kinds that pass CotReceiver's filter.")]
    public CotKind[] allowedKinds = { CotKind.UserMarker, CotKind.Waypoint, CotKind.Emergency };

    // UID → live scene object
    private readonly Dictionary<string, GameObject> _spawnedMarkers = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Awake()
    {
        if (cotReceiver == null)
            cotReceiver = GetComponent<CotReceiver>();

        if (cotReceiver == null)
        {
            Debug.LogError("[CotMarkerSpawner] No CotReceiver found — disabling.");
            enabled = false;
            return;
        }

        cotReceiver.OnMarkerAdded   += HandleAdd;
        cotReceiver.OnMarkerUpdated += HandleUpdate;
        cotReceiver.OnMarkerDeleted += HandleDelete;
    }

    void OnDestroy()
    {
        if (cotReceiver == null) return;
        cotReceiver.OnMarkerAdded   -= HandleAdd;
        cotReceiver.OnMarkerUpdated -= HandleUpdate;
        cotReceiver.OnMarkerDeleted -= HandleDelete;
    }

    // ── Event handlers ────────────────────────────────────────────────────

    private void HandleAdd(CotInboundEvent evt)
    {
        if (!IsAllowedKind(evt.Kind)) return;

        if (markerPrefab == null)
        {
            Debug.LogWarning($"[CotMarkerSpawner] markerPrefab not assigned — cannot spawn '{evt.Callsign}'.");
            return;
        }

        Vector3 worldPos = CoordinateConverter.Wgs84ToUnity(evt.Lat, evt.Lon, evt.HaeMeters);

        GameObject marker = Instantiate(markerPrefab, worldPos, Quaternion.identity);
        marker.name = $"CotMarker_{evt.Callsign}_{evt.Uid}";

        // ── Extend here ──────────────────────────────────────────────────
        // Give the marker its metadata so other systems can query it:
        //   var tag = marker.GetComponent<CotMarkerTag>();
        //   if (tag != null) tag.Populate(evt);
        //
        // Or drive a label:
        //   marker.GetComponentInChildren<TextMesh>().text = evt.Callsign;
        // ────────────────────────────────────────────────────────────────

        _spawnedMarkers[evt.Uid] = marker;

        Debug.Log($"[CotMarkerSpawner] Spawned marker '{evt.Callsign}' " +
                  $"({evt.CotType}) at {worldPos}  uid={evt.Uid}");
    }

    private void HandleUpdate(CotInboundEvent evt)
    {
        if (!_spawnedMarkers.TryGetValue(evt.Uid, out GameObject marker))
        {
            // We may not have spawned this UID if its kind was filtered —
            // treat an update as a late-arriving add in that case.
            HandleAdd(evt);
            return;
        }

        if (marker == null)
        {
            // Something else destroyed the object; clean up and re-spawn
            _spawnedMarkers.Remove(evt.Uid);
            HandleAdd(evt);
            return;
        }

        marker.transform.position = CoordinateConverter.Wgs84ToUnity(evt.Lat, evt.Lon, evt.HaeMeters);

        Debug.Log($"[CotMarkerSpawner] Moved marker '{evt.Callsign}' to " +
                  $"{marker.transform.position}");
    }

    private void HandleDelete(string uid)
    {
        if (!_spawnedMarkers.TryGetValue(uid, out GameObject marker))
        {
            Debug.Log($"[CotMarkerSpawner] Delete received for unknown uid '{uid}' — ignored.");
            return;
        }

        _spawnedMarkers.Remove(uid);

        if (marker != null)
        {
            Debug.Log($"[CotMarkerSpawner] Destroying marker uid='{uid}'");
            Destroy(marker);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────


    private bool IsAllowedKind(CotKind kind)
    {
        if (allowedKinds == null || allowedKinds.Length == 0) return true;
        foreach (CotKind k in allowedKinds)
            if (k == kind) return true;
        return false;
    }
}

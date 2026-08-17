using System.Collections.Generic;
using UnityEngine;

public class TankCotRegistration : MonoBehaviour
{
    [Header("Identity")]
    public Faction faction = Faction.NATO;
    public int unitIndex = 1;
    [SerializeField] private bool isDrone = false;
 
    // CoT type strings for each faction
    private const string CotTypeFriendly = "a-f-G-U-C"; // friendly ground combat
    private const string CotTypeFriendlyDrone = "a-f-A"; // friendly air unmanned
    private const string CotTypeHostile  = "a-h-G-U-C"; // hostile ground combat
 
    private CotSender _sender;
    private string    _uid;
 
    private void Start()
    {
        // Locate the single scene-wide CotSender
        _sender = FindObjectOfType<CotSender>();
        if (_sender == null)
        {
            Debug.LogError("[TankCotRegistration] No CotSender found in scene — entity will NOT be broadcast.");
            return;
        }
 
        string role = faction == Faction.NATO ? "tank-blue" : "tank-red";
        _uid = CotSender.MakeUid(role, unitIndex);
 
        CotEntity entity = new CotEntity
        {
            uid       = _uid,
            cotType   = ClassifyType(),
            callsign  = $"{(faction == Faction.NATO ? "BLUE" : "RED")}-{unitIndex:D2}",
            transform = transform,
            remarks   = $"Simulated {faction} tank (Unity)"
        };
 
        _sender.RegisterEntity(entity);
    }
 
    private void OnDestroy()
    {
        // Remove from broadcast list so CIV-TAK stops receiving updates for this tank
        // TODO: maybe add destroyed state (check civ-tak/fts docs to see if there's a way to mark an entity as destroyed)
        if (_sender != null)
            _sender.UnregisterEntity(_uid);
    }

    private string ClassifyType()
    {
        if (isDrone) return CotTypeFriendlyDrone;
        else return faction == Faction.NATO ? CotTypeFriendly : CotTypeHostile;
    }
}

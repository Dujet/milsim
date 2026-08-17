using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Networking;

public class CotSender : MonoBehaviour
{
    public string ftsBroadcastUrl = "http://127.0.0.1:8087";
    public float broadcastIntervalSeconds = 5f;
    public float staleOffsetSeconds = 30f;
    public CotTcpSender tcpSender;

    [SerializeField] private FieldOfView _fieldOfView;

    //TODO: broadcast hostile entities only if they are visible by the drone
    private List<CotEntity> _entities = new();

    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(BroadcastLoop());
        //StartCoroutine(TestPayload());

        if (_fieldOfView == null)
        {
            Debug.LogWarning("[CotSender] FieldOfView reference is null. Attempting to find one in the scene.");
            _fieldOfView = FindObjectOfType<FieldOfView>();
            if (_fieldOfView == null)
            {
                Debug.LogError("[CotSender] No FieldOfView component found in the scene. Visible target tracking will not work.");
            }
        }
    }

    private IEnumerator TestPayload()
    {
        yield return new WaitForSeconds(5f);

        while (true)
        {


            CotEntity ent = _entities[0];
            Wgs84Coordinate coords = CoordinateConverter.UnityToWgs84Double(ent.transform.position);
            string xml = BuildCotXml(
                ent.uid,
                ent.cotType,
                ent.callsign,
                coords.Latitude, coords.Longitude, coords.HaeMeters,
                ent.remarks
            );

            Debug.Log(xml);
            tcpSender.SendCot(xml);

            yield return new WaitForSeconds(10f);
        }


    }


    private IEnumerator BroadcastLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(broadcastIntervalSeconds);

            var snapshot = new List<CotEntity>(_entities);
            var visibleTargets = _fieldOfView.GetVisibleTargetSet();
            //snapshot.RemoveRange(1, 11); // debug for single tank testing
            foreach (CotEntity entity in snapshot)
            {
                if (entity.transform == null)
                {
                    Debug.LogWarning($"[CotSender] Entity '{entity.callsign}' has a null transform — skipping.");
                    continue;
                }

                if (!entity.cotType.StartsWith("a-f") && !visibleTargets.Contains(entity.transform))
                {
                    //Debug.Log($"[CotSender] Skipping non-friendly entity '{entity.callsign}' because it is not visible.");
                    continue;
                }

                Wgs84Coordinate coords = CoordinateConverter.UnityToWgs84Double(entity.transform.position);

                string xml = BuildCotXml(
                    entity.uid,
                    entity.cotType,
                    entity.callsign,
                    coords.Latitude, coords.Longitude, coords.HaeMeters,
                    entity.remarks
                );

                //StartCoroutine(PostCoT(xml));
                tcpSender.SendCot(xml);

            }
        }
    }

    public void SendSingleCoT(
    string uid,
    string cotType,
    string callsign,
    double lat,
    double lon,
    double haeMeters = 0.0,
    string remarks = "")
    {
        string xml = BuildCotXml(uid, cotType, callsign, lat, lon, haeMeters, remarks);
        tcpSender.SendCot(xml);
    }

    /// <summary>
    /// Sends a CoT delete instruction for the entity identified by <paramref name="uid"/>.
    ///
    /// TAK clients do NOT identify the deletion target by the event's own uid.
    /// The event carries an arbitrary uid of its own, while the item to remove is
    /// named in detail/link/@uid and accompanied by an empty &lt;__forcedelete/&gt;
    /// element. A &lt;point&gt; element is also required by the base schema, even
    /// though it carries no meaning here.
    /// </summary>
    public void SendDeleteCoT(string uid)
    {
        DateTime now = DateTime.UtcNow;
        const string fmt = "yyyy-MM-ddTHH:mm:ss.fffZ";

        string time = now.ToString(fmt);

        // Trenutak isteka MORA biti u buducnosti. Poruka koja je pri dolasku
        // vec istekla odbacuje se prije nego sto se brisanje primijeni, sto
        // objasnjava neuspjeh ranijih pokusaja sa stale == time i stale < start.
        string stale = now.AddHours(1).ToString(fmt);

        // Redoslijed elemenata slijedi primjer za koji je poznato da radi:
        // detail dolazi prije point.
        string xml =
            "<?xml version=\"1.0\" standalone=\"yes\"?>" +
            $"<event version=\"2.0\" uid=\"{Guid.NewGuid()}\" type=\"t-x-d-d\"" +
            $" time=\"{time}\" start=\"{time}\" stale=\"{stale}\" how=\"m-g\">" +
            "<detail>" +
            $"<link uid=\"{EscapeXml(uid)}\" relation=\"none\" type=\"none\"/>" +
            "<__forcedelete/>" +
            "</detail>" +
            "<point lat=\"0\" lon=\"0\" hae=\"0\" ce=\"9999999\" le=\"9999999\"/>" +
            "</event>";

        tcpSender.SendCot(xml);
    }



    public void RegisterEntity(CotEntity entity)
    {
        if (entity == null || entity.transform == null)
        {
            Debug.LogWarning("[CotSender] RegisterEntity: entity or its transform is null");
            return;
        }
        _entities.Add(entity);
        Debug.Log($"[CotSender] Registered entity '{entity.callsign}' ({entity.uid})");
    }

    public void UnregisterEntity(string uid)
    {
        int removed = _entities.RemoveAll(e => e.uid == uid);
        if (removed > 0)
            Debug.Log($"[CotSender] Unregistered {removed} entities with UID '{uid}'");

    }

    public static string MakeUid(string role, int index)
    {
        int hash = (role + index).GetHashCode();
        return $"unity-{role}-{index}-{Mathf.Abs(hash):x6}";
    }

    /// <summary>
    /// Builds a valid CoT XML event string.
    ///
    /// Example output:
    /// <![CDATA[
    /// <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
    /// <event version="2.0"
    ///        uid="unity-tank-blue-1"
    ///        type="a-f-G-U-C"
    ///        time="2026-06-04T10:00:00.000Z"
    ///        start="2026-06-04T10:00:00.000Z"
    ///        stale="2026-06-04T10:00:30.000Z"
    ///        how="m-g">
    ///   <point lat="48.857" lon="2.353" hae="0" ce="10" le="10"/>
    ///   <detail>
    ///     <contact callsign="TANK-BLUE-1"/>
    ///     <remarks>Simulated friendly tank</remarks>
    ///   </detail>
    /// </event>
    /// ]]>
    /// </summary>
    private string BuildCotXml(
        string uid,
        string cotType,
        string callsign,
        double lat,
        double lon,
        double hae,
        string remarks)
    {
        DateTime now = DateTime.UtcNow;
        DateTime stale = now.AddSeconds(staleOffsetSeconds);

        // CoT timestamp format: 2026-06-04T10:00:00.000Z
        string timeFmt = "yyyy-MM-ddTHH:mm:ss.fffZ";
        string timeStr = now.ToString(timeFmt);
        string staleStr = stale.ToString(timeFmt);

        // Escape any XML special characters in user-supplied strings
        string safeCallsign = EscapeXml(callsign);
        string safeRemarks = EscapeXml(remarks);
        string safeUid = EscapeXml(uid);

        // ce = circular error (horizontal accuracy in metres), le = linear error (vertical)
        // 9999999 = "unknown"
        const string ce = "10";
        const string le = "10";





        // how="m-g" = machine generated (simulation)
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
        sb.Append("<event version=\"2.0\"");
        sb.Append($" uid=\"{safeUid}\"");
        sb.Append($" type=\"{cotType}\"");
        sb.Append($" time=\"{timeStr}\"");
        sb.Append($" start=\"{timeStr}\"");
        sb.Append($" stale=\"{staleStr}\"");
        sb.Append(" how=\"m-g\">");
        sb.Append($"<point lat=\"{lat.ToString(CultureInfo.InvariantCulture):F7}\" lon=\"{lon.ToString(CultureInfo.InvariantCulture):F7}\" hae=\"{hae.ToString(CultureInfo.InvariantCulture):F2}\" ce=\"{ce}\" le=\"{le}\"/>");
        sb.Append("<detail>");
        sb.Append($"<contact callsign=\"{safeCallsign}\"/>");
        if (!string.IsNullOrEmpty(safeRemarks))
            sb.Append($"<remarks>{safeRemarks}</remarks>");
        sb.Append("</detail>");
        sb.Append("</event>");

        return sb.ToString();
    }

    private static string EscapeXml(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    void OnApplicationQuit()
    {
        Debug.Log("[CoTSender] Application quit. Stopping broadcast loop.");
        StopAllCoroutines();

        var snapshot = new List<CotEntity>(_entities);
        //snapshot.RemoveRange(1, 11); // debug for single tank testing

        foreach (CotEntity ent in snapshot)
        {
            SendDeleteCoT(ent.uid);
        }
    }

    [ContextMenu("Test: obriši prvi prijavljeni entitet")]
    private void DebugDeleteFirstEntity()
    {
        if (_entities.Count == 0)
        {
            Debug.LogWarning("[CotSender] Nema prijavljenih entiteta.");
            return;
        }

        CotEntity entity = _entities[0];
        Debug.Log($"[CotSender] TEST brisanja: '{entity.callsign}' ({entity.uid})");

        UnregisterEntity(entity.uid);
        SendDeleteCoT(entity.uid);
    }


}

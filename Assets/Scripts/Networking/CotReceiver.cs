using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;
using UnityEngine;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
//  DATA TYPES
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/// <summary>
/// Parsed representation of one inbound CoT event received from FTS/ATAK.
/// Populated entirely on the background thread, consumed on the main thread.
/// </summary>
public class CotInboundEvent
{
    /// <summary>Globally-unique identifier for this TAK entity.</summary>
    public string Uid;

    /// <summary>
    /// CoT type atom, e.g. "a-f-G-U-C" (friendly ground unit)
    /// or "b-m-p-s-m" (ATAK map marker).
    /// </summary>
    public string CotType;

    /// <summary>Human-readable call sign from &lt;contact callsign=…/&gt;.</summary>
    public string Callsign;

    /// <summary>WGS-84 latitude in decimal degrees.</summary>
    public double Lat;

    /// <summary>WGS-84 longitude in decimal degrees.</summary>
    public double Lon;

    /// <summary>Height above ellipsoid in metres (may be 0 when unknown).</summary>
    public double HaeMeters;

    /// <summary>Free-text content of the &lt;remarks&gt; element, if present.</summary>
    public string Remarks;

    /// <summary>UTC timestamp from the CoT event header.</summary>
    public DateTime Time;

    /// <summary>UTC stale time from the CoT event header.</summary>
    public DateTime Stale;

    /// <summary>True when this event represents a t-x-d-d (delete) message.</summary>
    public bool IsDelete;

    /// <summary>Convenience classification of <see cref="CotType"/>.</summary>
    public CotKind Kind => CotKindHelper.Classify(CotType);

    public override string ToString() =>
        IsDelete
            ? $"[CoT DELETE uid={Uid}]"
            : $"[CoT uid={Uid} type={CotType} callsign={Callsign} " +
              $"lat={Lat:F6} lon={Lon:F6} hae={HaeMeters:F1}]";
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>Broad semantic category of a CoT type string.</summary>
public enum CotKind
{
    Unknown,

    /// <summary>a-f-* — friendly track / unit atom.</summary>
    FriendlyAtom,

    /// <summary>a-h-* — hostile track.</summary>
    HostileAtom,

    /// <summary>a-n-* — neutral track.</summary>
    NeutralAtom,

    /// <summary>a-u-* — unknown track (affiliation unknown, not "Unknown").</summary>
    UnknownAtom,

    /// <summary>b-m-p-s-* — user-placed shape/marker in ATAK.</summary>
    UserMarker,

    /// <summary>b-m-p-w-* — GOTO waypoint.</summary>
    Waypoint,

    /// <summary>b-a-* — emergency / 9-line type marker.</summary>
    Emergency,

    /// <summary>u-d-* — geofence / drawn shape.</summary>
    Geofence,

    /// <summary>b-t-f — GeoChat message (has no &lt;point&gt; block).</summary>
    Chat,

    /// <summary>t-x-d-d — delete instruction.</summary>
    Delete,

    /// <summary>Anything else not explicitly categorised above.</summary>
    Other,
}

// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Static helpers for working with CoT type strings.
/// </summary>
public static class CotKindHelper
{
    /// <summary>Classify a raw CoT type string into a <see cref="CotKind"/>.</summary>
    public static CotKind Classify(string t)
    {
        if (string.IsNullOrEmpty(t)) return CotKind.Unknown;
        if (t == "t-x-d-d") return CotKind.Delete;
        if (t == "b-t-f") return CotKind.Chat;
        if (t.StartsWith("a-f-")) return CotKind.FriendlyAtom;
        if (t.StartsWith("a-h-")) return CotKind.HostileAtom;
        if (t.StartsWith("a-n-")) return CotKind.NeutralAtom;
        if (t.StartsWith("a-u-")) return CotKind.UnknownAtom;
        if (t.StartsWith("b-m-p-w-")) return CotKind.Waypoint;
        if (t.StartsWith("b-m-p-")) return CotKind.UserMarker;
        if (t.StartsWith("b-a-")) return CotKind.Emergency;
        if (t.StartsWith("u-d-")) return CotKind.Geofence;
        return CotKind.Other;
    }

    /// <summary>
    /// Returns true for CoT types that are typically created by a human TAK
    /// operator interacting with ATAK/CIV-TAK (as opposed to automated feeds).
    /// Adjust to match the mission types used in your exercise.
    /// </summary>
    public static bool IsOperatorPlaced(string t) =>
        t != null &&
        (t.StartsWith("b-m-") ||   // map markers / waypoints
         t.StartsWith("b-a-") ||   // emergency markers
         t.StartsWith("u-d-"));     // drawn shapes / geofences
}


// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
//  MAIN COMPONENT
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

/// <summary>
/// Connects to FreeTAKServer as an additional CoT TCP client and surfaces
/// inbound CoT events — especially operator-placed markers from ATAK/CIV-TAK —
/// as C# events dispatched safely on Unity's main thread.
///
/// ── Architecture ─────────────────────────────────────────────────────────
///
/// FTS broadcasts every CoT event it ingests to ALL connected TCP clients.
/// This component opens its own read-dedicated connection on the same port
/// used by <see cref="CotTcpSender"/> so it receives the full broadcast
/// stream, including anything placed by TAK operators in the field.
///
/// Because Unity's own entities are also echoed back on this connection,
/// the <see cref="ignoredUidPrefixes"/> list filters them out (default:
/// "unity-", matching the prefix used by <see cref="CotSender.MakeUid"/>).
///
/// ── Wire protocol ────────────────────────────────────────────────────────
///
/// CoT messages arrive as a continuous byte stream.  <see cref="CotTcpSender"/>
/// terminates each XML blob with \n; ATAK may send well-formatted multi-line
/// XML without a trailing newline.  The reader therefore buffers raw bytes
/// and slices on the &lt;/event&gt; sentinel, which is reliable regardless
/// of whitespace or line-ending conventions.
///
/// ── Thread safety ────────────────────────────────────────────────────────
///
/// All TCP I/O runs on a background thread.  Parsed events are queued via
/// a <see cref="ConcurrentQueue{T}"/> and drained in <c>Update()</c> so
/// that subscribers can call Unity APIs without marshalling concerns.
///
/// ── Usage ────────────────────────────────────────────────────────────────
/// <code>
/// void Awake()
/// {
///     var receiver = GetComponent<CotReceiver>;();
///     receiver.OnMarkerAdded   += evt => SpawnMarker(evt);
///     receiver.OnMarkerUpdated += evt => MoveMarker(evt);
///     receiver.OnMarkerDeleted += uid  => DestroyMarker(uid);
/// }
/// </code>
/// </summary>
public class CotReceiver : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────

    [Header("FTS Connection")]
    [Tooltip("FTS hostname or IP — same as CotTcpSender.host.")]
    public string host = "127.0.0.1";

    [Tooltip("FTS TCP CoT port — same as CotTcpSender.port.")]
    public int port = 8087;

    [Tooltip("Seconds to wait before retrying after a disconnect or error.")]
    public float reconnectDelay = 5f;

    [Header("Filtering")]
    [Tooltip("CoT events whose UID starts with any of these prefixes are ignored. "
           + "This prevents Unity's own broadcasted entities from firing callbacks.")]
    public string[] ignoredUidPrefixes = { "unity-" };

    [Tooltip("When enabled, OnMarkerAdded / OnMarkerUpdated only fire for "
           + "operator-placed types (b-m-*, b-a-*, u-d-*). "
           + "Atom tracks (a-*) will still appear in OnAnyEventReceived. "
           + "Disable to receive all CoT types including unit position updates.")]
    public bool filterToOperatorMarkers = true;

    // ── C# events (subscribe from code) ──────────────────────────────────

    /// <summary>
    /// Fired on the main thread the first time a CoT UID is seen.
    /// Represents a new marker or unit appearing on the TAK common
    /// operating picture.
    /// </summary>
    public event Action<CotInboundEvent> OnMarkerAdded;

    /// <summary>
    /// Fired on the main thread when an already-known CoT UID arrives
    /// again with fresh data (position update, remarks change, etc.).
    /// </summary>
    public event Action<CotInboundEvent> OnMarkerUpdated;

    /// <summary>
    /// Fired on the main thread when a <c>t-x-d-d</c> delete event
    /// is received. The argument is the UID of the deleted entity.
    /// </summary>
    public event Action<string> OnMarkerDeleted;

    /// <summary>
    /// Fired on the main thread for every parsed, non-filtered CoT event
    /// (add, update, and delete alike). Useful for logging or custom routing.
    /// </summary>
    public event Action<CotInboundEvent> OnAnyEventReceived;

    // ── Private state ─────────────────────────────────────────────────────

    private Thread _thread;
    private volatile bool _running;
    private TcpClient _client;

    // Background thread → Unity main thread handoff
    private readonly ConcurrentQueue<CotInboundEvent> _pending = new();

    // Tracks which UIDs have already triggered OnMarkerAdded so we can
    // distinguish subsequent updates from initial additions.
    private readonly HashSet<string> _knownUids = new();

    // ── Unity lifecycle ───────────────────────────────────────────────────

    void Start()
    {
        _running = true;
        _thread = new Thread(ReceiveLoop)
        {
            IsBackground = true,
            Name = "CotReceiver",
        };
        _thread.Start();
        Debug.Log($"[CotReceiver] Starting — will connect to {host}:{port}");
    }

    /// <summary>
    /// Drains the concurrent queue and dispatches events on the main thread.
    /// Called every frame by Unity.
    /// </summary>
    void Update()
    {
        while (_pending.TryDequeue(out CotInboundEvent evt))
            DispatchEvent(evt);
    }

    void OnDestroy()
    {
        _running = false;
        try { _client?.Close(); } catch { /* ignored — we're shutting down */ }
        _thread?.Join(2000);
    }

    // ── Main-thread dispatch ──────────────────────────────────────────────

    private void DispatchEvent(CotInboundEvent evt)
    {
        // Always fire the catch-all hook first
        try { OnAnyEventReceived?.Invoke(evt); }
        catch (Exception ex) { Debug.LogError($"[CotReceiver] OnAnyEventReceived threw: {ex}"); }

        if (evt.IsDelete)
        {
            _knownUids.Remove(evt.Uid);
            try { OnMarkerDeleted?.Invoke(evt.Uid); }
            catch (Exception ex) { Debug.LogError($"[CotReceiver] OnMarkerDeleted threw: {ex}"); }
            return;
        }

        // Optionally restrict add/update callbacks to operator-placed types
        bool passFilter = !filterToOperatorMarkers
                          || CotKindHelper.IsOperatorPlaced(evt.CotType);

        if (passFilter)
        {
            if (_knownUids.Add(evt.Uid))       // HashSet.Add returns true for new entries
            {
                Debug.Log($"[CotReceiver] ADDED   {evt}");
                try { OnMarkerAdded?.Invoke(evt); }
                catch (Exception ex) { Debug.LogError($"[CotReceiver] OnMarkerAdded threw: {ex}"); }
            }
            else
            {
                Debug.Log($"[CotReceiver] UPDATED {evt}");
                try { OnMarkerUpdated?.Invoke(evt); }
                catch (Exception ex) { Debug.LogError($"[CotReceiver] OnMarkerUpdated threw: {ex}"); }
            }
        }
        else
        {
            // Still track UID so a future delete or "pass" update resolves correctly
            _knownUids.Add(evt.Uid);
        }
    }

    // ── Background receive loop ───────────────────────────────────────────

    private void ReceiveLoop()
    {
        while (_running)
        {
            try
            {
                Connect();
                ReadLoop();
            }
            catch (ThreadAbortException)
            {
                break; // Engine is shutting down
            }
            catch (Exception ex) when (_running)
            {
                Debug.LogWarning($"[CotReceiver] {ex.GetType().Name}: {ex.Message} " +
                                 $"— reconnecting in {reconnectDelay}s");
            }
            finally
            {
                try { _client?.Close(); } catch { /* ignored */ }
                _client = null;
            }

            if (_running)
                Thread.Sleep(TimeSpan.FromSeconds(reconnectDelay));
        }

        Debug.Log("[CotReceiver] Receive loop exited.");
    }

    private void Connect()
    {
        _client = new TcpClient();
        _client.Connect(host, port);
        Debug.Log($"[CotReceiver] TCP connected to FTS at {host}:{port}");

        // FTS expects an initial CoT event to register the connection.
        // Send a minimal presence ping so the socket isn't treated as dead.
        SendHandshake();
    }

    private void SendHandshake()
    {
        string time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ");
        string xml =
            $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
            $"<event version=\"2.0\"" +
            $" uid=\"unity-receiver-ping\"" +
            $" type=\"a-f-G-U-C\"" +
            $" time=\"{time}\"" +
            $" start=\"{time}\"" +
            $" stale=\"{time}\"" +
            $" how=\"m-g\">" +
            $"<point lat=\"0.0\" lon=\"0.0\" hae=\"0.0\" ce=\"9999999\" le=\"9999999\"/>" +
            $"<detail><contact callsign=\"unity-receiver\"/></detail>" +
            $"</event>\n";

        byte[] data = Encoding.UTF8.GetBytes(xml);
        _client.GetStream().Write(data, 0, data.Length);
        _client.GetStream().Flush();
        Debug.Log("[CotReceiver] Handshake sent.");
    }

    /// <summary>
    /// Reads raw bytes from the TCP stream and extracts complete CoT events.
    ///
    /// Strategy: accumulate bytes into a string buffer and carve out every
    /// complete XML document by locating the </event> end-of-message
    /// sentinel.  This handles both the single-line newline-terminated format
    /// used by <see cref="CotTcpSender"/> and the multi-line pretty-printed
    /// XML that ATAK clients typically produce.
    /// </summary>
    private void ReadLoop()
    {
        const int ReadBufferSize = 8192;
        const string EndSentinel = "</event>";

        byte[] readBuf = new byte[ReadBufferSize];
        StringBuilder accumulator = new StringBuilder(ReadBufferSize * 2);
        NetworkStream stream = _client.GetStream();

        while (_running && _client.Connected)
        {
            int bytesRead = stream.Read(readBuf, 0, ReadBufferSize);
            if (bytesRead == 0)
            {
                Debug.Log("[CotReceiver] FTS closed the connection.");
                break; // Graceful close by the server
            }

            accumulator.Append(Encoding.UTF8.GetString(readBuf, 0, bytesRead));

            // Extract every complete </event> block from the accumulator
            string acc = accumulator.ToString();
            int searchFrom = 0;

            while (true)
            {
                int endIdx = acc.IndexOf(EndSentinel, searchFrom, StringComparison.Ordinal);
                if (endIdx < 0) break; // No complete event yet — wait for more data

                int endPos = endIdx + EndSentinel.Length;

                // Find the start of THIS event (search backwards from </event>)
                int startIdx = acc.LastIndexOf("<?xml", endIdx, StringComparison.OrdinalIgnoreCase);
                if (startIdx < 0 || startIdx >= endIdx)
                    startIdx = acc.LastIndexOf("<event", endIdx, StringComparison.OrdinalIgnoreCase);

                if (startIdx >= 0 && startIdx < endIdx)
                {
                    string rawXml = acc.Substring(startIdx, endPos - startIdx);
                    TryEnqueue(rawXml);
                }

                searchFrom = endPos; // Advance past the sentinel we just consumed
            }

            // Discard all data up to the last processed sentinel
            if (searchFrom > 0)
            {
                accumulator.Clear();
                if (searchFrom < acc.Length)
                    accumulator.Append(acc, searchFrom, acc.Length - searchFrom);
            }
        }
    }

    // ── XML parsing (background thread) ──────────────────────────────────

    /// <summary>
    /// Parses one raw XML string into a <see cref="CotInboundEvent"/> and
    /// pushes it onto the pending queue.  Silently drops malformed messages
    /// and events that match the UID ignore-list.
    /// </summary>
    private void TryEnqueue(string xml)
    {
        try
        {
            var doc = new XmlDocument();
            doc.LoadXml(xml);

            XmlElement ev = doc.DocumentElement;
            if (ev == null || ev.LocalName != "event") return;

            string uid = ev.GetAttribute("uid");
            string cotType = ev.GetAttribute("type");

            if (string.IsNullOrEmpty(uid)) return;

            // ── UID filter ──────────────────────────────────────────────
            foreach (string prefix in ignoredUidPrefixes)
            {
                if (uid.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    return; // This is one of our own Unity entities echoed back
            }

            // ── Delete event ────────────────────────────────────────────
            if (cotType == "t-x-d-d")
            {
                _pending.Enqueue(new CotInboundEvent
                {
                    Uid = uid,
                    CotType = cotType,
                    IsDelete = true,
                });
                return;
            }

            // ── Location ────────────────────────────────────────────────
            XmlElement point = ev["point"];
            if (point == null)
            {
                // GeoChat (b-t-f) and some control messages have no <point>.
                // Skip silently — there is no spatial data to relay.
                return;
            }

            double lat = ParseDouble(point.GetAttribute("lat"));
            double lon = ParseDouble(point.GetAttribute("lon"));
            double hae = ParseDouble(point.GetAttribute("hae"));

            // ── Detail block ────────────────────────────────────────────
            XmlElement detail = ev["detail"];
            string callsign = uid;          // sensible fallback
            string remarks = string.Empty;

            if (detail != null)
            {
                XmlElement contact = detail["contact"];
                if (contact != null)
                {
                    string cs = contact.GetAttribute("callsign");
                    if (!string.IsNullOrWhiteSpace(cs)) callsign = cs;
                }

                XmlElement rem = detail["remarks"];
                if (rem != null) remarks = rem.InnerText ?? string.Empty;
            }

            // ── Timestamps ──────────────────────────────────────────────
            DateTime.TryParse(ev.GetAttribute("time"), null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime time);
            DateTime.TryParse(ev.GetAttribute("stale"), null,
                System.Globalization.DateTimeStyles.RoundtripKind, out DateTime stale);

            _pending.Enqueue(new CotInboundEvent
            {
                Uid = uid,
                CotType = cotType,
                Callsign = callsign,
                Lat = lat,
                Lon = lon,
                HaeMeters = hae,
                Remarks = remarks,
                Time = time,
                Stale = stale,
                IsDelete = false,
            });
        }
        catch (XmlException xmlEx)
        {
            // Truncated or garbled TCP segment — harmless, next read will retry
            Debug.LogWarning($"[CotReceiver] XML parse error: {xmlEx.Message} " +
                             $"(snippet: {Snippet(xml, 120)})");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[CotReceiver] Unexpected parse error: {ex.Message}");
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────

    private static double ParseDouble(string s)
    {
        return double.TryParse(
            s,
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture,
            out double v) ? v : 0.0;
    }

    private static string Snippet(string s, int maxLen) =>
        s.Length <= maxLen ? s : s.Substring(0, maxLen) + "…";
}
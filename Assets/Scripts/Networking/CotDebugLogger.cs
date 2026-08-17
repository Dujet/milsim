using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>
/// Bilježi kašnjenje posredničkog lanca: od sastavljanja poruke (atribut time)
/// do njezina primitka natrag u simulaciji.
///
/// Mjerenje radi samo dok je popis ignoredUidPrefixes u CotReceiver prazan,
/// jer se inače vlastite poruke odbacuju prije obrade.
/// </summary>
public class CotDebugLogger : MonoBehaviour
{
    public CotReceiver cotReceiver;

    [Tooltip("Bilježi samo poruke s ovim predznakom identifikatora. Prazno znači sve.")]
    public string uidPrefixFilter = "unity-";

    [Tooltip("Ispis u konzolu. Isključiti pri duljim mjerenjima.")]
    public bool logToConsole = true;

    [Tooltip("Naziv datoteke CSV unutar Application.persistentDataPath.")]
    public string csvFileName = "cot_kasnjenje.csv";

    private StreamWriter _writer;
    private readonly List<double> _samples = new List<double>();

    void Awake()
    {
        if (cotReceiver == null) cotReceiver = FindObjectOfType<CotReceiver>();
        if (cotReceiver == null)
        {
            Debug.LogError("[CotDebug] Nema komponente CotReceiver.");
            enabled = false;
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, csvFileName);
        _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
        _writer.WriteLine("uid;tip;odlazak;dolazak;kasnjenje_ms");
        Debug.Log($"[CotDebug] Zapisujem u {path}");

        cotReceiver.OnAnyEventReceived += HandleEvent;
    }

    void OnDestroy()
    {
        if (cotReceiver != null)
            cotReceiver.OnAnyEventReceived -= HandleEvent;

        WriteSummary();

        _writer?.Dispose();
        _writer = null;
    }

    private void HandleEvent(CotInboundEvent evt)
    {
        DateTime arrivedUtc = DateTime.UtcNow;

        if (evt.IsDelete) return;              // poruka o brisanju nema usporedivo vrijeme
        if (evt.Time == default) return;       // atribut time nije raščlanjen

        if (!string.IsNullOrEmpty(uidPrefixFilter) &&
            !evt.Uid.StartsWith(uidPrefixFilter, StringComparison.OrdinalIgnoreCase))
            return;

        DateTime sentUtc  = evt.Time.ToUniversalTime();
        double latencyMs  = (arrivedUtc - sentUtc).TotalMilliseconds;

        _samples.Add(latencyMs);

        const string fmt = "yyyy-MM-ddTHH:mm:ss.fffZ";
        _writer.WriteLine(string.Join(";",
            evt.Uid,
            evt.CotType,
            sentUtc.ToString(fmt, CultureInfo.InvariantCulture),
            arrivedUtc.ToString(fmt, CultureInfo.InvariantCulture),
            latencyMs.ToString("F1", CultureInfo.InvariantCulture)));

        if (logToConsole)
            Debug.Log($"[CotDebug] {evt.Uid}: odlazak {sentUtc:HH:mm:ss.fff}, " +
                      $"dolazak {arrivedUtc:HH:mm:ss.fff}, kašnjenje {latencyMs:F1} ms");
    }

    private void WriteSummary()
    {
        if (_samples.Count == 0)
        {
            Debug.LogWarning("[CotDebug] Nema uzoraka. Je li ignoredUidPrefixes prazan?");
            return;
        }

        var sorted = new List<double>(_samples);
        sorted.Sort();

        double sum = 0.0;
        foreach (double v in sorted) sum += v;

        int p95Index = Mathf.Clamp((int)(sorted.Count * 0.95), 0, sorted.Count - 1);

        Debug.Log($"[CotDebug] Uzoraka: {sorted.Count}, " +
                  $"najmanje {sorted[0]:F1} ms, " +
                  $"medijan {sorted[sorted.Count / 2]:F1} ms, " +
                  $"srednje {sum / sorted.Count:F1} ms, " +
                  $"95. percentil {sorted[p95Index]:F1} ms, " +
                  $"najveće {sorted[sorted.Count - 1]:F1} ms");
    }
}
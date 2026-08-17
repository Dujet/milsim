using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

public class FrameTimeLogger : MonoBehaviour
{
    [Tooltip("Oznaka mjerenja, npr. 'sa-razmjenom' ili 'bez-razmjene'.")]
    public string runLabel = "mjerenje";

    [Tooltip("Broj početnih sličica koje se odbacuju zbog zagrijavanja.")]
    public int warmupFrames = 300;

    [Tooltip("Trajanje mjerenja u sekundama.")]
    public float durationSeconds = 60f;

    [Tooltip("Bez ovoga se mjeri osvježavanje zaslona, a ne kod.")]
    public bool disableVSync = true;

    private readonly List<float> _frameTimesMs = new List<float>();
    private int _frameCount;
    private float _elapsed;
    private bool _done;

    void Start()
    {
        if (disableVSync)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
        }
    }

    void Update()
    {
        if (_done) return;

        _frameCount++;
        if (_frameCount <= warmupFrames) return;   // zagrijavanje

        _frameTimesMs.Add(Time.unscaledDeltaTime * 1000f);

        _elapsed += Time.unscaledDeltaTime;
        if (_elapsed >= durationSeconds)
        {
            _done = true;
            Report();
        }
    }

    private void Report()
    {
        if (_frameTimesMs.Count == 0)
        {
            Debug.LogWarning("[FrameTime] Nema uzoraka.");
            return;
        }

        var sorted = new List<float>(_frameTimesMs);
        sorted.Sort();

        double sum = 0.0;
        foreach (float v in sorted) sum += v;
        float mean = (float)(sum / sorted.Count);

        int i95 = Mathf.Clamp((int)(sorted.Count * 0.95f), 0, sorted.Count - 1);
        int i99 = Mathf.Clamp((int)(sorted.Count * 0.99f), 0, sorted.Count - 1);

        Debug.Log($"[FrameTime] {runLabel}: sličica {sorted.Count}, " +
                  $"srednje {mean:F2} ms ({1000f / mean:F0} sl/s), " +
                  $"medijan {sorted[sorted.Count / 2]:F2} ms, " +
                  $"95. perc. {sorted[i95]:F2} ms, " +
                  $"99. perc. {sorted[i99]:F2} ms, " +
                  $"najgore {sorted[sorted.Count - 1]:F2} ms");

        string path = Path.Combine(Application.persistentDataPath,
                                   $"frametime_{runLabel}.csv");
        using (var w = new StreamWriter(path, false, Encoding.UTF8))
        {
            w.WriteLine("slicica;vrijeme_ms");
            for (int k = 0; k < _frameTimesMs.Count; k++)
                w.WriteLine($"{k};{_frameTimesMs[k].ToString("F3", CultureInfo.InvariantCulture)}");
        }
        Debug.Log($"[FrameTime] Zapisano u {path}");
    }
}
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class ReconstructStepsPerConfig_Auto
{
    // === YOUR FIXED GATES ===
    public static float minSeparationThresholdMeters = 0.3f; // d > 0.3f
    public static float minStepIntervalSeconds       = 0.2f; // gap >= 0.2f

#if UNITY_EDITOR
    [MenuItem("Tools/Steps/Reconstruct (per-config, fixed)")]
    private static void MenuRun()
    {
        var rawPath = EditorUtility.OpenFilePanel("Select RAW per-frame CSV", "", "csv,tsv,txt");
        if (string.IsNullOrEmpty(rawPath)) return;

        var outPath = EditorUtility.SaveFilePanel("Save reconstructed CSV", Path.GetDirectoryName(rawPath),
            Path.GetFileNameWithoutExtension(rawPath) + "_withSteps.csv", "csv");
        if (string.IsNullOrEmpty(outPath)) return;

        try
        {
            Run(rawPath, outPath);
            EditorUtility.RevealInFinder(outPath);
        }
        catch (Exception ex)
        {
            Debug.LogError("[ReconstructStepsPerConfig_Fixed] " + ex);
            EditorUtility.DisplayDialog("Reconstruction failed", ex.Message, "OK");
        }
    }
#endif

    /// <summary>
    /// Reconstructs step counts per Config with fixed thresholds (no anchoring).
    /// Assumes columns: Config, Timestamp, rawLeftFootPositionZ, rawRightFootPositionZ (plus the other pos cols are OK but unused).
    /// </summary>
    public static void Run(string rawCsvPath, string outCsvPath)
    {
        if (!File.Exists(rawCsvPath)) throw new FileNotFoundException("RAW CSV not found", rawCsvPath);

        var inv = CultureInfo.InvariantCulture;
        var lines = File.ReadAllLines(rawCsvPath);
        if (lines.Length < 2) throw new Exception("RAW CSV has no data rows.");

        char delim = DetectDelimiter(lines[0]);
        var header = SplitAndTrim(lines[0], delim);
        var map = HeaderMap(header);

        Require(map, "Config", "Timestamp",
                     "rawLeftFootPositionZ", "rawRightFootPositionZ"); // Z-only as per your RT code

        // Parse rows
        var rows = new List<Row>(lines.Length - 1);
        for (int i = 1; i < lines.Length; i++)
        {
            var p = SplitAndTrim(lines[i], delim);
            if (p.Length != header.Length) continue;

            rows.Add(new Row
            {
                cfg = p[map["Config"]],
                t   = ParseDouble(p[map["Timestamp"]], inv),
                Lz  = ParseDouble(p[map["rawLeftFootPositionZ"]], inv),
                Rz  = ParseDouble(p[map["rawRightFootPositionZ"]], inv),
                original = p
            });
        }
        if (rows.Count == 0) throw new Exception("No valid rows parsed.");

        // Group indices by config
        var byCfg = new Dictionary<string, List<int>>();
        for (int i = 0; i < rows.Count; i++)
        {
            var key = rows[i].cfg;
            if (!byCfg.TryGetValue(key, out var list)) { list = new List<int>(); byCfg[key] = list; }
            list.Add(i);
        }

        // Reconstruct per config using your real-time logic:
        // d = Round(|Lz - Rz|, 1), derivative = d - prevFeetDistance
        // if (prevDerivative > 0 && derivative <= 0 && d > minSep && gapOK) step++
        int[] recon = new int[rows.Count];
        float minSep = minSeparationThresholdMeters;
        float minGap = minStepIntervalSeconds;

        foreach (var kv in byCfg)
        {
            var idxs = kv.Value; if (idxs.Count < 2) continue;

            // init per-config state
            float prevFeetDistance = Round1((float)Math.Abs(rows[idxs[0]].Lz - rows[idxs[0]].Rz));
            float prevDerivative   = 0f;
            double lastStepTime    = rows[idxs[0]].t - 999.0;

            // first row: zero
            recon[idxs[0]] = 0;

            for (int k = 1; k < idxs.Count; k++)
            {
                int gi = idxs[k];
                var r  = rows[gi];

                float d = Round1((float)Math.Abs(r.Lz - rows[gi].Rz));
                // NOTE: the above line mistakenly used rows[gi].Rz twice; correct to use current Rz:
                d = Round1((float)Math.Abs(r.Lz - r.Rz));

                float deriv = d - prevFeetDistance;
                bool isPeak = (prevDerivative > 0f) && (deriv <= 0f);
                bool sepOK  = d > minSep;
                bool gapOK  = (r.t - lastStepTime) >= minGap;

                if (isPeak && sepOK && gapOK)
                {
                    recon[gi] = recon[idxs[k - 1]] + 1;
                    lastStepTime = r.t;
                }
                else
                {
                    recon[gi] = recon[idxs[k - 1]];
                }

                prevDerivative   = deriv;
                prevFeetDistance = d;
            }
        }

        // Write output with appended stepCountReconstructed + (for visibility) the fixed params used
        var outHeader = header.ToList();
        outHeader.Add("stepCountReconstructed");
        outHeader.Add("minSeparationUsed");   // same value per row, but handy for provenance
        outHeader.Add("minStepIntervalUsed"); // same value per row

        using (var sw = new StreamWriter(outCsvPath, false, Encoding.UTF8))
        {
            sw.WriteLine(string.Join(delim, outHeader));
            for (int i = 0; i < rows.Count; i++)
            {
                var line = rows[i].original.ToList();
                line.Add(recon[i].ToString(inv));
                line.Add(minSep.ToString(inv));
                line.Add(minGap.ToString(inv));
                sw.WriteLine(string.Join(delim, line));
            }
        }

        Debug.Log($"[ReconstructStepsPerConfig_Fixed] Wrote: {outCsvPath}  | configs={byCfg.Count}  | rows={rows.Count}");
    }

    // ===== types & utils =====
    private class Row
    {
        public string cfg;
        public double t;
        public double Lz, Rz;
        public string[] original;
    }

    private static float Round1(float v) => (float)Math.Round(v, 1);

    private static char DetectDelimiter(string s) => s.Contains("\t") ? '\t' : ',';
    private static string[] SplitAndTrim(string s, char d) => s.Split(d).Select(x => x.Trim()).ToArray();

    private static Dictionary<string,int> HeaderMap(string[] header)
    {
        var map = new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < header.Length; i++) map[header[i].Replace(" ", "")] = i;
        return map;
    }
    private static void Require(Dictionary<string,int> map, params string[] cols)
    {
        foreach (var c in cols) if (!map.ContainsKey(c.Replace(" ", ""))) throw new Exception($"Missing column '{c}'");
    }
    private static double ParseDouble(string s, IFormatProvider inv)
    {
        double.TryParse(s, NumberStyles.Float, inv, out double v);
        return v;
    }
}
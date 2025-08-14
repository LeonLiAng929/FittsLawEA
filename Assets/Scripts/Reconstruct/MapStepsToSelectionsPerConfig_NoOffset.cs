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

public static class MapStepsToSelectionsPerConfig_NoOffset
{
    // We expect stepCountReconstructed from the reconstructor; fall back to stepCountRaw if needed.
    static readonly string[] StepColsPriority = { "stepCountReconstructed", "stepCountRaw" };

#if UNITY_EDITOR
    [MenuItem("Tools/Steps/Map Steps → Selection (per-config, no offset)")]
    private static void MenuRun()
    {
        var rawPath = EditorUtility.OpenFilePanel("Select RAW-with-steps CSV", "", "csv,tsv,txt");
        if (string.IsNullOrEmpty(rawPath)) return;

        var selPath = EditorUtility.OpenFilePanel("Select Selection CSV", Path.GetDirectoryName(rawPath), "csv,tsv,txt");
        if (string.IsNullOrEmpty(selPath)) return;

        var outPath = EditorUtility.SaveFilePanel("Save mapped Selection CSV", Path.GetDirectoryName(selPath),
            Path.GetFileNameWithoutExtension(selPath) + "_withStepMap.csv", "csv");
        if (string.IsNullOrEmpty(outPath)) return;

        try { Run(rawPath, selPath, outPath); EditorUtility.RevealInFinder(outPath); }
        catch (Exception ex) { Debug.LogError("[MapStepsToSelectionsPerConfig_NoOffset] " + ex);
            EditorUtility.DisplayDialog("Mapping failed", ex.Message, "OK"); }
    }
#endif

    public static void Run(string rawCsvPath, string selectionCsvPath, string outCsvPath)
    {
        var inv = CultureInfo.InvariantCulture;

        // --- RAW ---
        var rawLines = File.ReadAllLines(rawCsvPath);
        if (rawLines.Length < 2) throw new Exception("RAW CSV has no data.");
        char rD = Detect(rawLines[0]);
        var rH = Split(rawLines[0], rD);
        var rM = Map(rH);
        Require(rM, "Config","Timestamp");

        string stepKey = StepColsPriority.FirstOrDefault(k => rM.ContainsKey(k));
        if (stepKey == null) throw new Exception("No step column found in RAW file (need 'stepCountReconstructed' or 'stepCountRaw').");

        var perCfg = new Dictionary<string, (double[] t, int[] s)>();
        var accT = new Dictionary<string, List<double>>();
        var accS = new Dictionary<string, List<int>>();

        for (int i = 1; i < rawLines.Length; i++)
        {
            var p = Split(rawLines[i], rD); if (p.Length != rH.Length) continue;
            string cfg = p[rM["Config"]];
            double t = ParseD(p[rM["Timestamp"]], inv);
            int sc = ParseI(p[rM[stepKey]]);
            if (!accT.ContainsKey(cfg)) { accT[cfg] = new List<double>(); accS[cfg] = new List<int>(); }
            accT[cfg].Add(t); accS[cfg].Add(sc);
        }
        foreach (var kv in accT) perCfg[kv.Key] = (kv.Value.ToArray(), accS[kv.Key].ToArray());

        // --- SELECTION ---
        var selLines = File.ReadAllLines(selectionCsvPath);
        if (selLines.Length < 2) throw new Exception("Selection CSV has no data.");
        char sD = Detect(selLines[0]);
        var sH = Split(selLines[0], sD);
        var sM = Map(sH);
        Require(sM, "Config","Timestamp");

        bool hasTruth = sM.ContainsKey("SelectionStepCount");

        // OUT header = selection + mapped step + which column used + cadence
        var outHeader = sH.ToList();
        outHeader.Add("StepCountFromRawAtSelection");
        outHeader.Add("StepColumnUsed");
        outHeader.Add("SelectionCadenceComputed"); // steps per minute

        using (var sw = new StreamWriter(outCsvPath, false, Encoding.UTF8))
        {
            sw.WriteLine(string.Join(sD, outHeader));
            for (int i = 1; i < selLines.Length; i++)
            {
                var p = Split(selLines[i], sD); if (p.Length != sH.Length) continue;

                string cfg  = p[sM["Config"]];
                double tSel = ParseD(p[sM["Timestamp"]], inv);

                if (!perCfg.TryGetValue(cfg, out var series))
                    throw new Exception($"No RAW series for config '{cfg}'");

                int idx = Nearest(series.t, tSel);
                int mapped = series.s[Mathf.Clamp(idx, 0, series.s.Length - 1)];

                // Cadence: use SelectionStepCount if available; else fallback to mapped
                int stepsForCadence = mapped;
                if (hasTruth)
                {
                    int truth = ParseI(p[sM["SelectionStepCount"]]);
                    stepsForCadence = truth;
                }

                double cadenceSpm = (tSel > 0) ? (stepsForCadence / tSel) * 60.0 : double.NaN;

                var row = p.ToList();
                row.Add(mapped.ToString(inv));
                row.Add(stepKey);
                row.Add(DoubleToCsv(cadenceSpm, inv));
                sw.WriteLine(string.Join(sD, row));
            }
        }
    }

    // --- utils ---
    static char Detect(string s) => s.Contains("\t") ? '\t' : ',';
    static string[] Split(string s, char d) => s.Split(d).Select(x => x.Trim()).ToArray();
    static Dictionary<string,int> Map(string[] h){ var m=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase); for(int i=0;i<h.Length;i++) m[h[i].Replace(" ","")] = i; return m; }
    static void Require(Dictionary<string,int> m, params string[] cols){ foreach(var c in cols) if(!m.ContainsKey(c.Replace(" ",""))) throw new Exception($"Missing column '{c}'"); }
    static double ParseD(string s, IFormatProvider inv){ double.TryParse(s, NumberStyles.Float, inv, out double v); return v; }
    static int    ParseI(string s){ int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v); return v; }
    static int    Nearest(double[] t, double q){ int lo=0,hi=t.Length-1; while(lo<hi){ int mid=(lo+hi)>>1; if(t[mid]<q) lo=mid+1; else hi=mid; } int idx=lo; if(idx>0 && Math.Abs(t[idx-1]-q) < Math.Abs(t[idx]-q)) idx--; return idx; }
    static string DoubleToCsv(double x, IFormatProvider inv)
    {
        if (double.IsNaN(x) || double.IsInfinity(x)) return "";
        return x.ToString(inv);
    }
}
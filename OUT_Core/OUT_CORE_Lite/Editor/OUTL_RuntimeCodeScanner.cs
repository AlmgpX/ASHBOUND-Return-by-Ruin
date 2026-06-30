#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public sealed class OUTL_RuntimeCodeScanner : EditorWindow
{
    private readonly List<Hit> hits = new List<Hit>(512);
    private Vector2 scroll;
    private string report = "Not scanned yet.";
    private bool includeAllowedInReport;

    private enum Classification
    {
        Allowed,
        Violation,
        NeedsReview
    }

    private struct Hit
    {
        public string Path;
        public int Line;
        public string Kind;
        public string Token;
        public Classification Classification;
        public string Boundary;
        public string Code;
    }

    [MenuItem("OUT CORE Lite/Diagnostics/Runtime Code Scanner")]
    public static void Open()
    {
        GetWindow<OUTL_RuntimeCodeScanner>("OUTL Runtime Scanner");
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("OUT CORE Lite Runtime Code Scanner", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Scans OUT_CORE_Lite .cs files for direct runtime construction/search and hot-path risks. It classifies hits against OUT_CORE_LITE.md and does not rewrite code.", MessageType.Info);

        includeAllowedInReport = EditorGUILayout.ToggleLeft("Include allowed boundary hits in report", includeAllowedInReport);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan OUT_CORE_Lite")) Scan();
        if (GUILayout.Button("Copy Report")) EditorGUIUtility.systemCopyBuffer = report;
        EditorGUILayout.EndHorizontal();

        int allowed;
        int violations;
        int review;
        CountHits(out allowed, out violations, out review);
        EditorGUILayout.LabelField("Hits", hits.Count + "  allowed=" + allowed + "  violations=" + violations + "  needsReview=" + review);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    private void Scan()
    {
        hits.Clear();
        string root = ResolveLiteRoot();
        if (!Directory.Exists(root))
        {
            report = "OUT CORE Lite root not found: " + root;
            Repaint();
            return;
        }

        string[] files = Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++)
        {
            string assetPath = ToAssetPath(files[i]);
            ScanFile(files[i], assetPath);
        }

        report = BuildReport();
        Repaint();
    }

    private void ScanFile(string file, string assetPath)
    {
        string[] lines;
        try { lines = File.ReadAllLines(file); }
        catch (IOException) { return; }

        bool inHotPath = false;
        int hotBraceDepth = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string code = StripLineComment(lines[i]);
            if (string.IsNullOrWhiteSpace(code)) continue;

            if (!inHotPath && IsHotPathStart(code))
            {
                inHotPath = true;
                hotBraceDepth = CountChar(code, '{') - CountChar(code, '}');
                if (hotBraceDepth <= 0) hotBraceDepth = 1;
            }
            else if (inHotPath)
            {
                hotBraceDepth += CountChar(code, '{') - CountChar(code, '}');
                if (hotBraceDepth <= 0) inHotPath = false;
            }

            string token;
            if (TryFindConstructionToken(code, out token))
                AddHit(assetPath, i + 1, "Construction/search", token, code);

            if (inHotPath && TryFindHotPathToken(code, out token))
                AddHit(assetPath, i + 1, "Hot path", token, code);
        }
    }

    private void AddHit(string path, int line, string kind, string token, string code)
    {
        Classification classification;
        string boundary;
        Classify(path, kind, token, code, out classification, out boundary);
        hits.Add(new Hit
        {
            Path = path,
            Line = line,
            Kind = kind,
            Token = token,
            Classification = classification,
            Boundary = boundary,
            Code = code.Trim()
        });
    }

    private string BuildReport()
    {
        int allowed;
        int violations;
        int review;
        CountHits(out allowed, out violations, out review);

        StringBuilder sb = new StringBuilder(512 + hits.Count * 120);
        sb.AppendLine("OUT CORE Lite Runtime Code Scanner");
        sb.AppendLine("Scope: " + ToAssetPath(ResolveLiteRoot()).TrimEnd('/'));
        sb.AppendLine("Hits: " + hits.Count + " | Allowed: " + allowed + " | Violation: " + violations + " | NeedsReview: " + review);
        sb.AppendLine("Allowed boundaries: low-level pool/facade, OUTL_World bootstrap/despawn, Editor, Debug/Smoke/Golden, Templates/Foundation authoring, Console, Worldgen export, editor-authoring partials.");
        sb.AppendLine();

        AppendSection(sb, Classification.Violation);
        AppendSection(sb, Classification.NeedsReview);
        if (includeAllowedInReport) AppendSection(sb, Classification.Allowed);

        if (hits.Count == 0)
        {
            sb.AppendLine("No direct runtime construction/search or hot-path hits found.");
        }
        else if (allowed > 0 && !includeAllowedInReport)
        {
            sb.AppendLine();
            sb.AppendLine("Allowed hits hidden. Enable 'Include allowed boundary hits in report' to audit every classified hit.");
        }

        return sb.ToString();
    }

    private void AppendSection(StringBuilder sb, Classification classification)
    {
        bool header = false;
        for (int i = 0; i < hits.Count; i++)
        {
            Hit hit = hits[i];
            if (hit.Classification != classification) continue;
            if (!header)
            {
                sb.AppendLine("[" + classification + "]");
                header = true;
            }

            sb.Append(hit.Path).Append(':').Append(hit.Line)
                .Append(" [").Append(hit.Kind).Append("] ")
                .Append(hit.Token).Append(" | ").Append(hit.Boundary).Append(" :: ")
                .AppendLine(hit.Code);
        }

        if (header) sb.AppendLine();
    }

    private void CountHits(out int allowed, out int violations, out int review)
    {
        allowed = 0;
        violations = 0;
        review = 0;
        for (int i = 0; i < hits.Count; i++)
        {
            switch (hits[i].Classification)
            {
                case Classification.Allowed: allowed++; break;
                case Classification.Violation: violations++; break;
                case Classification.NeedsReview: review++; break;
            }
        }
    }

    private static void Classify(string assetPath, string kind, string token, string code, out Classification classification, out string boundary)
    {
        string p = NormalizePath(assetPath);

        if (IsLowLevelPoolBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: low-level pool/facade lifetime boundary"; return; }
        if (IsWorldBootstrapBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: OUTL_World bootstrap/despawn boundary"; return; }
        if (IsEditorBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: Editor/repair/generation tool"; return; }
        if (IsDebugOrTestBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: Debug/smoke/golden test boundary"; return; }
        if (IsTemplateBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: Templates/Foundation authoring boundary"; return; }
        if (IsConsoleBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: debug console/inspection boundary"; return; }
        if (IsWorldgenBoundary(p)) { classification = Classification.Allowed; boundary = "Allowed: Worldgen/export authoring boundary"; return; }
        if (IsEditorAuthoringPartial(p)) { classification = Classification.Allowed; boundary = "Allowed: editor-only authoring partial"; return; }
        if (IsCanonicalPoolFacadeUse(code)) { classification = Classification.Allowed; boundary = "Allowed: canonical OutCore.pool.OUT facade call"; return; }

        if (kind == "Hot path")
        {
            classification = Classification.NeedsReview;
            boundary = "Needs review: hot-path allocation/search risk";
            return;
        }

        classification = Classification.Violation;
        boundary = "Violation: runtime gameplay/module code must route lifetime/search through canonical OUTL systems";
    }

    private static bool TryFindConstructionToken(string code, out string token)
    {
        token = string.Empty;
        if (string.IsNullOrWhiteSpace(code)) return false;
        if (IsCanonicalPoolFacadeUse(code)) return false;
        if (code.IndexOf("UnityEngine.Object.Instantiate", System.StringComparison.Ordinal) >= 0 || ContainsCall(code, "Instantiate")) { token = "Instantiate"; return true; }
        if (code.IndexOf("new GameObject", System.StringComparison.Ordinal) >= 0) { token = "new GameObject"; return true; }
        if (ContainsCall(code, "Destroy") || code.IndexOf("UnityEngine.Object.Destroy", System.StringComparison.Ordinal) >= 0) { token = "Destroy"; return true; }
        if (code.IndexOf("AddComponent<", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".AddComponent(", System.StringComparison.Ordinal) >= 0) { token = "AddComponent"; return true; }
        if (code.IndexOf("Resources.Load", System.StringComparison.Ordinal) >= 0) { token = "Resources.Load"; return true; }
        if (code.IndexOf("Resources.GetBuiltinResource", System.StringComparison.Ordinal) >= 0) { token = "Resources.GetBuiltinResource"; return true; }
        if (code.IndexOf("GameObject.Find", System.StringComparison.Ordinal) >= 0) { token = "GameObject.Find"; return true; }
        if (ContainsCall(code, "FindObjectOfType")) { token = "FindObjectOfType"; return true; }
        if (ContainsCall(code, "FindObjectsOfType")) { token = "FindObjectsOfType"; return true; }
        return false;
    }

    private static bool TryFindHotPathToken(string code, out string token)
    {
        token = string.Empty;
        if (code.IndexOf("GetComponent<", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".GetComponent(", System.StringComparison.Ordinal) >= 0) { token = "GetComponent in hot path"; return true; }
        if (code.IndexOf(".Where(", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".Select(", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".ToList(", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".Any(", System.StringComparison.Ordinal) >= 0 || code.IndexOf(".First(", System.StringComparison.Ordinal) >= 0) { token = "LINQ in hot path"; return true; }
        return false;
    }

    private static bool IsHotPathStart(string code)
    {
        return ContainsMethodDeclaration(code, "Update")
            || ContainsMethodDeclaration(code, "FixedUpdate")
            || ContainsMethodDeclaration(code, "LateUpdate")
            || ContainsMethodDeclaration(code, "OUTL_Tick")
            || ContainsMethodDeclaration(code, "OUTL_RandomTick");
    }

    private static bool ContainsMethodDeclaration(string code, string name)
    {
        int index = code.IndexOf(name + "(", System.StringComparison.Ordinal);
        if (index < 0) return false;
        return code.IndexOf("=>", System.StringComparison.Ordinal) < 0;
    }

    private static bool ContainsCall(string code, string methodName)
    {
        string needle = methodName + "(";
        int index = -1;
        while (true)
        {
            index = code.IndexOf(needle, index + 1, System.StringComparison.Ordinal);
            if (index < 0) return false;
            if (index == 0) return true;
            char previous = code[index - 1];
            if (!char.IsLetterOrDigit(previous) && previous != '_') return true;
        }
    }

    private static int CountChar(string text, char value)
    {
        int count = 0;
        for (int i = 0; i < text.Length; i++)
            if (text[i] == value)
                count++;
        return count;
    }

    private static string StripLineComment(string line)
    {
        if (string.IsNullOrEmpty(line)) return string.Empty;
        int comment = line.IndexOf("//", System.StringComparison.Ordinal);
        return comment >= 0 ? line.Substring(0, comment) : line;
    }

    private static string ResolveLiteRoot()
    {
        return Path.Combine(Application.dataPath, "OUT", "OUT_Core", "OUT_CORE_Lite");
    }

    private static string ToAssetPath(string fileOrFolder)
    {
        string f = fileOrFolder.Replace('\\', '/');
        string data = Application.dataPath.Replace('\\', '/');
        if (f.StartsWith(data, System.StringComparison.OrdinalIgnoreCase))
            return "Assets/" + f.Substring(data.Length).TrimStart('/');
        return f;
    }

    private static string NormalizePath(string path)
    {
        return string.IsNullOrEmpty(path) ? string.Empty : path.Replace('\\', '/');
    }

    private static bool IsLowLevelPoolBoundary(string p)
    {
        return EndsWithPath(p, "/Core/OUTL_PoolSystem.cs")
            || EndsWithPath(p, "/Core/OUTL_PoolFacade.cs")
            || EndsWithPath(p, "/Core/OUTL_PoolAPI.cs")
            || EndsWithPath(p, "/Pool/OUT.cs");
    }

    private static bool IsWorldBootstrapBoundary(string p)
    {
        return EndsWithPath(p, "/Core/OUTL_World.cs") || EndsWithPath(p, "/Core/OUTL_TickProfile.cs");
    }

    private static bool IsEditorBoundary(string p)
    {
        return p.IndexOf("/Editor/", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsDebugOrTestBoundary(string p)
    {
        return p.IndexOf("/Debug/", System.StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("/StressTest/", System.StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("/Tests/", System.StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("/Test/", System.StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("SmokeRunner", System.StringComparison.OrdinalIgnoreCase) >= 0
            || p.IndexOf("GoldenTest", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsTemplateBoundary(string p)
    {
        return p.IndexOf("/Templates/", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsConsoleBoundary(string p)
    {
        return p.IndexOf("/Console/", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsWorldgenBoundary(string p)
    {
        return p.IndexOf("/Worldgen/", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsEditorAuthoringPartial(string p)
    {
        return p.EndsWith(".EditorAuthoring.cs", System.StringComparison.OrdinalIgnoreCase)
            || p.EndsWith(".Authoring.cs", System.StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsCanonicalPoolFacadeUse(string code)
    {
        if (string.IsNullOrEmpty(code)) return false;
        return code.IndexOf("OutCore.pool.OUT.", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUT.Instantiate", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUT.Destroy", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUT.Release", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUTL_PoolSystem.SpawnShared", System.StringComparison.Ordinal) >= 0
            || code.IndexOf("OUTL_PoolSystem.ReleaseShared", System.StringComparison.Ordinal) >= 0;
    }

    private static bool EndsWithPath(string path, string suffix)
    {
        return path.EndsWith(suffix, System.StringComparison.OrdinalIgnoreCase);
    }
}
#endif

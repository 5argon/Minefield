using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace E7.Minefield
{
    /// <summary>
    /// One capture, as remembered between runs.
    /// </summary>
    [Serializable]
    internal class ShotRecord
    {
        public string name;
        public string variant;
        public string description;
        public string[] tags;
        public int width;
        public int height;
        public bool hasBaseline;
        public int baselineWidth;
        public int baselineHeight;
        public float mismatchRatio;
    }

    /// <summary>
    /// Every capture the report knows about, plus the order names were first seen in.
    /// </summary>
    [Serializable]
    internal class ShotManifest
    {
        public List<ShotRecord> shots = new List<ShotRecord>();
        public List<string> nameOrder = new List<string>();
        public List<string> variantOrder = new List<string>();
    }

    /// <summary>
    /// Keeps the on-disk manifest of captures and rewrites the HTML report from it.
    /// </summary>
    /// <remarks>
    /// The report is a small static site rather than one page : an index listing every folder of
    /// capture points, and a page per folder that you click into and back out of. A run that
    /// photographs thirteen games in three languages produces far too many images to scroll
    /// through at once, but one folder of them at a time is the amount a person can judge.
    ///
    /// Once there are hundreds of captures, browsing is not enough, so the index carries a search
    /// over every capture point's folder, name, and description, and each page can be narrowed to
    /// the variants and the capture points you care about.
    ///
    /// Pages are rebuilt after every single capture rather than once at the end of the run. That
    /// costs almost nothing for files this size and means an interrupted or failing run still
    /// leaves a readable report of everything up to that point.
    ///
    /// The manifest is what lets several runs accumulate. Running the same fixture once per
    /// language gives each capture point a column per language, even though each run only knew
    /// about its own.
    /// </remarks>
    internal static class ScreenshotReport
    {
        private const string ManifestFileName = "manifest.json";
        private const string IndexFileName = "index.html";
        private const string StyleFileName = "screenshots.css";
        private const string ScriptFileName = "screenshots.js";

        internal static string ReportPath(string outputRoot) => Path.Combine(outputRoot, IndexFileName);

        /// <summary>
        /// Files a capture into the manifest, replacing any earlier capture of the same name and
        /// variant, then rewrites the index and the page of the folder it belongs to.
        /// </summary>
        internal static void Record(string outputRoot, ShotRecord record)
        {
            ShotManifest manifest = Load(outputRoot);

            int existing = manifest.shots.FindIndex(s => s.name == record.name && s.variant == record.variant);
            if (existing >= 0)
            {
                manifest.shots[existing] = record;
            }
            else
            {
                manifest.shots.Add(record);
            }

            if (manifest.nameOrder.Contains(record.name) == false)
            {
                manifest.nameOrder.Add(record.name);
            }
            if (manifest.variantOrder.Contains(record.variant) == false)
            {
                manifest.variantOrder.Add(record.variant);
            }

            Save(outputRoot, manifest);

            File.WriteAllText(Path.Combine(outputRoot, StyleFileName), Style, Encoding.UTF8);
            File.WriteAllText(Path.Combine(outputRoot, ScriptFileName), Script, Encoding.UTF8);
            File.WriteAllText(ReportPath(outputRoot), BuildIndex(manifest), Encoding.UTF8);

            string folder = FolderOf(record.name);
            File.WriteAllText(
                Path.Combine(outputRoot, PageFileName(folder)),
                BuildFolderPage(manifest, folder),
                Encoding.UTF8);
        }

        /// <summary>
        /// Forgets every capture and removes every generated page, so the next run starts fresh.
        /// </summary>
        internal static void Clear(string outputRoot)
        {
            DeleteIfExists(Path.Combine(outputRoot, ManifestFileName));
            DeleteIfExists(Path.Combine(outputRoot, StyleFileName));
            DeleteIfExists(Path.Combine(outputRoot, ScriptFileName));

            if (Directory.Exists(outputRoot))
            {
                foreach (string page in Directory.GetFiles(outputRoot, "*.html", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(page);
                }
            }
        }

        /// <summary>
        /// The folder part of a capture name, which is everything before the last separator.
        /// </summary>
        private static string FolderOf(string name)
        {
            int lastSeparator = name.LastIndexOf('/');
            return lastSeparator < 0 ? string.Empty : name.Substring(0, lastSeparator);
        }

        /// <summary>
        /// The capture point's own name, without the folder it sits in.
        /// </summary>
        private static string LeafOf(string name)
        {
            int lastSeparator = name.LastIndexOf('/');
            return lastSeparator < 0 ? name : name.Substring(lastSeparator + 1);
        }

        private static string Slug(string value)
        {
            StringBuilder slug = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                slug.Append(char.IsLetterOrDigit(c) || c == '-' || c == '_' ? c : '-');
            }
            return slug.ToString();
        }

        private static string PageFileName(string folder)
            => folder.Length == 0 ? "folder-root.html" : "folder-" + Slug(folder) + ".html";

        private static string AnchorOf(string name) => "shot-" + Slug(LeafOf(name));

        private static string FolderLabel(string folder) => folder.Length == 0 ? "(ungrouped)" : folder;

        /// <summary>
        /// The description of a capture point, taken from whichever variant recorded one.
        /// </summary>
        private static string DescriptionOf(ShotManifest manifest, string name)
        {
            foreach (ShotRecord shot in manifest.shots)
            {
                if (shot.name == name && string.IsNullOrEmpty(shot.description) == false)
                {
                    return shot.description;
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Every tag a capture point carries, gathered across the variants that recorded it.
        /// </summary>
        private static List<string> TagsOf(ShotManifest manifest, string name)
        {
            List<string> tags = new List<string>();
            foreach (ShotRecord shot in manifest.shots)
            {
                if (shot.name != name || shot.tags == null)
                {
                    continue;
                }
                foreach (string tag in shot.tags)
                {
                    if (tags.Contains(tag) == false)
                    {
                        tags.Add(tag);
                    }
                }
            }
            return tags;
        }

        /// <summary>
        /// Every tag in the report, in the order they were first captured.
        /// </summary>
        private static List<string> AllTags(ShotManifest manifest, string folder = null)
        {
            List<string> tags = new List<string>();
            foreach (string name in manifest.nameOrder)
            {
                if (folder != null && FolderOf(name) != folder)
                {
                    continue;
                }
                foreach (string tag in TagsOf(manifest, name))
                {
                    if (tags.Contains(tag) == false)
                    {
                        tags.Add(tag);
                    }
                }
            }
            return tags;
        }

        /// <summary>
        /// The tag chips, which cycle between leaving a tag alone, showing only what carries it,
        /// and hiding what carries it.
        /// </summary>
        private static string TagChips(List<string> tags)
        {
            if (tags.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder html = new StringBuilder();
            html.Append("<span class=\"tags\" id=\"tags\" title=\"Click to show only this tag, again to hide it\">");
            foreach (string tag in tags)
            {
                html.Append($"<button type=\"button\" class=\"tag\" data-tag=\"{Escape(tag)}\" data-state=\"0\">{Escape(tag)}</button>");
            }
            html.Append("<button type=\"button\" class=\"tag reset\" id=\"tags-reset\">clear</button></span>");
            return html.ToString();
        }

        private static ShotManifest Load(string outputRoot)
        {
            string path = Path.Combine(outputRoot, ManifestFileName);
            if (File.Exists(path) == false)
            {
                return new ShotManifest();
            }

            try
            {
                ShotManifest loaded = JsonUtility.FromJson<ShotManifest>(File.ReadAllText(path, Encoding.UTF8));
                return loaded ?? new ShotManifest();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Minefield] Screenshot manifest at {path} could not be read, starting a new one. {e.Message}");
                return new ShotManifest();
            }
        }

        private static void Save(string outputRoot, ShotManifest manifest)
        {
            Directory.CreateDirectory(outputRoot);
            File.WriteAllText(
                Path.Combine(outputRoot, ManifestFileName),
                JsonUtility.ToJson(manifest, prettyPrint: true),
                Encoding.UTF8);
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        /// <summary>
        /// Every folder that has captures, in the order their first capture was taken.
        /// </summary>
        private static List<string> FoldersOf(ShotManifest manifest)
        {
            List<string> folders = new List<string>();
            foreach (string name in manifest.nameOrder)
            {
                string folder = FolderOf(name);
                if (folders.Contains(folder) == false)
                {
                    folders.Add(folder);
                }
            }
            return folders;
        }

        private static Dictionary<string, ShotRecord> Index(ShotManifest manifest)
        {
            Dictionary<string, ShotRecord> byKey = new Dictionary<string, ShotRecord>();
            foreach (ShotRecord shot in manifest.shots)
            {
                byKey[shot.name + " " + shot.variant] = shot;
            }
            return byKey;
        }

        private static string SourceOf(string folder, ShotRecord shot)
            => $"{folder}/{shot.name}.{shot.variant}.png";

        private static string BuildIndex(ShotManifest manifest)
        {
            Dictionary<string, ShotRecord> byKey = Index(manifest);
            List<string> folders = FoldersOf(manifest);

            StringBuilder html = new StringBuilder();
            html.Append(Head("Minefield screenshots"));

            html.Append("<header><h1>Minefield screenshots</h1><p class=\"sub\">");
            html.Append($"{folders.Count} folders &middot; {manifest.nameOrder.Count} capture points &middot; ");
            html.Append($"{manifest.shots.Count} images &middot; {manifest.variantOrder.Count} variants</p>");
            html.Append("<div class=\"controls\">");
            html.Append("<input type=\"search\" id=\"search\" placeholder=\"Search every capture point&hellip;\" autocomplete=\"off\">");
            html.Append("<span class=\"seg\">");
            html.Append("<label><input type=\"radio\" name=\"view\" value=\"folders\" checked> Folders</label>");
            html.Append("<label><input type=\"radio\" name=\"view\" value=\"all\"> All captures</label>");
            html.Append("</span>");
            html.Append(TagChips(AllTags(manifest)));
            html.Append("</div></header>");

            html.Append("<main>");

            html.Append("<table class=\"folders\" id=\"folders\"><thead><tr>");
            html.Append("<th>Folder</th><th class=\"num\">Points</th><th class=\"num\">Images</th>");
            html.Append("<th>Variants</th><th>Baseline</th></tr></thead><tbody>");

            foreach (string folder in folders)
            {
                int points = 0;
                int images = 0;
                int compared = 0;
                int drifted = 0;
                List<string> variants = new List<string>();

                foreach (string name in manifest.nameOrder)
                {
                    if (FolderOf(name) != folder)
                    {
                        continue;
                    }
                    points++;
                    foreach (string variant in manifest.variantOrder)
                    {
                        if (byKey.TryGetValue(name + " " + variant, out ShotRecord shot) == false)
                        {
                            continue;
                        }
                        images++;
                        if (variants.Contains(variant) == false)
                        {
                            variants.Add(variant);
                        }
                        if (shot.hasBaseline)
                        {
                            compared++;
                            if (shot.mismatchRatio > 0f)
                            {
                                drifted++;
                            }
                        }
                    }
                }

                string status = compared == 0
                    ? "<span class=\"dim\">no baseline</span>"
                    : drifted == 0
                        ? "<span class=\"badge good\">unchanged</span>"
                        : $"<span class=\"badge bad\">{drifted} drifted</span>";

                html.Append("<tr>");
                html.Append($"<td><a href=\"{Escape(PageFileName(folder))}\">{Escape(FolderLabel(folder))}</a></td>");
                html.Append($"<td class=\"num\">{points}</td><td class=\"num\">{images}</td>");
                html.Append($"<td>{Escape(string.Join(", ", variants.ToArray()))}</td>");
                html.Append($"<td>{status}</td></tr>");
            }

            html.Append("</tbody></table>");
            html.Append("<div id=\"all\" class=\"cards\"></div>");
            html.Append("<div id=\"results\" class=\"cards\"></div>");
            html.Append("</main>");

            html.Append("<script>window.MINEFIELD_SHOTS = ");
            html.Append(SearchIndexJson(manifest, byKey));
            html.Append(";</script>");
            html.Append(Tail());
            return html.ToString();
        }

        /// <summary>
        /// The data the index page searches over, one entry per capture point.
        /// </summary>
        private static string SearchIndexJson(ShotManifest manifest, Dictionary<string, ShotRecord> byKey)
        {
            StringBuilder json = new StringBuilder("[");
            bool first = true;

            foreach (string name in manifest.nameOrder)
            {
                ShotRecord thumbnail = null;
                bool drifted = false;
                int images = 0;
                foreach (string variant in manifest.variantOrder)
                {
                    if (byKey.TryGetValue(name + " " + variant, out ShotRecord shot) == false)
                    {
                        continue;
                    }
                    images++;
                    thumbnail = thumbnail ?? shot;
                    if (shot.hasBaseline && shot.mismatchRatio > 0f)
                    {
                        drifted = true;
                    }
                }

                if (thumbnail == null)
                {
                    continue;
                }

                if (first == false)
                {
                    json.Append(',');
                }
                first = false;

                json.Append('{');
                json.Append($"\"f\":{JsonString(FolderLabel(FolderOf(name)))},");
                json.Append($"\"n\":{JsonString(LeafOf(name))},");
                json.Append($"\"d\":{JsonString(DescriptionOf(manifest, name))},");
                json.Append($"\"p\":{JsonString(PageFileName(FolderOf(name)) + "#" + AnchorOf(name))},");
                json.Append($"\"t\":{JsonString(SourceOf(Screenshot.CurrentFolder, thumbnail))},");
                json.Append("\"g\":[");
                List<string> tags = TagsOf(manifest, name);
                for (int i = 0; i < tags.Count; i++)
                {
                    json.Append(i == 0 ? string.Empty : ",").Append(JsonString(tags[i]));
                }
                json.Append("],");
                json.Append($"\"i\":{images},");
                json.Append($"\"x\":{(drifted ? "true" : "false")}");
                json.Append('}');
            }

            return json.Append(']').ToString();
        }

        private static string BuildFolderPage(ShotManifest manifest, string folder)
        {
            Dictionary<string, ShotRecord> byKey = Index(manifest);
            string label = FolderLabel(folder);

            StringBuilder html = new StringBuilder();
            html.Append(Head(label));

            html.Append("<header>");
            html.Append($"<nav class=\"crumbs\"><a href=\"{IndexFileName}\">&larr; All folders</a></nav>");
            html.Append($"<h1>{Escape(label)}</h1>");

            html.Append("<div class=\"controls\">");
            html.Append("<input type=\"search\" id=\"search\" placeholder=\"Filter capture points&hellip;\" autocomplete=\"off\">");
            html.Append("<span class=\"seg\">");
            html.Append("<label><input type=\"radio\" name=\"view\" value=\"columns\" checked> Columns</label>");
            html.Append("<label><input type=\"radio\" name=\"view\" value=\"grid\"> Grid</label>");
            html.Append("</span>");
            html.Append("<span class=\"seg\">");
            html.Append("<label><input type=\"radio\" name=\"mode\" value=\"variants\" checked> Current</label>");
            html.Append("<label><input type=\"radio\" name=\"mode\" value=\"baseline\"> Vs baseline</label>");
            html.Append("</span>");
            html.Append("<label class=\"size\">Size <input type=\"range\" id=\"size\" min=\"120\" max=\"640\" step=\"20\" value=\"300\"></label>");
            html.Append("<span class=\"seg\">");
            foreach (string variant in manifest.variantOrder)
            {
                html.Append($"<label><input type=\"checkbox\" data-variant-filter=\"{Escape(variant)}\" checked> {Escape(variant)}</label>");
            }
            html.Append("</span>");
            html.Append("<label><input type=\"checkbox\" id=\"only-drifted\"> Only drifted</label>");
            html.Append(TagChips(AllTags(manifest, folder)));
            html.Append("</div></header>");

            html.Append("<main>");
            foreach (string name in manifest.nameOrder)
            {
                if (FolderOf(name) != folder)
                {
                    continue;
                }

                bool rowDrifted = false;
                foreach (string variant in manifest.variantOrder)
                {
                    if (byKey.TryGetValue(name + " " + variant, out ShotRecord found) && found.hasBaseline && found.mismatchRatio > 0f)
                    {
                        rowDrifted = true;
                    }
                }

                string leaf = LeafOf(name);
                string description = DescriptionOf(manifest, name);
                List<string> tags = TagsOf(manifest, name);
                string tagList = tags.Count == 0 ? string.Empty : "|" + string.Join("|", tags.ToArray()) + "|";
                string haystack = (leaf + " " + description + " " + string.Join(" ", tags.ToArray())).ToLowerInvariant();

                html.Append($"<section class=\"shot{(rowDrifted ? " drifted" : string.Empty)}\" ");
                html.Append($"id=\"{Escape(AnchorOf(name))}\" data-search=\"{Escape(haystack)}\" ");
                html.Append($"data-tags=\"{Escape(tagList)}\">");
                html.Append($"<h2>{Escape(leaf)}</h2>");
                if (description.Length > 0)
                {
                    html.Append($"<p class=\"desc\">{Escape(description)}</p>");
                }
                if (tags.Count > 0)
                {
                    html.Append("<p class=\"taglist\">");
                    foreach (string tag in tags)
                    {
                        html.Append($"<span class=\"tag static\">{Escape(tag)}</span>");
                    }
                    html.Append("</p>");
                }
                html.Append("<div class=\"variants\">");

                foreach (string variant in manifest.variantOrder)
                {
                    if (byKey.TryGetValue(name + " " + variant, out ShotRecord shot) == false)
                    {
                        html.Append($"<div class=\"variant missing\" data-variant=\"{Escape(variant)}\">");
                        html.Append($"<h3>{Escape(variant)}</h3><p>not captured</p></div>");
                        continue;
                    }

                    html.Append($"<div class=\"variant\" data-variant=\"{Escape(variant)}\">");
                    html.Append($"<h3>{Escape(variant)} <span class=\"dim\">{shot.width}&times;{shot.height}</span>");
                    if (shot.hasBaseline)
                    {
                        string badge = shot.mismatchRatio > 0f ? "bad" : "good";
                        html.Append($" <span class=\"badge {badge}\">{shot.mismatchRatio.ToString("P3", CultureInfo.InvariantCulture)}</span>");
                    }
                    html.Append("</h3>");

                    html.Append(Figure("current", "current", Screenshot.CurrentFolder, shot));
                    if (shot.hasBaseline)
                    {
                        html.Append(Figure("baseline", "baseline", Screenshot.BaselineFolder, shot));
                        if (shot.mismatchRatio > 0f)
                        {
                            if (shot.baselineWidth != shot.width || shot.baselineHeight != shot.height)
                            {
                                html.Append("<figure class=\"baseline-only note\"><figcaption>baseline was " +
                                    $"{shot.baselineWidth}&times;{shot.baselineHeight}, not comparable</figcaption></figure>");
                            }
                            else
                            {
                                html.Append(Figure("baseline-only", "diff", Screenshot.DiffFolder, shot));
                            }
                        }
                    }
                    html.Append("</div>");
                }

                html.Append("</div></section>");
            }
            html.Append("<p id=\"empty\" class=\"dim\" hidden>Nothing matched.</p>");
            html.Append("</main>");

            html.Append(Tail());
            return html.ToString();
        }

        private static string Figure(string cssClass, string caption, string folder, ShotRecord shot)
        {
            string source = SourceOf(folder, shot);
            return $"<figure class=\"{cssClass}\"><a href=\"{Escape(source)}\" target=\"_blank\">" +
                $"<img loading=\"lazy\" src=\"{Escape(source)}\" alt=\"{Escape(caption)} {Escape(shot.name)}\"></a>" +
                $"<figcaption>{caption}</figcaption></figure>";
        }

        private static string Escape(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        /// <summary>
        /// A JSON string literal, safe to drop into the script block of a generated page.
        /// </summary>
        private static string JsonString(string value)
        {
            StringBuilder json = new StringBuilder(value.Length + 2);
            json.Append('"');
            foreach (char c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"': json.Append("\\\""); break;
                    case '\\': json.Append("\\\\"); break;
                    case '\n': json.Append("\\n"); break;
                    case '\r': json.Append("\\r"); break;
                    case '\t': json.Append("\\t"); break;
                    //Closing an inline script early is the one way page data could become markup.
                    case '<': json.Append("\\u003c"); break;
                    case '>': json.Append("\\u003e"); break;
                    case '&': json.Append("\\u0026"); break;
                    default:
                        if (c < ' ')
                        {
                            json.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            json.Append(c);
                        }
                        break;
                }
            }
            return json.Append('"').ToString();
        }

        private static string Head(string title)
        {
            return "<!doctype html>\n<html lang=\"en\">\n<head>\n<meta charset=\"utf-8\">\n" +
                "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">\n" +
                $"<title>{Escape(title)}</title>\n" +
                $"<link rel=\"stylesheet\" href=\"{StyleFileName}\">\n</head>\n" +
                "<body class=\"mode-variants view-columns view-folders\">\n";
        }

        private static string Tail() => $"<script src=\"{ScriptFileName}\"></script>\n</body>\n</html>\n";

        private const string Style =
@":root { color-scheme: light dark; --bg: #fff; --fg: #16161a; --dim: #6b6b76; --line: #e2e2e8;
  --card: #fafafc; --link: #2c5fd8; --shot-w: 300px; }
@media (prefers-color-scheme: dark) {
  :root { --bg: #131316; --fg: #ecedf1; --dim: #93939f; --line: #2c2c33; --card: #1b1b1f; --link: #7fa4ff; }
}
* { box-sizing: border-box; }
body { margin: 0; background: var(--bg); color: var(--fg);
  font: 14px/1.5 ui-sans-serif, -apple-system, ""Segoe UI"", sans-serif; }
a { color: var(--link); }
header { position: sticky; top: 0; z-index: 2; padding: 14px 20px;
  background: var(--bg); border-bottom: 1px solid var(--line); }
h1 { margin: 0; font-size: 17px; letter-spacing: -0.01em; }
.crumbs { margin-bottom: 8px; font-size: 13px; }
.sub { margin: 4px 0 0; color: var(--dim); }
.controls { display: flex; gap: 14px; flex-wrap: wrap; align-items: center; margin-top: 10px; }
.controls label { display: flex; align-items: center; gap: 6px; cursor: pointer; user-select: none; }
.seg { display: flex; gap: 12px; align-items: center; padding: 3px 10px;
  border: 1px solid var(--line); border-radius: 999px; }
input[type=search] { flex: 1 1 260px; min-width: 200px; padding: 7px 12px; font: inherit;
  color: inherit; background: var(--card); border: 1px solid var(--line); border-radius: 999px; }
input[type=range] { width: 120px; }
main { padding: 8px 20px 64px; }
table.folders { border-collapse: collapse; width: 100%; max-width: 900px; margin-top: 12px; }
table.folders th, table.folders td { text-align: left; padding: 9px 12px; border-bottom: 1px solid var(--line); }
table.folders th { font-size: 11px; text-transform: uppercase; letter-spacing: 0.05em; color: var(--dim); }
table.folders td.num, table.folders th.num { text-align: right; font-variant-numeric: tabular-nums; }
table.folders tbody tr:hover { background: var(--card); }
section.shot { padding: 18px 0; border-bottom: 1px solid var(--line); }
h2 { margin: 0; font-size: 15px; font-weight: 600; }
.desc { margin: 4px 0 0; color: var(--dim); max-width: 70ch; }
.variants { display: flex; gap: 18px; align-items: flex-start; overflow-x: auto;
  padding-bottom: 6px; margin-top: 12px; }
.variant { flex: 0 0 auto; background: var(--card); border: 1px solid var(--line);
  border-radius: 10px; padding: 10px; }
.variant.missing { color: var(--dim); min-width: 160px; }
h3 { margin: 0 0 8px; font-size: 12px; font-weight: 600; text-transform: uppercase;
  letter-spacing: 0.04em; display: flex; align-items: center; gap: 8px; }
.dim { color: var(--dim); font-weight: 400; text-transform: none; letter-spacing: 0; }
.badge { border-radius: 999px; padding: 1px 8px; font-weight: 600; letter-spacing: 0;
  text-transform: none; font-size: 11px; }
.badge.good { background: #1f7a3f22; color: #1f7a3f; }
.badge.bad { background: #c0303022; color: #d0453f; }
figure { margin: 0; display: inline-block; vertical-align: top; }
figure + figure { margin-left: 10px; }
img { display: block; width: var(--shot-w); max-width: 100%; height: auto;
  border-radius: 6px; background: #7772; }
figcaption { margin-top: 4px; color: var(--dim); font-size: 11px; text-align: center; }
figure.note { width: var(--shot-w); }
.cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr));
  gap: 12px; margin-top: 14px; }
a.hit { display: flex; gap: 12px; padding: 10px; text-decoration: none; color: inherit;
  background: var(--card); border: 1px solid var(--line); border-radius: 10px; }
a.hit:hover { border-color: var(--link); }
a.hit img { width: 64px; flex: 0 0 auto; }
a.hit strong { display: block; }
a.hit .where { color: var(--dim); font-size: 12px; }
a.hit p { margin: 4px 0 0; color: var(--dim); font-size: 12px; }
.tags { display: flex; gap: 6px; flex-wrap: wrap; align-items: center; }
.tag { font: inherit; font-size: 12px; padding: 2px 10px; border-radius: 999px; cursor: pointer;
  color: var(--dim); background: transparent; border: 1px solid var(--line); }
.tag:hover { color: var(--fg); }
.tag[data-state=""1""] { background: #1f7a3f22; border-color: #1f7a3f66; color: #1f7a3f; font-weight: 600; }
.tag[data-state=""2""] { background: #c0303022; border-color: #c0303066; color: #d0453f;
  text-decoration: line-through; }
.tag.reset { border-style: dashed; }
.tag.static { cursor: default; pointer-events: none; }
.taglist { display: flex; gap: 6px; flex-wrap: wrap; margin: 8px 0 0; }
body.mode-variants .baseline, body.mode-variants .baseline-only { display: none; }
body.only-drifted section.shot:not(.drifted) { display: none; }
section.shot.filtered { display: none; }
body.view-grid main { display: grid; align-items: start; gap: 14px;
  grid-template-columns: repeat(auto-fill, minmax(calc(var(--shot-w) + 44px), 1fr)); }
body.view-grid section.shot { border: 1px solid var(--line); border-radius: 12px;
  padding: 12px; margin: 0; }
body.view-grid .variants { flex-wrap: wrap; overflow: visible; gap: 10px; }
body:not(.view-all) #all { display: none; }
body.view-all #folders { display: none; }
body:not(.searching) #results { display: none; }
body.searching #folders, body.searching #all { display: none; }
";

        private const string Script =
@"(function () {
  var body = document.body;
  var shots = window.MINEFIELD_SHOTS;
  var search = document.getElementById('search');
  var chips = Array.prototype.slice.call(document.querySelectorAll('.tag[data-tag]'));

  var escapeHtml = function (value) {
    return String(value).replace(/[&<>""]/g, function (c) {
      return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '""': '&quot;' }[c];
    });
  };

  var query = function () { return search ? search.value.trim().toLowerCase() : ''; };

  var chipsOf = function (state) {
    return chips.filter(function (chip) { return chip.getAttribute('data-state') === state; })
      .map(function (chip) { return chip.getAttribute('data-tag'); });
  };

  var tagsFiltering = function () { return chipsOf('1').length > 0 || chipsOf('2').length > 0; };

  // A capture survives when it carries none of the pushed-aside tags, and, if any tag was asked
  // for, at least one of those.
  var passesTags = function (has) {
    var hidden = chipsOf('2');
    for (var i = 0; i < hidden.length; i++) { if (has(hidden[i])) { return false; } }
    var wanted = chipsOf('1');
    if (wanted.length === 0) { return true; }
    for (var j = 0; j < wanted.length; j++) { if (has(wanted[j])) { return true; } }
    return false;
  };

  var renderInto = function (host, list) {
    if (!host) { return; }
    if (!list.length) { host.innerHTML = '<p class=""dim"">Nothing matched.</p>'; return; }
    host.innerHTML = list.map(function (s) {
      return '<a class=""hit"" href=""' + escapeHtml(s.p) + '"">' +
        '<img loading=""lazy"" src=""' + escapeHtml(s.t) + '"" alt="""">' +
        '<span><strong>' + escapeHtml(s.n) + (s.x ? ' &bull; drifted' : '') + '</strong>' +
        '<span class=""where"">' + escapeHtml(s.f) + '</span>' +
        (s.d ? '<p>' + escapeHtml(s.d) + '</p>' : '') + '</span></a>';
    }).join('');
  };

  var apply = function () {
    var q = query();

    if (shots) {
      var list = shots.filter(function (s) {
        var tags = s.g || [];
        var matches = passesTags(function (tag) { return tags.indexOf(tag) >= 0; });
        if (!matches) { return false; }
        return !q || (s.f + ' ' + s.n + ' ' + s.d + ' ' + tags.join(' ')).toLowerCase().indexOf(q) >= 0;
      });
      var narrowing = q.length > 0 || tagsFiltering();
      body.classList.toggle('searching', narrowing);
      if (narrowing) { renderInto(document.getElementById('results'), list); }
      else if (body.classList.contains('view-all')) { renderInto(document.getElementById('all'), list); }
      return;
    }

    var shown = 0;
    document.querySelectorAll('section.shot').forEach(function (section) {
      var carried = section.getAttribute('data-tags') || '';
      var hit = passesTags(function (tag) { return carried.indexOf('|' + tag + '|') >= 0; }) &&
        (!q || section.getAttribute('data-search').indexOf(q) >= 0);
      section.classList.toggle('filtered', !hit);
      if (hit) { shown++; }
    });
    var empty = document.getElementById('empty');
    if (empty) { empty.hidden = shown > 0; }
  };

  var size = document.getElementById('size');
  if (size) {
    var applySize = function () { body.style.setProperty('--shot-w', size.value + 'px'); };
    size.addEventListener('input', applySize);
    applySize();
  }

  document.querySelectorAll('input[name=view]').forEach(function (radio) {
    radio.addEventListener('change', function () {
      if (!radio.checked) { return; }
      body.classList.toggle('view-grid', radio.value === 'grid');
      body.classList.toggle('view-columns', radio.value === 'columns');
      body.classList.toggle('view-all', radio.value === 'all');
      body.classList.toggle('view-folders', radio.value === 'folders');
      apply();
    });
  });

  document.querySelectorAll('input[name=mode]').forEach(function (radio) {
    radio.addEventListener('change', function () {
      if (!radio.checked) { return; }
      body.classList.toggle('mode-variants', radio.value === 'variants');
    });
  });

  var drifted = document.getElementById('only-drifted');
  if (drifted) {
    drifted.addEventListener('change', function () {
      body.classList.toggle('only-drifted', drifted.checked);
    });
  }

  var variantBoxes = Array.prototype.slice.call(document.querySelectorAll('input[data-variant-filter]'));
  var applyVariants = function () {
    variantBoxes.forEach(function (box) {
      var wanted = box.getAttribute('data-variant-filter');
      document.querySelectorAll('.variant').forEach(function (card) {
        if (card.getAttribute('data-variant') === wanted) {
          card.style.display = box.checked ? '' : 'none';
        }
      });
    });
  };
  variantBoxes.forEach(function (box) { box.addEventListener('change', applyVariants); });

  chips.forEach(function (chip) {
    chip.addEventListener('click', function () {
      var next = (parseInt(chip.getAttribute('data-state'), 10) + 1) % 3;
      chip.setAttribute('data-state', String(next));
      apply();
    });
  });

  var reset = document.getElementById('tags-reset');
  if (reset) {
    reset.addEventListener('click', function () {
      chips.forEach(function (chip) { chip.setAttribute('data-state', '0'); });
      apply();
    });
  }

  if (search) { search.addEventListener('input', apply); }
})();
";
    }
}

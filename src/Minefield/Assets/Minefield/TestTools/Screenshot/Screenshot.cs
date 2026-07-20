using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace E7.Minefield
{
    /// <summary>
    /// Captures the game's back buffer at chosen moments of a play mode test, writes each capture to
    /// disk as a PNG, and maintains an HTML report that lays every capture out side by side.
    /// </summary>
    /// <remarks>
    /// The intended use is a dedicated set of tests that navigate the game and do nothing but
    /// <see cref="Take(string)"/> at interesting places. Because a beacon becoming
    /// <see cref="Is.Clickable"/> already means "the UI has settled into a known state", the places
    /// where you were going to wait for a beacon are exactly the places worth capturing.
    ///
    /// Two jobs are served by the same run :
    ///
    /// - **Regression** : a capture is compared against `Baseline/` if a file of the same name is
    /// there, and the report shows baseline, current, and a diff image together.
    /// - **Gathering references** : run the same tests once per <see cref="Variant"/> (a language,
    /// a device aspect ratio, a difficulty) and the report gains one column per variant, so every
    /// screen can be eyeballed across all of them at once. This is the artifact to hand to a
    /// translator, both to show them where their text will land and to review what they sent back.
    ///
    /// Capture reads the back buffer rather than rendering a camera into a
    /// <see cref="RenderTexture"/>. That is deliberate : a `Screen Space - Overlay` canvas does
    /// **not** appear in a camera render, and overlay canvases are usually the entire thing being
    /// localized. It also means the capture is exactly the pixels the player would have seen.
    ///
    /// Output goes outside the `Assets` folder so Unity never imports it, at
    /// `&lt;project&gt;/Screenshots` in the editor and <see cref="Application.persistentDataPath"/>
    /// in a player.
    /// </remarks>
    public static class Screenshot
    {
        /// <summary>
        /// Where captures, baselines, diffs, the manifest, and the report are written.
        /// Defaults to a `Screenshots` folder next to the `Assets` folder in the editor,
        /// and to <see cref="Application.persistentDataPath"/> in a player.
        /// </summary>
        public static string OutputRoot { get; set; } = DefaultOutputRoot();

        /// <summary>
        /// Names the axis you are varying between runs. It becomes the suffix of every file name
        /// and a column in the report. A language code is the typical value. Captures of different
        /// variants never overwrite each other, so several runs accumulate into one report.
        /// </summary>
        public static string Variant { get; set; } = "default";

        /// <summary>
        /// Turns <see cref="Take(string)"/> into a no-op when `false`, so screenshot calls
        /// may sit inside tests that normally run without paying for any capture.
        /// </summary>
        public static bool Enabled { get; set; } = true;

        /// <summary>
        /// How many frames to let pass before reading the back buffer. Waiting on a beacon proves
        /// the game reached a state, not that it finished drawing it — a fade or a layout rebuild
        /// often needs another frame or two. Raise this if captures come out mid-transition.
        /// </summary>
        public static int SettleFrames { get; set; } = 2;

        /// <summary>
        /// How the screen is read into a texture.
        /// </summary>
        /// <remarks>
        /// Worth changing for one reason : on macOS with Metal and a scriptable render pipeline,
        /// the engine's screen capture module logs `Ignoring depth surface load action as it is
        /// memoryless` on every capture. The message comes from native rendering code, so no log
        /// filter reaches it, and the only way to be rid of it is to read the frame buffer
        /// directly instead. The captures themselves are fine either way — it is noise, not a
        /// symptom.
        ///
        /// Defaults to the module when the project has it, since reading the frame buffer directly
        /// assumes nothing is bound at end of frame, which a render pipeline is free to break.
        /// Falls back to reading directly when the project has no screen capture module, whatever
        /// this is set to.
        /// </remarks>
        public static CaptureMethod Method { get; set; } =
#if HAS_SCREEN_CAPTURE
            CaptureMethod.ScreenCaptureModule;
#else
            CaptureMethod.BackBuffer;
#endif

        /// <summary>
        /// How far one channel of one pixel may drift from the baseline before that pixel counts as
        /// different, in 0-1. The default absorbs the dithering and compression noise that a
        /// GPU produces run to run without hiding a moved or reflowed glyph.
        /// </summary>
        public static float PixelThreshold { get; set; } = 8f / 255f;

        /// <summary>
        /// The share of differing pixels a capture is allowed before <see cref="FailOnMismatch"/>
        /// makes it throw, in 0-1.
        /// </summary>
        public static float MismatchTolerance { get; set; } = 0.002f;

        /// <summary>
        /// Makes <see cref="Take(string)"/> throw a <see cref="ScreenshotMismatchException"/> when a
        /// baseline exists and the capture differs from it by more than
        /// <see cref="MismatchTolerance"/>. Off by default, because a gathering run wants every
        /// capture taken even when earlier ones moved.
        /// </summary>
        public static bool FailOnMismatch { get; set; } = false;

        /// <summary>
        /// The share of differing pixels found by the most recent <see cref="Take(string)"/>, or 0
        /// when that capture had no baseline to compare against.
        /// </summary>
        public static float LastMismatchRatio { get; private set; }

        /// <summary>
        /// Captures the back buffer and files it under <paramref name="name"/>.
        /// </summary>
        /// <remarks>
        /// The name is the identity of this capture across runs and variants, so keep it stable —
        /// it decides which baseline the capture is compared against and which report row it joins.
        /// A `/` in the name makes a subfolder, which is the tidy way to keep one test's captures
        /// together.
        ///
        /// Ordering in the report follows the order names were first seen, not alphabet, so the
        /// report reads as a walkthrough of the flow. Nothing needs a numeric prefix.
        /// </remarks>
        /// <param name="name">Stable identifier for this point in the flow, `/` allowed.</param>
        /// <param name="description">
        /// What someone looking at this capture should know or check, written at the call site so
        /// it stays next to the navigation that produced it. It is shown beside the images in the
        /// report and is searchable there. One language, whichever the team reads.
        /// </param>
        /// <param name="tags">
        /// Labels to slice the report by, such as what kind of screen this is or what it is worth
        /// checking for. The report lists every tag it has seen and lets a reader narrow to the
        /// captures carrying one, or push aside the ones carrying another.
        /// </param>
        /// <exception cref="ScreenshotMismatchException">
        /// When <see cref="FailOnMismatch"/> is on and the capture drifted past
        /// <see cref="MismatchTolerance"/>.
        /// </exception>
        public static async Awaitable Take(string name, string description = null, params string[] tags)
        {
            LastMismatchRatio = 0f;
            if (Enabled == false)
            {
                return;
            }

            string safeName = SanitizeName(name);
            for (int i = 0; i < SettleFrames; i++)
            {
                await Awaitable.NextFrameAsync();
            }
            await Awaitable.EndOfFrameAsync();

            Texture2D captured = ReadBackBuffer();
            try
            {
                string currentPath = PathFor(CurrentFolder, safeName);
                WritePng(currentPath, captured);

                ShotRecord record = new ShotRecord
                {
                    name = safeName,
                    variant = SafeVariant,
                    description = description ?? string.Empty,
                    tags = CleanTags(tags),
                    width = captured.width,
                    height = captured.height,
                };

                string baselinePath = PathFor(BaselineFolder, safeName);
                if (File.Exists(baselinePath))
                {
                    CompareAgainstBaseline(baselinePath, captured, safeName, record);
                }

                LastMismatchRatio = record.mismatchRatio;
                ScreenshotReport.Record(OutputRoot, record);

                if (FailOnMismatch && record.hasBaseline && record.mismatchRatio > MismatchTolerance)
                {
                    throw new ScreenshotMismatchException(
                        $"[Minefield] Screenshot '{safeName}' ({Variant}) differs from its baseline by " +
                        $"{record.mismatchRatio:P3} of pixels, over the {MismatchTolerance:P3} allowed.\n" +
                        $"  baseline : {baselinePath}\n" +
                        $"  current  : {currentPath}\n" +
                        $"  diff     : {PathFor(DiffFolder, safeName)}\n" +
                        $"  report   : {ScreenshotReport.ReportPath(OutputRoot)}");
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(captured);
            }
        }

        /// <summary>
        /// Captures the back buffer, naming the file after a beacon enum member as
        /// `EnumType.Member`. Handy when the capture point is the beacon you just waited on.
        /// </summary>
        /// <param name="beacon">Any beacon enum member, used only for its name.</param>
        /// <param name="description">What someone looking at this capture should know or check.</param>
        /// <param name="tags">Labels to slice the report by.</param>
        public static Awaitable Take(Enum beacon, string description = null, params string[] tags)
            => Take($"{beacon.GetType().Name}.{beacon}", description, tags);

        /// <summary>
        /// Drops blanks and duplicates so the report's tag list stays short and stable.
        /// </summary>
        private static string[] CleanTags(string[] tags)
        {
            if (tags == null || tags.Length == 0)
            {
                return new string[0];
            }

            List<string> cleaned = new List<string>(tags.Length);
            foreach (string tag in tags)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    continue;
                }
                string trimmed = tag.Trim();
                if (trimmed.Length > 0 && cleaned.Contains(trimmed) == false)
                {
                    cleaned.Add(trimmed);
                }
            }
            return cleaned.ToArray();
        }

        /// <summary>
        /// Asks for a fixed capture resolution and waits for the screen to actually take it.
        /// </summary>
        /// <remarks>
        /// Back buffer capture is whatever size the Game View happens to be, and a
        /// <see cref="UI.CanvasScaler"/> set to scale with screen size lays out differently at
        /// different aspect ratios. Comparing a capture taken at one aspect against a baseline taken
        /// at another produces a diff that is all noise, so pin the resolution before the first
        /// capture of a run.
        ///
        /// In the editor this asks the Game View to resize, which it may refuse depending on how its
        /// size dropdown is set. Rather than capture at the wrong size, this throws and tells you to
        /// pick the size by hand.
        /// </remarks>
        /// <param name="width">Wanted back buffer width in pixels.</param>
        /// <param name="height">Wanted back buffer height in pixels.</param>
        /// <param name="settleFrames">How many frames to allow for the resize to land.</param>
        /// <exception cref="ScreenshotMismatchException">When the screen never reached that size.</exception>
        public static async Awaitable RequireResolution(int width, int height, int settleFrames = 10)
        {
            if (Screen.width != width || Screen.height != height)
            {
                Screen.SetResolution(width, height, fullscreen: false);
            }

            for (int i = 0; i < settleFrames; i++)
            {
                if (Screen.width == width && Screen.height == height)
                {
                    return;
                }
                await Awaitable.NextFrameAsync();
            }

            if (Screen.width != width || Screen.height != height)
            {
                throw new ScreenshotMismatchException(
                    $"[Minefield] Screenshots were asked for {width}x{height} but the screen stayed at " +
                    $"{Screen.width}x{Screen.height}. In the editor, set the Game View size dropdown to a " +
                    $"fixed {width}x{height} resolution, otherwise captures cannot be compared to baselines " +
                    $"taken at another size.");
            }
        }

        /// <summary>
        /// Deletes every capture, diff, the manifest, and the report, leaving baselines alone.
        /// Call from `[OneTimeSetUp]` when a run should not inherit rows from an older run.
        /// </summary>
        public static void ClearCaptures()
        {
            DeleteFolder(Path.Combine(OutputRoot, CurrentFolder));
            DeleteFolder(Path.Combine(OutputRoot, DiffFolder));
            ScreenshotReport.Clear(OutputRoot);
        }

        /// <summary>
        /// Copies every capture of the current run over the baselines, which is how a reviewed
        /// change becomes the thing future runs are compared against.
        /// </summary>
        public static void AcceptCapturesAsBaseline()
        {
            string current = Path.Combine(OutputRoot, CurrentFolder);
            if (Directory.Exists(current) == false)
            {
                return;
            }

            foreach (string source in Directory.GetFiles(current, "*.png", SearchOption.AllDirectories))
            {
                string relative = source.Substring(current.Length).TrimStart(Path.DirectorySeparatorChar, '/');
                string destination = Path.Combine(OutputRoot, BaselineFolder, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(source, destination, overwrite: true);
            }
        }

        internal const string CurrentFolder = "Current";
        internal const string BaselineFolder = "Baseline";
        internal const string DiffFolder = "Diff";

        private static void CompareAgainstBaseline(string baselinePath, Texture2D captured, string safeName, ShotRecord record)
        {
            Texture2D baseline = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
            try
            {
                if (baseline.LoadImage(File.ReadAllBytes(baselinePath)) == false)
                {
                    return;
                }

                record.hasBaseline = true;
                record.baselineWidth = baseline.width;
                record.baselineHeight = baseline.height;

                if (baseline.width != captured.width || baseline.height != captured.height)
                {
                    record.mismatchRatio = 1f;
                    return;
                }

                Texture2D diff = ScreenshotComparer.Compare(baseline, captured, PixelThreshold, out float ratio);
                try
                {
                    record.mismatchRatio = ratio;
                    if (ratio > 0f)
                    {
                        WritePng(PathFor(DiffFolder, safeName), diff);
                    }
                    else
                    {
                        DeleteIfExists(PathFor(DiffFolder, safeName));
                    }
                }
                finally
                {
                    UnityEngine.Object.Destroy(diff);
                }
            }
            finally
            {
                UnityEngine.Object.Destroy(baseline);
            }
        }

        /// <summary>
        /// <see cref="Variant"/> reduced to something usable at the end of a file name.
        /// </summary>
        private static string SafeVariant
        {
            get
            {
                string safe = SanitizeName(Variant).Replace('/', '_').Replace('.', '_');
                return safe.Length == 0 ? "default" : safe;
            }
        }

        /// <summary>
        /// Reads what is currently on screen into a texture.
        /// </summary>
        /// <remarks>
        /// Reading the back buffer is what makes this useful for UI work. A `Screen Space - Overlay`
        /// canvas is composited straight onto the back buffer and never appears when a camera is
        /// rendered into a <see cref="RenderTexture"/>, so the camera route would photograph the
        /// game world with the entire interface missing.
        ///
        /// The engine's own screen capture module is used when the project has it, and a direct
        /// <see cref="Texture2D.ReadPixels(Rect, int, int, bool)"/> stands in when it does not.
        /// The fallback trusts that nothing is bound at end of frame, which a render pipeline is
        /// free to break, so a project that photographs its UI is better off with the module on.
        ///
        /// Either way the result is stored without an alpha channel. A back buffer's alpha is
        /// whatever the last shader left there, and carrying that into a PNG makes captures come
        /// out see-through in a browser. Normalizing also keeps a baseline taken through one route
        /// comparable with a capture taken through the other.
        /// </remarks>
        private static Texture2D ReadBackBuffer()
        {
#if HAS_SCREEN_CAPTURE
            if (Method == CaptureMethod.ScreenCaptureModule)
            {
                Texture2D raw = ScreenCapture.CaptureScreenshotAsTexture();
                try
                {
                    Texture2D fromModule = new Texture2D(raw.width, raw.height, TextureFormat.RGB24, mipChain: false);
                    fromModule.SetPixels32(raw.GetPixels32());
                    fromModule.Apply(updateMipmaps: false);
                    return fromModule;
                }
                finally
                {
                    UnityEngine.Object.Destroy(raw);
                }
            }
#endif
            Texture2D captured = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, mipChain: false);
            captured.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), destX: 0, destY: 0, recalculateMipMaps: false);
            captured.Apply(updateMipmaps: false);
            return captured;
        }

        /// <summary>
        /// Where one capture's file lives.
        /// </summary>
        /// <remarks>
        /// The variant sits at the end of the file name rather than in a folder above it, so a
        /// capture point's folder holds every variant of it together. Opening
        /// `Current/tutorial-pinball` then shows every page in every language at once, which is
        /// exactly what the report's page for that folder shows.
        /// </remarks>
        private static string PathFor(string folder, string safeName)
        {
            return Path.Combine(OutputRoot, folder, safeName + "." + SafeVariant + ".png");
        }

        private static void WritePng(string path, Texture2D texture)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static void DeleteFolder(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }

        /// <summary>
        /// Keeps a name usable as a relative file path, letting `/` through so callers may group
        /// their captures into folders.
        /// </summary>
        private static string SanitizeName(string name)
        {
            StringBuilder builder = new StringBuilder(name.Length);
            foreach (char c in name.Replace('\\', '/'))
            {
                bool keep = c == '/' || c == '.' || c == '-' || c == '_' || char.IsLetterOrDigit(c);
                builder.Append(keep ? c : '_');
            }
            return builder.ToString().Trim('/');
        }

        private static string DefaultOutputRoot()
        {
            if (Application.isEditor)
            {
                return Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Screenshots"));
            }
            return Path.Combine(Application.persistentDataPath, "Screenshots");
        }
    }
}

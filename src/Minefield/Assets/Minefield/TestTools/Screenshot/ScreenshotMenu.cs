#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace E7.Minefield
{
    /// <summary>
    /// Menu entries for the parts of the screenshot workflow that happen between runs rather than
    /// during one : looking at the report, and deciding that what a run produced is now the truth.
    /// </summary>
    /// <remarks>
    /// These live in the test assembly next to the feature they operate on, because
    /// <see cref="Screenshot"/> is only compiled when tests are included, and an editor-only
    /// assembly could not reference it.
    /// </remarks>
    internal static class ScreenshotMenu
    {
        private const string Root = "Window/Analysis/Minefield/Screenshots/";

        [MenuItem(Root + "Open Report", priority = 100)]
        private static void OpenReport()
        {
            string report = Path.Combine(Screenshot.OutputRoot, "index.html");
            if (File.Exists(report) == false)
            {
                Debug.LogWarning($"[Minefield] No screenshot report at {report} yet. Run a screenshot test first.");
                return;
            }
            Application.OpenURL("file://" + report);
        }

        [MenuItem(Root + "Reveal Folder", priority = 101)]
        private static void RevealFolder()
        {
            Directory.CreateDirectory(Screenshot.OutputRoot);
            EditorUtility.RevealInFinder(Screenshot.OutputRoot);
        }

        [MenuItem(Root + "Accept Captures As Baseline", priority = 200)]
        private static void AcceptBaseline()
        {
            string current = Path.Combine(Screenshot.OutputRoot, Screenshot.CurrentFolder);
            if (Directory.Exists(current) == false)
            {
                Debug.LogWarning($"[Minefield] Nothing to accept, no captures at {current}.");
                return;
            }

            int count = Directory.GetFiles(current, "*.png", SearchOption.AllDirectories).Length;
            bool confirmed = EditorUtility.DisplayDialog(
                "Accept screenshots as baseline",
                $"Overwrite the baseline with {count} captures from the last run?\n\n" +
                "Future runs will be compared against these.",
                "Accept", "Cancel");

            if (confirmed)
            {
                Screenshot.AcceptCapturesAsBaseline();
                Debug.Log($"[Minefield] Accepted {count} captures as the new baseline.");
            }
        }

        [MenuItem(Root + "Clear Captures", priority = 201)]
        private static void ClearCaptures()
        {
            Screenshot.ClearCaptures();
            Debug.Log("[Minefield] Cleared captures, diffs, and the report. Baselines were left alone.");
        }
    }
}
#endif

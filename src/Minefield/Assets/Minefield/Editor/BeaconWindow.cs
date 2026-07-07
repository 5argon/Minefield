using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace E7.Minefield
{
    /// <summary>
    /// Edit-time overview of Minefield beacons. Lists every scene that contains beacons, and for an
    /// open scene shows each beacon's GameObject, type, and enum label. Selecting a beacon row pings
    /// (and selects) the object, and duplicate labels — which would throw a <c>BeaconException</c> at
    /// test time — are flagged here, where they were authored, instead of at runtime.
    ///
    /// Open via <b>Window ▸ Analysis ▸ Minefield ▸ Beacons</b>.
    /// </summary>
    public class BeaconWindow : EditorWindow
    {
        // Fixed asset GUIDs so the UXML/USS resolve whether the package is embedded under Assets or
        // installed via UPM under Packages.
        const string UxmlGuid = "1fdecb5ed1eac89ecacd37ea3cd6d294";
        const string UssGuid = "626cfcbe6f1c5fb5d48caed5431724f3";

        [MenuItem("Window/Analysis/Minefield/Beacons")]
        public static void Open()
        {
            var w = GetWindow<BeaconWindow>();
            w.titleContent = new GUIContent("Beacons");
            w.minSize = new Vector2(380, 260);
            w.Show();
        }

        class SceneRow
        {
            public string path;
            public string name;
            public bool isOpen;
            public bool inBuild;
            public int liveCount; // -1 when the scene isn't open (unknown without loading it)
        }

        class BeaconRow
        {
            public LabelBeacon beacon;
            public string objectPath;
            public string typeName;
            public string label;
            public bool duplicate;
        }

        readonly List<SceneRow> scenes = new List<SceneRow>();
        readonly List<BeaconRow> beacons = new List<BeaconRow>();
        SceneRow selectedScene;

        ListView scenesList;
        ListView beaconsList;
        Label statusLabel;
        Label dupWarning;
        Label beaconsHeaderLabel;
        Label emptyLabel;
        Button openSceneButton;
        Toggle buildOnlyToggle;

        public void CreateGUI()
        {
            var root = rootVisualElement;

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GUIDToAssetPath(UxmlGuid));
            if (uxml == null)
            {
                root.Add(new Label("Minefield: could not load BeaconWindow.uxml. Try reimporting the package."));
                return;
            }
            uxml.CloneTree(root);

            var uss = AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath(UssGuid));
            if (uss != null) root.styleSheets.Add(uss);

            statusLabel = root.Q<Label>("status");
            dupWarning = root.Q<Label>("dupWarning");
            beaconsHeaderLabel = root.Q<Label>("beaconsHeader");
            emptyLabel = root.Q<Label>("beaconsEmpty");
            openSceneButton = root.Q<Button>("openScene");
            buildOnlyToggle = root.Q<Toggle>("buildOnly");
            scenesList = root.Q<ListView>("scenesList");
            beaconsList = root.Q<ListView>("beaconsList");

            root.Q<Button>("refresh").clicked += RefreshScenes;
            buildOnlyToggle.RegisterValueChangedCallback(_ => RefreshScenes());
            openSceneButton.clicked += OpenSelectedScene;

            SetupScenesList();
            SetupBeaconsList();
            RefreshScenes();
        }

        void OnEnable()
        {
            EditorSceneManager.sceneOpened += OnSceneOpened;
            EditorSceneManager.sceneClosed += OnSceneClosed;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;
        }

        void OnDisable()
        {
            EditorSceneManager.sceneOpened -= OnSceneOpened;
            EditorSceneManager.sceneClosed -= OnSceneClosed;
            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
        }

        void OnSceneOpened(Scene s, OpenSceneMode m) { if (scenesList != null) RefreshScenes(); }
        void OnSceneClosed(Scene s) { if (scenesList != null) RefreshScenes(); }

        void OnHierarchyChanged()
        {
            // Cheap: only re-scan the currently selected open scene as its hierarchy changes.
            if (beaconsList != null && selectedScene != null && selectedScene.isOpen) RefreshBeaconsForSelected();
        }

        void SetupScenesList()
        {
            scenesList.selectionType = SelectionType.Single;
            scenesList.fixedItemHeight = 22;
            scenesList.itemsSource = scenes;
            scenesList.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("mf-row");
                var name = new Label { name = "n" };
                name.AddToClassList("mf-grow2");
                var meta = new Label { name = "m" };
                meta.AddToClassList("mf-meta");
                row.Add(name);
                row.Add(meta);
                return row;
            };
            scenesList.bindItem = (e, i) =>
            {
                var s = scenes[i];
                e.Q<Label>("n").text = (s.isOpen ? "● " : "○ ") + s.name;
                e.Q<Label>("m").text = s.isOpen
                    ? $"{s.liveCount} beacon(s)"
                    : (s.inBuild ? "closed" : "closed · not in build");
            };
            scenesList.selectionChanged += _ =>
            {
                int idx = scenesList.selectedIndex;
                selectedScene = (idx >= 0 && idx < scenes.Count) ? scenes[idx] : null;
                RefreshBeaconsForSelected();
            };
        }

        void SetupBeaconsList()
        {
            beaconsList.selectionType = SelectionType.Single;
            beaconsList.fixedItemHeight = 20;
            beaconsList.itemsSource = beacons;
            beaconsList.makeItem = () =>
            {
                var row = new VisualElement();
                row.AddToClassList("mf-row");
                var path = new Label { name = "p" };
                path.AddToClassList("mf-grow2");
                var type = new Label { name = "t" };
                type.AddToClassList("mf-grow1");
                var label = new Label { name = "l" };
                label.AddToClassList("mf-grow1");
                row.Add(path);
                row.Add(type);
                row.Add(label);
                return row;
            };
            beaconsList.bindItem = (e, i) =>
            {
                var b = beacons[i];
                e.Q<Label>("p").text = b.objectPath;
                e.Q<Label>("t").text = b.typeName;
                e.Q<Label>("l").text = b.label;
                if (b.duplicate) e.AddToClassList("mf-duplicate");
                else e.RemoveFromClassList("mf-duplicate");
            };
            beaconsList.selectionChanged += _ =>
            {
                int idx = beaconsList.selectedIndex;
                if (idx >= 0 && idx < beacons.Count && beacons[idx].beacon != null)
                {
                    var go = beacons[idx].beacon.gameObject;
                    EditorGUIUtility.PingObject(go);
                    Selection.activeGameObject = go;
                }
            };
        }

        void RefreshScenes()
        {
            if (scenesList == null) return;
            scenes.Clear();

            var beaconScriptPaths = BeaconScriptPaths();

            var openByPath = new Dictionary<string, Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var sc = SceneManager.GetSceneAt(i);
                if (sc.isLoaded && !string.IsNullOrEmpty(sc.path)) openByPath[sc.path] = sc;
            }

            var buildPaths = new HashSet<string>(EditorBuildSettings.scenes.Select(s => s.path));
            bool buildOnly = buildOnlyToggle != null && buildOnlyToggle.value;

            foreach (var sguid in AssetDatabase.FindAssets("t:Scene"))
            {
                var path = AssetDatabase.GUIDToAssetPath(sguid);
                if (buildOnly && !buildPaths.Contains(path)) continue;

                bool isOpen = openByPath.TryGetValue(path, out var openScene);
                int liveCount = isOpen ? CountBeacons(openScene) : -1;

                bool hasByDeps = false;
                if (beaconScriptPaths.Count > 0)
                {
                    foreach (var d in AssetDatabase.GetDependencies(path, false))
                    {
                        if (beaconScriptPaths.Contains(d)) { hasByDeps = true; break; }
                    }
                }

                if (!hasByDeps && !(isOpen && liveCount > 0)) continue;

                scenes.Add(new SceneRow
                {
                    path = path,
                    name = System.IO.Path.GetFileNameWithoutExtension(path),
                    isOpen = isOpen,
                    inBuild = buildPaths.Contains(path),
                    liveCount = liveCount,
                });
            }

            scenes.Sort((a, b) => string.Compare(a.name, b.name, System.StringComparison.OrdinalIgnoreCase));

            if (statusLabel != null)
                statusLabel.text = $"{scenes.Count} scene(s) with beacons" + (buildOnly ? " · build settings only" : "");

            selectedScene = null;
            scenesList.ClearSelection();
            scenesList.Rebuild();
            RefreshBeaconsForSelected();
        }

        void RefreshBeaconsForSelected()
        {
            beacons.Clear();

            if (selectedScene == null)
            {
                beaconsHeaderLabel.text = "Beacons";
                dupWarning.style.display = DisplayStyle.None;
                openSceneButton.style.display = DisplayStyle.None;
                beaconsList.style.display = DisplayStyle.None;
                emptyLabel.style.display = DisplayStyle.Flex;
                emptyLabel.text = "Select a scene above to see its beacons.";
                beaconsList.Rebuild();
                return;
            }

            beaconsHeaderLabel.text = $"Beacons in “{selectedScene.name}”";

            if (!selectedScene.isOpen)
            {
                dupWarning.style.display = DisplayStyle.None;
                beaconsList.style.display = DisplayStyle.None;
                openSceneButton.style.display = DisplayStyle.Flex;
                emptyLabel.style.display = DisplayStyle.Flex;
                emptyLabel.text = "This scene isn't open. Open it to inspect and ping its beacons.";
                beaconsList.Rebuild();
                return;
            }

            openSceneButton.style.display = DisplayStyle.None;

            var scene = SceneManager.GetSceneByPath(selectedScene.path);
            var all = new List<LabelBeacon>();
            if (scene.IsValid() && scene.isLoaded)
            {
                foreach (var rootGo in scene.GetRootGameObjects())
                    all.AddRange(rootGo.GetComponentsInChildren<LabelBeacon>(true));
            }

            // Duplicate detection: same enum type + value (mirrors the runtime "one active beacon per label" rule).
            var counts = new Dictionary<string, int>();
            foreach (var b in all)
            {
                var key = LabelKey(b);
                counts.TryGetValue(key, out int n);
                counts[key] = n + 1;
            }

            int dupCount = 0;
            foreach (var b in all)
            {
                bool dup = counts[LabelKey(b)] > 1;
                if (dup) dupCount++;
                beacons.Add(new BeaconRow
                {
                    beacon = b,
                    objectPath = GetPath(b.transform),
                    typeName = b.GetType().Name,
                    label = LabelDisplay(b),
                    duplicate = dup,
                });
            }

            beacons.Sort((a, b) => string.Compare(a.objectPath, b.objectPath, System.StringComparison.OrdinalIgnoreCase));

            bool any = beacons.Count > 0;
            beaconsList.style.display = any ? DisplayStyle.Flex : DisplayStyle.None;
            emptyLabel.style.display = any ? DisplayStyle.None : DisplayStyle.Flex;
            if (!any) emptyLabel.text = "No beacons found in this scene.";

            if (dupCount > 0)
            {
                dupWarning.style.display = DisplayStyle.Flex;
                dupWarning.text = $"⚠ {dupCount} beacon(s) share a duplicate label — this throws BeaconException at test time.";
            }
            else
            {
                dupWarning.style.display = DisplayStyle.None;
            }

            beaconsList.Rebuild();
        }

        static int CountBeacons(Scene scene)
        {
            int c = 0;
            foreach (var root in scene.GetRootGameObjects())
                c += root.GetComponentsInChildren<LabelBeacon>(true).Length;
            return c;
        }

        // All script assets whose class derives from LabelBeacon. Used to detect beacon scenes via the
        // asset dependency graph, without loading each scene.
        static HashSet<string> BeaconScriptPaths()
        {
            var set = new HashSet<string>();
            foreach (var guid in AssetDatabase.FindAssets("t:MonoScript"))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(path);
                var cls = ms != null ? ms.GetClass() : null;
                if (cls != null && !cls.IsAbstract && typeof(LabelBeacon).IsAssignableFrom(cls))
                    set.Add(path);
            }
            return set;
        }

        static string LabelKey(LabelBeacon b)
        {
            var l = b.Label;
            return l == null ? b.GetType().FullName + "::<null>" : l.GetType().FullName + "::" + l;
        }

        static string LabelDisplay(LabelBeacon b)
        {
            var l = b.Label;
            return l == null ? "(no label)" : l.GetType().Name + "." + l;
        }

        static string GetPath(Transform t)
        {
            var sb = new System.Text.StringBuilder(t.name);
            for (var p = t.parent; p != null; p = p.parent) sb.Insert(0, p.name + "/");
            return sb.ToString();
        }

        void OpenSelectedScene()
        {
            if (selectedScene == null || selectedScene.isOpen) return;
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(selectedScene.path, OpenSceneMode.Single);
                RefreshScenes();
            }
        }
    }
}

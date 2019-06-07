using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace E7.Minefield
{
    public static class AssignIconTools
    {
        static MethodInfo m1 = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo m2 = typeof(MonoImporter).GetMethod("CopyMonoScriptIconToImporters", BindingFlags.Static | BindingFlags.NonPublic);
        [MenuItem("Assets/Minefield/Auto-assign all script icons")]
        public static void AssignIcons()
        {
            var beaconClasses = MonoImporter.GetAllRuntimeMonoScripts().Where((x) =>
            {
                //There are null returning from GetClass as well (why?)
                var cls = x.GetClass();
                return cls == null ? false : typeof(ITestBeacon).IsAssignableFrom(cls);
            });

            Texture2D navigationBeaconIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("3d6634608b55541ddac251be41744121"));
            Texture2D testBeaconIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(AssetDatabase.GUIDToAssetPath("d99be7b16603541eebc812d11bb2edf4"));

            foreach (var bc in beaconClasses)
            {
                Debug.Log($"[Minefield] Assigning a new script icon to {bc.name}");
                m1.Invoke(null, new object[] { bc,
                    typeof(INavigationBeacon).IsAssignableFrom(bc.GetClass()) ?
                    navigationBeaconIcon : testBeaconIcon });
                m2.Invoke(null, new object[] { bc });
            }
        }
    }
}
using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace E7.Minefield
{
    public static class AssignIconTool
    {
        static MethodInfo SetIconForObject = typeof(EditorGUIUtility).GetMethod("SetIconForObject", BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo CopyMonoScriptIconToImporters = typeof(MonoImporter).GetMethod("CopyMonoScriptIconToImporters", BindingFlags.Static | BindingFlags.NonPublic);

        static Type T_Annotation = Type.GetType("UnityEditor.Annotation, UnityEditor");
        static FieldInfo AnnotationClassId = T_Annotation.GetField("classID");
        static FieldInfo AnnotationScriptClass = T_Annotation.GetField("scriptClass");
        static FieldInfo AnnotationFlags = T_Annotation.GetField("flags");
        static FieldInfo AnnotationGizmoEnabled = T_Annotation.GetField("gizmoEnabled");
        static FieldInfo AnnotationIconEnabled = T_Annotation.GetField("iconEnabled");

        static Type AnnotationUtility = Type.GetType("UnityEditor.AnnotationUtility, UnityEditor");
        static MethodInfo GetAnnotations = AnnotationUtility.GetMethod("GetAnnotations", BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo SetGizmoEnabled = AnnotationUtility.GetMethod("SetGizmoEnabled", BindingFlags.Static | BindingFlags.NonPublic);
        static MethodInfo SetIconEnabled = AnnotationUtility.GetMethod("SetIconEnabled", BindingFlags.Static | BindingFlags.NonPublic);

        // [MenuItem("Assets/Minefield/Yo")]
        // public static void Yo()
        // {
        //     Array annotations = (Array)m5.Invoke(null, null);
        //     foreach (var a in annotations)
        //     {
        //         int classId = (int)ClassId.GetValue(a);
        //         int flags = (int)Flags.GetValue(a);
        //         int gizmoEnabled = (int)GizmoEnabled.GetValue(a);
        //         int iconEnabled = (int)IconEnabled.GetValue(a);
        //         string scriptClass = (string)ScriptClass.GetValue(a);
        //         Debug.Log($"{classId} {scriptClass} {flags} {iconEnabled} {gizmoEnabled}");
        //     }
        // }

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
                SetIconForObject.Invoke(null, new object[] { bc,
                    typeof(INavigationBeacon).IsAssignableFrom(bc.GetClass()) ?
                    navigationBeaconIcon : testBeaconIcon });
                CopyMonoScriptIconToImporters.Invoke(null, new object[] { bc });
            }

            //Disable the icon in gizmos annotation.
            Array annotations = (Array)GetAnnotations.Invoke(null, null);

            foreach (var bc in beaconClasses)
            {
                foreach (var a in annotations)
                {
                    string scriptClass = (string)AnnotationScriptClass.GetValue(a);
                    if (scriptClass == bc.name)
                    {
                        int classId = (int)AnnotationClassId.GetValue(a);
                        SetIconEnabled.Invoke(null, new object[] { classId, scriptClass, 0 });
                    }
                }
            }
        }
    }
}
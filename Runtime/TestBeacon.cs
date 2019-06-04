//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using UnityEngine;
using System;

namespace E7.Minefield
{
    public abstract class TestBeacon : MonoBehaviour
    {
        public abstract Enum Label { get; }
        public abstract RectTransform Rect { get; }

        public static void Click<T>(T action) where T : Enum
        {
            var beacons = UnityEngine.Object.FindObjectsOfType<TestBeacon>();
            foreach (var b in beacons)
            {
                if (b.Label is T t && t.Equals(action))
                {
                    Debug.Log($"Type matches {b.Label.GetType()} {t}");
                    Utility.RaycastClick(b.Rect);
                    return;
                }
            }
            throw new Exception($"Action {action} not found on any beacon in the scene.");
        }
    }
}
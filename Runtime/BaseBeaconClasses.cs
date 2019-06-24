using UnityEngine;
using System;

namespace E7.Minefield
{
    /// <summary>
    /// Non interface base class allows search box compatibility.
    /// </summary>
    public abstract class TestBeacon : MonoBehaviour, ITestBeacon
    {
        public abstract Enum Label { get; }
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Shortcut to write more concise code in combination with <see cref="Beacon.Get">
        /// </summary>
        public T Component<T>() => GameObject.GetComponent<T>();
    }

    /// <summary>
    /// Non-interface base class allows search box compatibility.
    /// </summary>
    public abstract class NavigationBeacon : TestBeacon, ITestBeacon, INavigationBeacon
    {
        public Vector2 ScreenClickPoint
        {
            get
            {
                var rectTransform = GetComponent<RectTransform>();
                var collider2d = GetComponent<Collider2D>();
                var collider = GetComponent<Collider>();
                if (rectTransform != null)
                {
                    return Utility.ScreenCenterOfRectTransform(rectTransform);
                }
                if (collider2d != null)
                {
                    return Camera.main.WorldToScreenPoint(collider2d.bounds.center);
                }
                if (collider != null)
                {
                    return Camera.main.WorldToScreenPoint(collider.bounds.center);
                }
                throw new Exception($"Navigation beacon {Label} is on an object {name} without {nameof(RectTransform)}, {nameof(Collider)}, or {nameof(Collider2D)}. We need something to click on.");
            }
        }
    }

    public abstract class TestBeacon<T> : TestBeacon
    where T : Enum
    {
        public T label;
        public override Enum Label => label;
    }

    public abstract class NavigationBeacon<T> : NavigationBeacon
    where T : Enum
    {
        public T label;
        public override Enum Label => label;
    }
}
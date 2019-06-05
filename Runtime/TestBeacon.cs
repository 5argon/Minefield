using UnityEngine;
using System;

namespace E7.Minefield
{
    public interface ITestBeacon
    {
        Enum Label { get; }
        GameObject GameObject { get; }
    }

    public interface INavigationBeacon : ITestBeacon
    {
        RectTransform RectTransform { get; }
    }

    /// <summary>
    /// Non interface base class allows search box compatibility.
    /// </summary>
    public abstract class TestBeacon : MonoBehaviour, ITestBeacon
    {
        public abstract Enum Label { get; }
        public GameObject GameObject => gameObject;
    }

    /// <summary>
    /// Non interface base class allows search box compatibility.
    /// </summary>
    public abstract class NavigationBeacon : TestBeacon, ITestBeacon, INavigationBeacon
    {
        public RectTransform RectTransform
        {
            get
            {
                var r = GetComponent<RectTransform>();
                if (r == null)
                {
                    throw new Exception($"Navigation beacon {Label} is on an object {name} without {nameof(RectTransform)}.");
                }
                return r;
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
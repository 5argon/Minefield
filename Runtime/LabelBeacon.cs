using UnityEngine;
using System;

namespace E7.Minefield
{
    /// <summary>
    /// The simplest beacon that you could look up things from the scene by label.
    /// </summary>
    public interface ILabelBeacon
    {
        Enum Label { get; }
        GameObject GameObject { get; }
    }

    /// <summary>
    /// The simplest beacon that you could look up things from the scene by label.
    /// </summary>
    public abstract class LabelBeacon<T> : LabelBeacon
    where T : Enum
    {
        public T label;
        public override Enum Label => label;
    }

    [DisallowMultipleComponent]
    /// <summary>
    /// The simplest beacon that you could look up things from the scene by label.
    /// 
    /// This non-generic version is not meant to be subclassed, it is just to allow compatibility with Unity search box
    /// which couldn't handle generic class.
    /// </summary>
    public abstract class LabelBeacon : MonoBehaviour, ILabelBeacon
    {
        public abstract Enum Label { get; }
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Shortcut to write more concise code in combination with <see cref="Beacon.Get">
        /// </summary>
        public T Component<T>() => GameObject.GetComponent<T>();
    }
}
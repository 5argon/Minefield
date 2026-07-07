using UnityEngine;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// The simplest beacon that you could look up things from the scene by label.
    /// 
    /// This non-generic version is not meant to be subclassed, it is just to allow compatibility with Unity search box
    /// which couldn't handle generic class.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class LabelBeacon : MonoBehaviour, ILabelBeacon
    {
        public abstract Enum Label { get; }
        public GameObject GameObject => gameObject;

        /// <summary>
        /// Shortcut to write more concise code in combination with <see cref="Beacon.Get" />
        /// </summary>
        public T Component<T>() => GameObject.GetComponent<T>();

        // --- Active beacon registry ---------------------------------------------------------
        // Beacons register themselves here while enabled, so lookups (Beacon.FindActive, every
        // WaitUntil / ClickWhen / constraint check) are O(1) and allocation-free instead of scanning
        // the whole scene each call. "Registered" == "active", because OnEnable/OnDisable fire when a
        // GameObject (or any parent) is (de)activated, preserving the "beacon must be active to be
        // found" contract. Keyed by the boxed Enum, whose equality includes the concrete enum type,
        // so labels from different enum types never collide.

        private static readonly Dictionary<Enum, List<LabelBeacon>> registry = new Dictionary<Enum, List<LabelBeacon>>();

        /// <summary>
        /// Override in a subclass ONLY if you also call <c>base.OnEnable()</c>, or the beacon will not
        /// register and become unfindable in tests.
        /// </summary>
        protected virtual void OnEnable()
        {
            Enum label = Label;
            if (!registry.TryGetValue(label, out List<LabelBeacon> list))
            {
                list = new List<LabelBeacon>(1);
                registry.Add(label, list);
            }
            if (!list.Contains(this))
            {
                list.Add(this);
            }
        }

        /// <summary>
        /// Override in a subclass ONLY if you also call <c>base.OnDisable()</c>.
        /// </summary>
        protected virtual void OnDisable()
        {
            if (registry.TryGetValue(Label, out List<LabelBeacon> list))
            {
                list.Remove(this);
                if (list.Count == 0)
                {
                    registry.Remove(Label);
                }
            }
        }

        /// <summary>
        /// Number of currently-active beacons carrying <paramref name="label"/>, and the first one via
        /// <paramref name="first"/>. Destroyed-but-unremoved entries are pruned defensively.
        /// </summary>
        public static int CountActive(Enum label, out LabelBeacon first)
        {
            first = null;
            if (!registry.TryGetValue(label, out List<LabelBeacon> list))
            {
                return 0;
            }
            int count = 0;
            for (int i = list.Count - 1; i >= 0; i--)
            {
                LabelBeacon b = list[i];
                if (b == null) // Unity "fake null": object was destroyed without OnDisable running.
                {
                    list.RemoveAt(i);
                    continue;
                }
                count++;
                first = b;
            }
            return count;
        }
    }
}
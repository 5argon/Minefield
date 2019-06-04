using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace E7.Minefield
{
    public abstract class TestBeacon<T> : TestBeacon
    where T : Enum
    {
        [FormerlySerializedAs("action")]
        public T label;
        public override Enum Label => label;

        public override RectTransform Rect
        {
            get
            {
                var r = GetComponent<RectTransform>();
                if (r == null)
                {
                    throw new Exception($"Beacon {label} is on an object {name} without {nameof(RectTransform)}.");
                }
                return r;
            }
        }
    }
}
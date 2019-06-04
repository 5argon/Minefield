//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using UnityEngine;
using System;

namespace E7.Minefield
{
    public abstract class TestBeacon : MonoBehaviour
    {
        public abstract Enum Label { get; }
        public abstract RectTransform Rect { get; }
    }
}
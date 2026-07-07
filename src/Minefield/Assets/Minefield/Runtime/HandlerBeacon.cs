using UnityEngine;
using System;

namespace E7.Minefield
{
    /// <summary>
    /// A beacon which could handle events. Implying that it must have some area on the screen to be clicked on.
    /// </summary>
    public interface IHandlerBeacon : ILabelBeacon
    {
        /// <summary>
        /// When simulating a click on this beacon, it will click on this point.
        /// </summary>
        Vector2 ScreenClickPoint { get; }
    }

    /// <summary>
    /// A beacon which could handle events. Implying that it must have some area on the screen to be clicked on.
    /// </summary>
    public abstract class HandlerBeacon<T> : HandlerBeacon
    where T : Enum
    {
        public T label;
        public override Enum Label => label;
    }

    /// <summary>
    /// A beacon which could handle events. Implying that it must have some area on the screen to be clicked on.
    /// 
    /// This non-generic version is not meant to be subclassed, it is just to allow compatibility with Unity search box
    /// which couldn't handle generic class.
    /// </summary>
    public abstract class HandlerBeacon : LabelBeacon, IHandlerBeacon
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
}
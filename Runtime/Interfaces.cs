using UnityEngine;
using UnityEngine.UI;
using System;

namespace E7.Minefield
{
    /// <summary>
    /// Use a new wording "on" or "off" to provide your own interpretation of the current status of a game object.
    /// It could be used with <see cref="Is.On"> or <see cref="Is.Off"> constraint in the test.
    /// 
    /// For example an image made invisible by setting <see cref="Behaviour.enabled"> 
    /// would not be detected by <see cref="Is.Active">.
    /// 
    /// A text component may be active and enabled,
    /// but if they have no text, you may consider it invisible.
    /// 
    /// The button is active, visible, but greyed out, you may want to consider it inactive.
    /// 
    /// Lastly, an image that is active, enabled, and its alpha is not even 0, but is invisible due to its <see cref="CanvasGroup">'s alpha.
    /// This case is very hard to detect automatically by testint lib, so it is better to just provide your own logic.
    /// </summary>
    public interface IMinefieldOnOffStatus
    {
        bool IsOn { get; }
    }

    public interface ITestBeacon
    {
        Enum Label { get; }
        GameObject GameObject { get; }
    }

    public interface INavigationBeacon : ITestBeacon
    {
        RectTransform RectTransform { get; }
    }
}
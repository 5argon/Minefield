using UnityEngine;
using UnityEngine.UI;
using System;

namespace E7.Minefield
{
    public interface IMinefieldReporter { }

    public interface ITestBeacon
    {
        Enum Label { get; }
        GameObject GameObject { get; }
    }

    public interface INavigationBeacon : ITestBeacon
    {
        /// <summary>
        /// When simulating a click on this beacon, it will click on this point.
        /// </summary>
        Vector2 ScreenClickPoint { get; }
    }
}
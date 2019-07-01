using System;

namespace E7.Minefield
{
    /// <summary>
    /// A special kind of <see cref="HandlerBeacon{T}"> for navigating the scene. (not for gameplay actions)
    /// Functionally it is the same, but it may provide some extra functions in the future so it is better
    /// to categorize you gameplay related handler beacons and UI-related navigation beacon correctly.
    /// </summary>
    public abstract class NavigationBeacon<T> : NavigationBeacon where T : Enum 
    { 
        public T label;
        public override Enum Label => label;
    }

    /// <summary>
    /// A special kind of <see cref="HandlerBeacon{T}"> for navigating the scene. (not for gameplay actions)
    /// Functionally it is the same, but it may provide some extra functions in the future so it is better
    /// to categorize you gameplay related handler beacons and UI-related navigation beacon correctly.
    /// 
    /// This non-generic version is not meant to be subclassed, it is just to allow compatibility with Unity search box
    /// which couldn't handle generic class.
    /// </summary>
    public abstract class NavigationBeacon : HandlerBeacon { }
}
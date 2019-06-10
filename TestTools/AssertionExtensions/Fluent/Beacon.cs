using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

namespace E7.Minefield
{
    public static class Beacon
    {
        /// <summary>
        /// Remember that base condition for all constraints is that the beacon must be found, and to be found the game object must be active.
        /// 
        /// It will not fail the test if the constraint doesn't happen yet, please check manually if you really have that type of
        /// beacon in the scene or it would wait forever when you actually forgot to add the beacon. (Unlike assertion where you immediately get
        /// an error message telling you about missing beacon.)
        /// </summary>
        public static IEnumerator WaitUntil<T>(T beacon, BeaconConstraint bc)
            where T : Enum
        {
            yield return new WaitUntil(() => bc.ApplyToBeacon(beacon).IsSuccess);
        }

        /// <summary>
        /// Like <see cref="WaitUntil{T}(T, BeaconConstraint)"> but additionally include <see cref="Click{T}(T)"> in one yield.
        /// </summary>
        public static IEnumerator ClickWhen<T>(T beacon, BeaconConstraint bc)
            where T : Enum
        {
            yield return new WaitUntil(() => bc.ApplyToBeacon(beacon).IsSuccess);
            yield return Click<T>(beacon);
        }

        /// <summary>
        /// Same as <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ITestBeacon)"> but use return value instead of `out` and error when no active beacon found.
        /// 
        /// Also the returned class is not the interface <see cref="ITestBeacon"> but <see cref="TestBeacon">, which provides some generic methods benefit
        /// unavailable on interfaces.
        /// </summary>
        public static TestBeacon Get<BEACONTYPE>(BEACONTYPE label) where BEACONTYPE : Enum
        {
            if (FindActive(label, out ITestBeacon found))
            {
                return (TestBeacon)found;
            }
            else
            {
                throw new Exception($"Label {label} not found on any navigation beacon in the scene.");
            }
        }

        /// <summary>
        /// Same as <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ITestBeacon)"> but use return value instead of `out` and error when no active beacon found.
        /// 
        /// Also the returned class is not the interface <see cref="ITestBeacon"> but <see cref="TestBeacon">, which provides some generic methods benefit
        /// unavailable on interfaces.
        /// </summary>
        public static COMPONENTTYPE GetComponent<COMPONENTTYPE>(Enum label) where COMPONENTTYPE : Component
        {
            if (FindActive(label, out ITestBeacon found))
            {
                return ((TestBeacon)found).GetComponent<COMPONENTTYPE>();
            }
            else
            {
                throw new Exception($"Label {label} not found on any navigation beacon in the scene.");
            }
        }

        /// <summary>
        /// Find an **active** beacon in the scene.
        /// </summary>
        /// <returns>`false` when not found.</returns>
        /// <exception cref="Exception">Thrown when found multiple beacons with the same <paramref name="label">.</exception>
        public static bool FindActive<BEACONTYPE>(BEACONTYPE label, out ITestBeacon foundBeacon) where BEACONTYPE : Enum
            => FindActiveInternal(label, out foundBeacon);

        private static bool FindActiveInternal<BEACONTYPE>(BEACONTYPE label, out ITestBeacon foundBeacon)
        {
            var beacons = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ITestBeacon>();
            //Debug.Log($"Find active {beacons.Count()}");
            ITestBeacon testBeacon = null;
            bool found = false;
            foreach (var b in beacons)
            {
                //Debug.Log($"Checking {b.Label} {b.GetType()} vs {label}");
                if (b.Label is BEACONTYPE t && t.Equals(label))
                {
                    if (found)
                    {
                        throw new BeaconException($"Multiple beacons with label {label} found. This is considered an error.");
                    }
                    testBeacon = b;
                    found = true;
                }
            }
            if (!found)
            {
                foundBeacon = null;
                return false;
            }
            else
            {
                foundBeacon = testBeacon;
                return true;
            }
        }

        /// <summary>
        /// Raycast click on a beacon.
        /// The beacon label must be on <see cref="NavigationBeacon{T}"> in the scene.
        /// </summary>
        public static IEnumerator Click<T>(T label) where T : Enum
        {
            if (FindActive(label, out ITestBeacon b) && b is INavigationBeacon nb)
            {
                //Debug.Log($"Type matches {nb.Label.GetType()} {label}");
                yield return Utility.RaycastClick(nb.RectTransform);
            }
            else
            {
                throw new Exception($"Label {label} not found on any navigation beacon in the scene.");
            }
        }
    }
}
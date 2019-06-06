//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Linq;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public static partial class Beacon
    {
        /// Find an **active** beacon in the scene.
        /// </summary>
        /// <returns>`false` when not found.</returns>
        /// <exception cref="Exception">Thrown when found multiple beacons with the same <paramref name="label">.</exception>
        public static bool FindActive<BEACONTYPE>(BEACONTYPE label, out ITestBeacon foundBeacon) where BEACONTYPE : Enum
            => FindActiveInternal(label, out foundBeacon);

        internal static bool FindActiveForAssert<BEACONTYPE>(BEACONTYPE label, out ITestBeacon foundBeacon)
        {
            if (label is Enum e)
            {
                return FindActiveInternal(e, out foundBeacon);
            }
            else
            {
                throw new BeaconException($"{label} is not an enum.");
            }
        }

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
        /// Keep checking for a beacon on uGUI component which will eventually became active **and** clickable to the player.
        /// 
        /// "Clickable" is :
        /// - Has `RectTransform`
        /// - A raycast from the center of that `RectTransform` could hit it. (This could be prevented with <see cref="Graphic.raycastTarget"> `false` or <see cref="CanvasGroup.blocksRaycasts"> `false`.)
        /// - Something must be able to happen on click, that is it must be <see cref="IPointerDownHandler">, <see cref="IPointerUpHandler">, or <see cref="IPointerClickHandler">.
        /// - **If** it is <see cref="Selectable">, it must **also** be <see cref="Selectable.IsInteractable()">. (This could be prevented with <see cref="CanvasGroup.interactable"> `false` or <see cref="Selectable.interactable">)
        /// </summary>
        /// <exception cref="BeaconException">Thrown when the beacon found does not even contain <see cref="Selectable"> component.
        public static BeaconWait<T> WaitUntilClickable<T>(T beaconLabel)
        where T : Enum
        {
            var bw = new BeaconWait<T>(beaconLabel)
            {
                ClickableCheck = true
            };

            return bw;
        }

        /// <summary>
        /// Keep checking for a beacon on uGUI component which will eventually became active **and** clickable to the player, then click it.
        /// 
        /// "Clickable" is :
        /// - Has `RectTransform`
        /// - A raycast from the center of that `RectTransform` could hit it. (This could be prevented with <see cref="Graphic.raycastTarget"> `false` or <see cref="CanvasGroup.blocksRaycasts"> `false`.)
        /// - Something must be able to happen on click, that is it must be <see cref="IPointerDownHandler">, <see cref="IPointerUpHandler">, or <see cref="IPointerClickHandler">.
        /// - **If** it is <see cref="Selectable">, it must **also** be <see cref="Selectable.IsInteractable()">. (This could be prevented with <see cref="CanvasGroup.interactable"> `false` or <see cref="Selectable.interactable">)
        /// </summary>
        /// <exception cref="BeaconException">Thrown when the beacon found does not even contain <see cref="Selectable"> component.
        public static IEnumerator ClickWhenClickable<T>(T beaconLabel)
        where T : Enum
        {
            yield return WaitUntilClickable<T>(beaconLabel);
            yield return Click<T>(beaconLabel);
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
//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

namespace E7.Minefield
{
    public static partial class Beacon
    {
        /// <summary>
        /// Raycast click on a beacon.
        /// Currently it only supports that the beacon object must have <see cref="RectTransform"> and it will click on its center.
        /// </summary>
        public static IEnumerator Click<T>(T label) where T : Enum
        {
            if (FindActive(label, out ITestBeacon b) && b is INavigationBeacon nb)
            {
                Debug.Log($"Type matches {nb.Label.GetType()} {label}");
                yield return Utility.RaycastClick(nb.RectTransform);
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
        public static bool FindActive<T>(T label, out ITestBeacon foundBeacon) where T : Enum
        {
            var beacons = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>().OfType<ITestBeacon>();
            ITestBeacon tb = null;
            bool found = false;
            foreach (var b in beacons)
            {
                if (b.Label is T t && t.Equals(label))
                {
                    if (found)
                    {
                        throw new BeaconException($"Multiple beacons with label {label} found. This is considered an error.");
                    }
                    tb = b;
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
                foundBeacon = tb;
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
        /// To be used with <see cref="NUnit.Framework.Constraints"> model.
        /// e.g. `Assert.That(____, Beacon.Is.____, "optional message");`
        /// </summary>
        public static class Is
        {
        }
    }
}
//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using UnityEngine;
using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;

namespace E7.Minefield
{
    public static class Beacon
    {
        public static IEnumerator Click<T>(T label) where T : Enum
        {
            var beacons = UnityEngine.Object.FindObjectsOfType<TestBeacon>();
            if(FindActive(label, out var b))
            {
                Debug.Log($"Type matches {b.Label.GetType()} {label}");
                yield return Utility.RaycastClick(b.Rect);
            }
            else
            {
                throw new Exception($"Label {label} not found on any beacon in the scene.");
            }
        }

        /// <summary>
        /// Find an **active** beacon in the scene.
        /// </summary>
        /// <returns>`false` when not found.</returns>
        /// <exception cref="Exception">Thrown when found multiple beacons with the same <paramref name="label">.</exception>
        public static bool FindActive<T>(T label, out TestBeacon foundBeacon) where T : Enum
        {
            var beacons = UnityEngine.Object.FindObjectsOfType<TestBeacon>();
            TestBeacon tb = null;
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

        public class BeaconWait<T> : CustomYieldInstruction
        where T : Enum
        {
            public override bool keepWaiting
            {
                get
                {
                    if (FindActive(Label, out var found) && PassedAdditionalCriterias(found))
                    {
                        targetBeacon = found;
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            private bool PassedAdditionalCriterias(TestBeacon found)
            {
                if (RectTransform)
                {
                    if (found.GetComponent<RectTransform>() == null)
                    {
                        throw new BeaconException($"Found beacon {found.Label} but not finding `RectTransform` component on it.");
                    }
                }
                if (ClickableCheck)
                {
                    //Criteria : 1. Raycast could hit it. 2. able to handle down or click, 3. if has selectable, it must be interactable.
                    bool handleDown = ExecuteEvents.CanHandleEvent<IPointerDownHandler>(found.gameObject);
                    bool handleClick = ExecuteEvents.CanHandleEvent<IPointerClickHandler>(found.gameObject);

                    var rt = found.GetComponent<RectTransform>();
                    if (rt == null)
                    {
                        throw new BeaconException($"Found beacon {found.Label} but not finding `RectTransform` component on it.");
                    }
                    GameObject firstHit = Utility.RaycastFirst(rt);
                    var hittable = ReferenceEquals(firstHit, found.gameObject);

                    var selectable = found.GetComponent<Selectable>();

                    //IsInteractable could be affected by parent CanvasGroup, however not all clickable things are Selectable.
                    bool interactable = (selectable == null || selectable.IsInteractable());
                    Debug.Log($"{found.name} - {hittable} {handleDown} {handleClick} {selectable} {selectable?.IsInteractable()}");
                    if (!hittable || !interactable || (!handleDown && !handleClick))
                    {
                        return false;
                    }
                }
                return true;
            }

            private T Label { get; }

            // Criterias
            internal bool RectTransform { private get; set; }
            internal bool ClickableCheck { private get; set; }

            public TestBeacon targetBeacon;
            public BeaconWait(T label)
            {
                this.Label = label;
            }
        }

        /// <summary>
        /// Keep looking for a beacon which will eventually became active and interactable to the player.
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

        public static BeaconWait<T> WaitUntilClickable<T>(T beaconLabel, out TestBeacon beacon)
        where T : Enum
        {
            var bw = new BeaconWait<T>(beaconLabel);
            //Target beacon will be resolved later, null for now but the reference is linked.
            beacon = bw.targetBeacon;
            return bw;
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
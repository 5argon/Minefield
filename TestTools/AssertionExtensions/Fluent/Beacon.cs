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
        // public sealed class ThrowableWaitUntil : IEnumerator
        // {
        //     Func<bool> m_Predicate;
        //     public ThrowableWaitUntil(Func<bool> predicate) { m_Predicate = predicate; }

        //     public object Current => null;

        //     public bool MoveNext()
        //     {
        //         try
        //         {
        //         }
        //     }

        //     public void Reset() {} 
        // }

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
            // Debug.Log($"Wait until {Time.frameCount} {bc}");
            yield return new WaitUntil(() => bc.ApplyToBeacon(beacon).IsSuccess);
            //Debug.Log($"Wait until {Time.frameCount} {bc} {bc.ApplyToBeacon(beacon).IsSuccess}");
            // while (true)
            // {
            //     // try
            //     // {
            //         if (bc.ApplyToBeacon(beacon).IsSuccess)
            //         {
            //             break;
            //         }
            //     // }
            //     // catch
            //     // {
            //         //Debug.Log($"Caught sometihing");
            //     //     throw;
            //     // }
            //     yield return null;
            // }
            // Debug.Log($"Wait ok {Time.frameCount}");
        }

        /// <summary>
        /// Until the constraint returns true, repeat the action.
        /// 
        /// The action could spans multiple frames because it works like a coroutine function.
        /// The constraint check occur again when the <paramref name="spamAction"> has completed all of its frames.
        /// 
        /// This is useful to create a "dumb AI" where normally complex actions are required to get through the scene, 
        /// but a simple spam without considering any timing could also do so in a less ideal way.
        /// 
        /// For example, testing a Mario stage with an objective if you could respawn after death or not, 
        /// could be simplified to holding right and jump repeatedly.
        /// You are bound to die sooner or later that way. Then you could use this test regardless of stages.
        /// </summary>
        public static IEnumerator SpamUntil<T>(T beacon, BeaconConstraint bc, Func<IEnumerator> spamAction)
            where T : Enum
        {
            while (Beacon.Check(beacon, bc) == false)
            {
                if (spamAction != null)
                {
                    yield return spamAction();
                }
                else
                {
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Just check if the constraint passed or not, without affecting failing/passing of the test.
        /// </summary>
        public static bool Check<T>(T beacon, BeaconConstraint bc)
            where T : Enum
            => bc.ApplyToBeacon(beacon).IsSuccess;

        /// <summary>
        /// Like <see cref="WaitUntil{T}(T, BeaconConstraint)"> but additionally include <see cref="Click{T}(T)"> in one yield.
        /// 
        /// Some note about the "click", it is a frame of clicking down, wait a frame, then a frame of up.
        /// 
        /// The gotcha of this "up" is that if you have an another click following, the next "down"
        /// will occur on the same frame as previous up. In some situation like you do clicking on the same button
        /// but expect the first click to disable the button and wait for the later one, you may ended up double clicking the button in the same frame if that
        /// disable didn't occur immediately.
        /// 
        /// (For example the disable is planned to be an effect from `TimelineAsset` that plays as a result of a button press,
        /// which need one more frame to take effect.)
        /// 
        /// This is **your bug** however, as it is possible in real play by having a player use the 2nd finger to touch
        /// on the same frame that the first finger lifted off, and that means Minefield is doing a good job catching this bug. You better ensure the disable
        /// comes immediately so <see cref="ClickWhen{T}(T, BeaconConstraint)"> is usable consecutively without `yield return null` in between to "cheat" it.
        /// (For example on `TimelineAsset` case, instead of just `Play()` it as a result of button press, also `Evaluate()` it so the disable take effect
        /// without waiting one more frame.
        /// </summary>
        public static IEnumerator ClickWhen<T>(T beacon, BeaconConstraint bc)
            where T : Enum
        {
            yield return new WaitUntil(() => bc.ApplyToBeacon(beacon).IsSuccess);
            yield return Click<T>(beacon);
        }

        /// <summary>
        /// Same as <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ITestBeacon)"> but use return value instead of `out`, and throw immediately when no active beacon found.
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
        /// Same as <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ITestBeacon)"> but use return value instead of `out` and error when no active beacon found, 
        /// then immediately get component of the game object with that beacon in one command.
        /// 
        /// Useful for asserting any component, for example : `Assert.That(Beacon.GetComponent<TMP_Text>(beacon).text, Does.Contain("Hello"))`
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
        /// Simulate a click on a beacon.
        /// The beacon label must be on <see cref="NavigationBeacon{T}"> in the scene.
        /// 
        /// Definition of a click is pointer down this frame then up at the same coordinate the next frame. So you need a coroutine on this.
        /// 
        /// The gotcha of this "up" is that if you have an another click following, the next "down"
        /// will occur on the same frame as previous up. In some situation like you do clicking on the same button
        /// but expect the first click to disable the button and wait for the later one, you may ended up double clicking the button in the same frame if that
        /// disable didn't occur immediately.
        /// 
        /// (For example the disable is planned to be an effect from `TimelineAsset` that plays as a result of a button press,
        /// which need one more frame to take effect.)
        /// 
        /// This is **your bug** however, as it is possible in real play by having a player use the 2nd finger to touch
        /// on the same frame that the first finger lifted off, and that means Minefield is doing a good job catching this bug. You better ensure the disable
        /// comes immediately so <see cref="ClickWhen{T}(T, BeaconConstraint)"> is usable consecutively without `yield return null` in between to "cheat" it.
        /// (For example on `TimelineAsset` case, instead of just `Play()` it as a result of button press, also `Evaluate()` it so the disable take effect
        /// without waiting one more frame.
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
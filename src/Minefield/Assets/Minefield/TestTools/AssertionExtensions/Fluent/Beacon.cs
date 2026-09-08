using UnityEngine;
using System;
using System.Collections;

namespace E7.Minefield
{
    public static class Beacon
    {
        /// <summary>
        /// Default timeout (seconds, unscaled) applied to <see cref="WaitUntil{T}(T, BeaconConstraint, float)"/>
        /// and <see cref="ClickWhen{T}(T, BeaconConstraint, float)"/> when no per-call timeout is given.
        ///
        /// This stops a mistyped or never-satisfiable wait from hanging silently until the test runner's
        /// global timeout and then failing with an unhelpful message. Instead you get a diagnostic that
        /// explains *why* the constraint never passed. 30s is generous for UI navigation; raise it, lower
        /// it, or set it to <see cref="float.PositiveInfinity"/> to restore the old wait-forever behaviour.
        /// A per-call value always wins over this. The "play button" pattern uses <see cref="Utility.WaitForever"/>,
        /// which is unaffected.
        /// </summary>
        public static float DefaultTimeout = 30f;

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
        /// Remember that base condition for all constraints is that the beacon must be found,
        /// and to be found the game object must be active.
        /// 
        /// It will not fail the test if the constraint doesn't happen yet,
        /// please check manually if you really have that type of
        /// beacon in the scene or it would wait forever when you actually forgot to add the beacon.
        /// (Unlike assertion where you immediately get
        /// an error message telling you about missing beacon.)
        /// </summary>
        /// <param name="timeout">
        /// Seconds (unscaled) to wait before failing with a diagnostic. Negative uses
        /// <see cref="DefaultTimeout"/>; pass <see cref="float.PositiveInfinity"/> to wait forever.
        /// </param>
        public static async Awaitable WaitUntil<T>(T beacon, BeaconConstraint bc, float timeout = -1f)
            where T : Enum
        {
            float limit = timeout < 0f ? DefaultTimeout : timeout;
            float elapsed = 0f;
            while (!bc.ApplyToBeacon(beacon).IsSuccess)
            {
                if (!float.IsInfinity(limit) && elapsed >= limit)
                {
                    throw new BeaconException(
                        $"[Minefield] Beacon '{beacon}' ({typeof(T).Name}) did not satisfy {bc.GetType().Name} within {limit:0.##}s.\n  {bc.Diagnostic()}");
                }
                await Awaitable.NextFrameAsync();
                elapsed += Time.unscaledDeltaTime;
            }
        }

        /// <summary>
        /// Until the constraint returns `true`, repeat the action.
        /// 
        /// The action could spans multiple frames because it works like a coroutine function.
        /// The constraint check occur again when the <paramref name="spamAction"/> has completed all of its frames.
        /// 
        /// This is useful to create a "dumb AI" where normally complex actions are required to get through the scene, 
        /// but a simple spam without considering any timing could also do so in a less ideal way.
        /// 
        /// For example, testing a Mario stage with an objective if you could respawn after death or not, 
        /// could be simplified to holding right and jump repeatedly.
        /// You are bound to die sooner or later that way. Then you could use this test regardless of stages.
        /// </summary>
        public static Awaitable SpamUntil<T>(T beacon, BeaconConstraint bc, Func<Awaitable> spamAction)
            where T : Enum
            => SpamInternal(beacon, bc, spamAction, lookFor: false);

        /// <summary>
        /// Until the constraint returns `false`, repeat the action.
        /// 
        /// The action could spans multiple frames because it works like a coroutine function.
        /// The constraint check occur again when the <paramref name="spamAction"/> has completed all of its frames.
        /// 
        /// This is useful to create a "dumb AI" where normally complex actions are required to get through the scene, 
        /// but a simple spam without considering any timing could also do so in a less ideal way.
        /// 
        /// For example, testing a Mario stage with an objective if you could respawn after death or not, 
        /// could be simplified to holding right and jump repeatedly.
        /// You are bound to die sooner or later that way. Then you could use this test regardless of stages.
        /// </summary>
        public static Awaitable SpamWhile<T>(T beacon, BeaconConstraint bc, Func<Awaitable> spamAction)
            where T : Enum
            => SpamInternal(beacon, bc, spamAction, lookFor: true);

        private static async Awaitable SpamInternal<T>(T beacon, BeaconConstraint bc, Func<Awaitable> spamAction, bool lookFor) where T : Enum
        {
            while (Beacon.Check(beacon, bc) == lookFor)
            {
                int frameBefore = Time.frameCount;
                if (spamAction != null)
                {
                    await spamAction();
                }

                //An action that finishes without spending a frame — one whose every step turned out
                //to be a no-op, say — would otherwise spin here with the game never running, so the
                //constraint it is waiting for could never come true. A timeout can report that; a
                //frozen editor cannot.
                if (Time.frameCount == frameBefore)
                {
                    await Awaitable.NextFrameAsync();
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
        /// Like <see cref="WaitUntil{T}(T, BeaconConstraint)"/>
        /// but additionally include <see cref="Click{T}"/> in one yield.
        /// 
        /// </summary>
        public static async Awaitable ClickWhen<T>(T beacon, BeaconConstraint bc, float timeout = -1f)
            where T : Enum
        {
            await WaitUntil(beacon, bc, timeout);
            await Click<T>(beacon);
        }

        /// <summary>
        /// Same as <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ILabelBeacon)"/>
        /// but use return value instead of `out`, and throw immediately when no active beacon found.
        /// 
        /// Also the returned class is not the interface <see cref="ILabelBeacon"/> but <see cref="LabelBeacon"/>,
        /// which provides some generic methods benefit unavailable on interfaces.
        /// </summary>
        public static LabelBeacon Get<BEACONTYPE>(BEACONTYPE label) where BEACONTYPE : Enum
        {
            if (FindActive(label, out ILabelBeacon found))
            {
                return (LabelBeacon)found;
            }
            else
            {
                throw new Exception($"Label {label} not found on any navigation beacon in the scene.");
            }
        }

        /// <summary>
        /// Same as <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ILabelBeacon)"/> but use return value
        /// instead of `out` and error when no active beacon found, 
        /// then immediately get component of the game object with that beacon in one command.
        /// </summary>
        /// <remarks>
        /// Useful for asserting any component,
        /// for example : `Assert.That(Beacon.GetComponent&lt;TMP_Text&gt;(beacon).text, Does.Contain("Hello"))`
        /// 
        /// Also the returned class is not the interface <see cref="ILabelBeacon"/> but <see cref="LabelBeacon"/>,
        /// which provides some generic methods benefit unavailable on interfaces.
        /// </remarks>
        public static COMPONENTTYPE GetComponent<COMPONENTTYPE>(Enum label) where COMPONENTTYPE : Component
        {
            if (FindActive(label, out ILabelBeacon found))
            {
                return ((LabelBeacon)found).GetComponent<COMPONENTTYPE>();
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
        /// <exception cref="Exception">
        /// Thrown when found multiple beacons with the same <paramref name="label"/>.
        /// </exception>
        public static bool FindActive<BEACONTYPE>(BEACONTYPE label, out ILabelBeacon foundBeacon) where BEACONTYPE : Enum
            => FindActiveEnum(label, out foundBeacon);

        /// <summary>
        /// Non-generic <see cref="FindActive{BEACONTYPE}(BEACONTYPE, out ILabelBeacon)"/> for callers that
        /// already hold a boxed <see cref="Enum"/> (avoids passing <c>Enum</c> to an <c>Enum</c>-constrained
        /// generic).
        /// </summary>
        public static bool FindActiveEnum(Enum label, out ILabelBeacon foundBeacon)
        {
            // O(1) lookup via the active-beacon registry (see LabelBeacon) instead of scanning the scene.
            int count = LabelBeacon.CountActive(label, out LabelBeacon first);
            if (count == 0)
            {
                foundBeacon = null;
                return false;
            }
            if (count > 1)
            {
                throw new BeaconException($"Multiple beacons with label {label} found. This is considered an error.");
            }
            foundBeacon = first;
            return true;
        }

        /// <summary>
        /// Simulate a click on a beacon.
        /// The beacon label must be on <see cref="HandlerBeacon{T}"/> in the scene.
        /// </summary>
        /// <remarks>
        /// Definition of a click is pointer down this frame then up at the same coordinate the next frame.
        /// So you need a coroutine on this.
        /// 
        /// The gotcha of this "up" is that if you have an another click following, the next "down"
        /// will occur on the same frame as previous up. In some situation like you do clicking on the same button
        /// but expect the first click to disable the button and wait for the later one,
        /// you may ended up double clicking the button in the same frame if that disable didn't occur immediately.
        /// 
        /// (For example the disable is planned to be an effect from `TimelineAsset`
        /// that plays as a result of a button press, which need one more frame to take effect.)
        /// 
        /// This is **your bug** however, as it is possible in real play by having a player use the 2nd finger to touch
        /// on the same frame that the first finger lifted off, and that means Minefield is
        /// doing a good job catching this bug. You better ensure the disable
        /// comes immediately so <see cref="ClickWhen{T}(T, BeaconConstraint)"/> is usable consecutively
        /// without `yield return null` in between to "cheat" it.
        /// 
        /// (For example on `TimelineAsset` case, instead of just `Play()` it as a result of button press,
        /// also `Evaluate()` it so the disable take effect without waiting one more frame.)
        /// </remarks>
        public static async Awaitable Click<T>(T label, bool ignoreError = false) where T : Enum
        {
            if (FindActive(label, out ILabelBeacon b) && b is IHandlerBeacon nb)
            {
                //Debug.Log($"Type matches {nb.Label.GetType()} {label}");
                await Utility.RaycastClick(nb.ScreenClickPoint);
            }
            else
            {
                if (!ignoreError)
                {
                    throw new Exception($"Label {label} not found on any navigation beacon in the scene.");
                }
            }
        }
    }
}
# Walkthrough — Navigation & Assertion

Once beacons are placed, write the tests. Aim for tests that contain only beacon queries — you shouldn't need to dig up any object that has no beacon.

## Navigating with beacons

Navigation lives on the `static` entry point `Beacon.___`, each method taking an `enum` that must sit on a `NavigationBeacon<>`. Follow it with an NUnit-3-style constraint starting from `Is.___`. You may not need to assert anything at all — if navigation doesn't throw, the test already helped you.

```csharp
using System.Collections;
using UnityEngine.TestTools;
using E7.Minefield;

public class TitleSceneTest : SceneTest
{
    protected override string Scene => "Title";

    [UnityTest]
    public IEnumerator TouchToStartGoToModeSelect()
    {
        yield return ActivateScene();
        yield return Beacon.ClickWhen(TitleLogic.Navigation.TouchToStart, Is.Clickable);
        yield return Beacon.WaitUntil(ModeSelectScreen.Navigation.Training, Is.Clickable);
    }

    [UnityTest]
    public IEnumerator SkippingToModeSelect()
    {
        // Hack the static variable to influence the scene, per the guideline.
        SceneOptions.title = new SceneOptions.Title
        {
            titleMode = SceneOptions.Title.TitleMode.SkipToModeSelect
        };
        yield return ActivateScene();
        yield return Beacon.WaitUntil(ModeSelectScreen.Navigation.Training, Is.Clickable);
    }
}
```

Even with no `Assert`, the final `WaitUntil` + `Is.Clickable` is an implicit assertion that the player can arrive at the destination — if not, the test fails on timeout. It also catches the common bug where something is unintentionally clickable on the first `Awake`/`Start` frame, because it retries every frame.

A `Click` simulates pointer-down, waits a frame, then pointer-up and pointer-click together the next frame, so `yield return` is required. Internally, navigation reuses the same `EventSystem` raycasting routine from the `UnityEngine.UI` namespace that a real player triggers.

Non-beacon helpers (like waiting for a GameObject to become active) live in the `Utility` static class, but prefer sticking to `Beacon` so you can tell whether your beacons are sufficient.

## Assertion

`Assert.Beacon` is the entry point to assert on a beacon. `Is.___` emulates NUnit 3's constraint style but is not a complete extension (so you can't combine it with every NUnit expression). These are Minefield-only constraints.

```csharp
Assert.Beacon(beacon, Is.Active);
Assert.Beacon(beacon, Is.Inactive);
```

For use with `yield return` in a test:

```csharp
Beacon.WaitUntil(____, Is.____);

// Fail immediately if the beacon to click is not found or inactive.
Beacon.Click(____);

// Keep waiting to click; still fails on timeout if it stays unclickable.
Beacon.ClickWhen(____, Is.____);

// Spam an action until / while a condition holds — a "dumb AI" that muddles through.
Beacon.SpamUntil(____, Is.____, spamAction);
Beacon.SpamWhile(____, Is.____, spamAction);
```

Minefield-only checks and NUnit interop:

```csharp
// Return a bool instead of failing — handy for a break condition in a while loop.
Beacon.Check(____, Is.____);

// Shortcut for Assert.That(Beacon.Check(____, Is.____)).
Assert.Beacon(____, Is.____);

// Get the GameObject (or a component) behind a beacon for arbitrary NUnit assertions.
Beacon.Get(____);
Beacon.GetComponent<T>(____);
// e.g. Assert.That(Beacon.GetComponent<TMP_Text>(____).text, Does.Contain(____));
```

### Negating a constraint

Any Minefield constraint inverts with the `!` operator, so you can wait for or assert the **absence** of a condition:

```csharp
// The confirm button must NOT be clickable while the popup animates in.
yield return Beacon.WaitUntil(Screen.Navigation.Confirm, !Is.Clickable);

// Assert a beacon is not active.
Assert.Beacon(Screen.Beacon.Portrait, !Is.Active);
```

`!` is used instead of `Is.Not` on purpose: Minefield's `Is` inherits from `NUnit.Framework.Is`, so shadowing `Is.Not` would break ordinary NUnit expressions like `Is.Not.Null`. `!Is.Clickable` reads just as clearly and cannot collide.

### Timeouts and diagnostics

`WaitUntil` and `ClickWhen` do not wait forever by default. They fail after `Beacon.DefaultTimeout` seconds (30s by default, in unscaled time), and the failure message explains *why* the constraint never passed — for `Is.Clickable`, whether the beacon was found, was non-interactable, the raycast hit nothing, or something was drawn on top and swallowed the click.

```csharp
// Per-call override (seconds). A negative value uses Beacon.DefaultTimeout.
yield return Beacon.ClickWhen(Screen.Navigation.Play, Is.Clickable, timeout: 5f);

// Change the global default, or disable timeouts entirely for the old wait-forever behaviour.
Beacon.DefaultTimeout = 15f;
Beacon.DefaultTimeout = float.PositiveInfinity;
```

This only affects `WaitUntil`/`ClickWhen`; the [play-button pattern](play-button.md) (`[NoTimeout]` + `Utility.WaitForever()`) is unaffected.

### How beacons are found (registry)

Beacons register into an internal lookup while their GameObject is active (via `OnEnable`/`OnDisable`), so `Beacon.Get`, `FindActive`, and every per-frame constraint check are an O(1) dictionary lookup rather than a full-scene scan. Two consequences: it is much cheaper inside tight `WaitUntil` loops, and "active" follows the component's enable state precisely — a beacon on an inactive GameObject (or a disabled beacon component) is simply not findable, which is the intended contract. If you override `OnEnable`/`OnDisable` in a beacon subclass, call `base` or it won't register.

### Using `[Values]`

In NUnit, [`[Values(...)]`](https://github.com/nunit/docs/wiki/Values-Attribute) makes a parameterized test, and a bare `[Values]` with no arguments has [special meaning for `bool` and `Enum`](https://github.com/nunit/docs/wiki/Values-Attribute#values-with-enum-or-boolean): every case is generated automatically. Since a beacon label is an `Enum`, you can "try everything" with almost no code — for example, one test that clicks each language button and checks the result:

```csharp
[UnityTest]
public IEnumerator TestLanguageButtons([Values] LanguageScreen.Navi languageLabel)
{
    LocalSave.Manager.ResetActive();
    yield return ActivateScene();
    yield return Beacon.ClickWhen(languageLabel, Is.Clickable);
    yield return Beacon.WaitUntil(TitleLogic.Navigation.TouchToStart, Is.Clickable);
}
```

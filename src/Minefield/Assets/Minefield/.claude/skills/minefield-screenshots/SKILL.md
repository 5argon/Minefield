---
name: minefield-screenshots
description: >-
  Write play mode tests that photograph a Unity game with E7.Minefield's Screenshot feature — a fixture that walks the
  UI, captures at chosen moments, and builds a browsable HTML report where every language sits side by side. Use when
  adding or editing a screenshot suite, gathering reference images per language for a translator, setting up visual
  regression against baselines, or diagnosing captures that are mistimed, off by one, or hang partway through.
---

# Writing Minefield screenshot tests

The point of these tests is the pictures, not the pass. They catch what no assertion can — a translated label that
wrapped onto a line with no room, a name that overflowed its banner, a font fallback that kicked in for one script and
not another. Output is `<project>/Screenshots/index.html`, browsed by folder.

Keep them in **separate fixtures** from the tests that check behaviour. A screenshot run is slow, wants a pinned Game
View, and is read by a person; a behaviour run wants to be fast and fail loudly. Mixing them makes both worse.

## 1. Shape of a suite

Derive from `ScreenshotTest` (a `SceneTest`), and set `ShotGroup` — it becomes a folder on disk and a page in the
report.

```csharp
[Explicit]
public class TitleFlowScreenshots : ScreenshotTest
{
    protected override string Scene => "Title";

    [UnityTest]
    public IEnumerator Screens([Values("en", "ja", "th")] string language)
    {
        ShotGroup = "title-flow";
        Screenshot.Variant = language;
        yield return SelectLanguage(language);       // before the scene wakes up — §5

        // Activate before anything that can throw. A SceneTest that fails before activating leaves
        // a scene stuck at 0.9 progress, which wrecks the test after it.
        yield return ActivateScene();
        yield return Screenshot.RequireResolution(1080, 2340);

        yield return Beacon.WaitUntil(TitleLogic.Navigation.TouchToStart, Is.Clickable);
        yield return Shot("01-title", "Title with the touch to start prompt.", "title");
    }
}
```

`Shot(name, description = null, params tags)` files into `ShotGroup`. Report rows order by when a name was **first
captured**, not alphabetically, so a folder page reads as a walkthrough. Numeric prefixes only help someone browsing
the raw folders.

Fixture per subject (a game, a screen) is what makes the report navigable. For a suite parameterised over many
subjects, use `[TestFixtureSource]` and set `ShotGroup` from the parameter, so thirteen games make thirteen folders
rather than one.

## 2. Keep them out of ordinary runs

`[Explicit]` on **each concrete fixture** — the runner skips explicit fixtures unless the filter names them, so *Run
All* and a plain command line run walk past. Selecting a fixture in the Test Runner window still runs it.

`[Category]` on a shared base gives a way to ask for the whole set (`-testCategory Screenshot`).

The split is not a style choice: `[Category]` is `Inherited=true`, `[Explicit]` is `Inherited=false`. Putting
`[Explicit]` on a base class does nothing.

## 3. Pin the resolution

`Screenshot.RequireResolution(w, h)` before the first capture. Captures come from the back buffer, which in the editor
is whatever size the Game View happens to be, and a `CanvasScaler` that scales with screen size lays out differently
per aspect. A baseline at one aspect against a capture at another is a difference image of pure noise.

Pick a multiple of the project's `CanvasScaler` reference resolution so the layout is what the artists authored. If the
Game View refuses to resize, the call throws and says so — better than quietly photographing the wrong layout.

## 4. Choose capture points

A beacon becoming `Is.Clickable` already means *the UI settled into a known state*, so the places you were going to
wait anyway are the places worth photographing. `Screenshot.SettleFrames` (default 2) covers the gap between a state
being reached and it being finished drawing; raise it if captures come out mid-fade.

## 5. Follow observable state, never count actions

**The mistake that costs a whole run.** A loop that assumes every tap lands, and captures a fixed number of times, breaks
the moment the game swallows one — and games swallow input constantly, during intros, transitions, and animations.
The failure is quiet: the first capture is of the intro, every capture after is off by one, the final action is never
spent, and the test hangs waiting for a state that will never arrive.

Watch something the game actually exposes, and drive the count from data rather than from how many times you acted:

```csharp
int pageCount = info.tutorialInfo.PageCount;          // from the game's own data, not a guess

string photographed = null;
for (int page = 1; page <= pageCount; page++)
{
    while (OnScreenPage() == null || OnScreenPage() == photographed)
    {
        yield return null;                            // wait for a page you have not shot yet
    }
    yield return Utility.Wait(SettleSeconds);
    photographed = OnScreenPage();

    yield return Shot($"page-{page}");
    yield return Advance();
}

// Whatever actions are still owed. A swallowed one now costs a moment, not the run.
while (Beacon.Check(SomeLogic.Beacon.Logic, Is.Reporting.Status(Step.Finished)) == false)
{
    yield return Advance();
    yield return null;
}
```

Good things to watch, in order of preference: a **reporter** (`Is.Reporting.Status(...)`), a beacon's presence, or the
identity of a live object the game creates and destroys per step — a page's localized string key works well, since it
names the content rather than the frame.

Always end with a bounded "spend whatever is still owed" loop. It converts a swallowed action from a hang into a
delay, and it proves the count was right rather than merely plausible.

## 6. Variants

`Screenshot.Variant` names what is being varied between runs — a language code, an aspect ratio, a difficulty. Each
becomes a report column, and captures of different variants never overwrite each other, so several runs accumulate and
re-running one language refreshes only that column.

Switch language **before `ActivateScene()`**, so every string and font swap is right on the first drawn frame. With
`SceneTest` preloading the scene without activating it, the top of the test is exactly the right moment.

This is what makes the suite useful before there is anything to regress against: run once per language and the report
becomes the thing to hand a translator, and the thing to review what they send back.

## 7. Descriptions and tags

Write them at the call site so they cannot drift from the navigation that produced the capture:

```csharp
yield return Shot(
    "03-settings",
    "The settings dialog, the densest block of text in the game.",
    Tag.Menu, Tag.Dialog, Tag.TextHeavy);
```

The description shows under the name and is searchable. Tags become chips that cycle *leave alone → only these → hide
these*, and combine. Keep tags to a short fixed vocabulary — a `static class` of `const string` next to the fixtures
stops the chip row growing three spellings of one idea.

## 8. Baselines

Every capture always goes to `Current/`. Nothing ever writes `Baseline/`; it is filled only by promoting a run that was
looked at, via *Window ▸ Analysis ▸ Minefield ▸ Screenshots ▸ Accept Captures As Baseline*.

Pairing is by exact file path, so a variant is only ever compared against its own baseline. Scope a promotion by
controlling what is in `Current/` when accepting — *Clear Captures*, run only what should be re-baselined, accept.

`Screenshot.FailOnMismatch` turns drift into a failure; leave it off for a gathering run, which wants every capture
taken even when an earlier one moved.

## 9. Silence the outside world

A screenshot suite resets and writes the save, walks menus, and triggers analytics — all of which can reach a live
backend. Guard the game's outbound calls with Minefield's own flag, wrapped so it cannot reach a release build:

```csharp
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    if (E7.Minefield.Utility.MinefieldTesting)
    {
        return;
    }
#endif
```

Put it at the top of the outbound entry point, before anything that authenticates. Check analytics, save upload,
score submission, and sign-in — the ones that fire on scene entry matter most.

## 10. Verify

Let Unity recompile and check the Console. Without the editor open, build the test assembly directly with
`msbuild <TheTestAssembly>.csproj` (Unity regenerates a stale csproj on next open; a new file needs adding to it by
hand first).

A suite is only really verified by running one fixture and looking at the images. Check the **first** capture
especially — an off-by-one from a swallowed action always shows up there.

## Gotchas

| Symptom | Cause / fix |
| --- | --- |
| Captured image has no UI, only the game world | something rendered a camera into a `RenderTexture`. A `Screen Space - Overlay` canvas never appears that way; capture must read the back buffer, which `Screenshot` does |
| First capture is an intro or transition, everything after off by one | the first action was swallowed before the game was listening. Follow observable state (§5), do not count actions |
| Test hangs near the end of a sequence | the final action was never spent, so the end state never arrived. Add the trailing "spend what is owed" loop (§5) |
| `Ignoring depth surface load action as it is memoryless` on every capture | Unity on macOS + Metal, from the screen capture module. Native-emitted, so no log filter reaches it; harmless. `Screenshot.Method = CaptureMethod.BackBuffer` avoids the API entirely |
| Captures come out black with `CaptureMethod.BackBuffer` | the render pipeline had something bound at end of frame; go back to `CaptureMethod.ScreenCaptureModule` |
| Every difference image is noise | the resolution was not pinned (§3), or something is animating. Drive animations to a fixed time, or keep baselines only for screens that hold still |
| Baselines suddenly all "not comparable" | capture resolution changed. Re-accept, or restore the old Game View size |
| Fixtures appear but report themselves skipped | the fixture list includes subjects the suite does not apply to. Filter the `[TestFixtureSource]` member instead of ignoring at runtime, so they never appear |
| `[Explicit]` on a base class does not exclude anything | it is not inherited — put it on each concrete fixture (§2) |
| The test after this one fails oddly | a `SceneTest` ended before `ActivateScene()`, leaving a scene stuck at 0.9 progress. Activate first, assert after |
| Glyphs missing in one language only | a dynamic font atlas had not built them yet. Raise `Screenshot.SettleFrames` |
| Report shows a capture point in the wrong place | ordering follows first capture, so a renamed point lands at the end. Renames also strand the old baseline — delete it |

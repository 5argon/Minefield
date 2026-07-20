# Screenshots

Some things about a UI cannot be asserted. Whether a translated button label wrapped onto a second line that has no room for it, whether a long name pushed a price off the edge of a panel, whether a font fallback quietly kicked in for one script and not another — a person has to look. What a test framework can do is put that person in front of the right pictures.

`Screenshot` photographs the game at points you choose during a play mode test, and writes an HTML report that lays the pictures out side by side.

Because a beacon becoming `Is.Clickable` already means *the UI settled into a known state*, the places you were going to wait anyway are the places worth photographing:

```csharp
yield return Beacon.WaitUntil(TitleLogic.Navigation.TouchToStart, Is.Clickable);
yield return Shot("title");
```

## A screenshot test

Derive from `ScreenshotTest` rather than `SceneTest`. It is a `SceneTest` that files captures under the fixture name, so two fixtures may both photograph something called `title` without colliding.

Keep these fixtures apart from the tests that check behaviour. A screenshot run wants to be repeated once per language and then read by a human; a behaviour run wants to be fast and to fail loudly. Mixing the two makes both worse.

```csharp
public class Walkthrough : ScreenshotTest
{
    protected override string Scene => "Title";

    [UnityTest]
    public IEnumerator Flow([Values("en", "ja", "th")] string language)
    {
        Screenshot.Variant = language;
        yield return SelectLanguage(language);
        yield return Screenshot.RequireResolution(1080, 2340);
        yield return ActivateScene();

        yield return Beacon.WaitUntil(TitleLogic.Navigation.TouchToStart, Is.Clickable);
        yield return Shot("01-title");

        yield return Beacon.ClickWhen(TitleLogic.Navigation.TouchToStart, Is.Clickable);
        yield return Beacon.WaitUntil(ModeSelect.Navigation.TwoPlayers, Is.Clickable);
        yield return Shot("02-mode-select");
    }
}
```

Report rows are ordered by when a name was first captured, not alphabetically, so the report reads as a walkthrough of the flow. The numeric prefixes above are only there to help someone browsing the raw folders.

## Say what a capture is for

A capture point can carry a description and any number of tags, written at the call site so they sit next to the navigation that produced them and cannot drift apart from it:

```csharp
yield return Shot(
    "03-settings",
    "The settings dialog, which holds the densest block of text in the game.",
    Tag.Menu, Tag.Dialog, Tag.TextHeavy);
```

The description shows under the capture point's name and is searchable. Tags become chips at the top of the page: clicking one narrows to the captures carrying it, clicking again pushes those aside, and clicking a third time forgets about it. Several chips combine, so "show me everything tagged text-heavy that is not a dialog" is two clicks.

Descriptions are plain text in whatever single language the team reads — they are notes for whoever is looking, not part of the game.

Tags are worth keeping to a short, fixed vocabulary. A `static class` of `const string`s next to your fixtures costs nothing and stops the chip row from growing three spellings of the same idea.

## Finding things

Once a run photographs every screen in every language there are more images than anyone will scroll through, so the report is built to be narrowed rather than browsed:

- The index has a **search** over every capture point's folder, name, description, and tags. Hits are listed with a thumbnail and link straight to that capture point's place in its folder page.
- The index also offers an **All captures** view, for when you would rather see one flat list than a table of folders.
- A folder page can be searched, filtered by tag, and narrowed to particular **variants** — untick `ja` and it disappears from every row at once.
- **Columns** view puts the variants side by side at a size you control with a slider; **Grid** view packs the capture points into a contact sheet for scanning a lot of them quickly.
- **Only drifted** hides everything that still matches its baseline.

## Keeping them out of ordinary runs

A screenshot suite is slow, wants a pinned Game View, and produces something to look at rather than a pass or a fail. None of that belongs in the run you do after changing a line of code, so mark the fixtures `[Explicit]`:

```csharp
[Explicit]
public class Walkthrough : ScreenshotTest { … }
```

Unity's test runner skips explicit fixtures unless the filter names them, so *Run All* and a plain command line run walk straight past. Selecting the fixture in the Test Runner window and pressing Run still works, because a selection counts as naming it.

`[Explicit]` is not inherited, so it goes on each concrete fixture rather than on a shared base. `[Category]` *is* inherited, which makes a base class the right home for one:

```csharp
[Category("Screenshot")]
public abstract class MyScreenshotTest : ScreenshotTest { … }
```

That gives a way to ask for the whole set at once — `-testCategory Screenshot` on the command line, or the category dropdown in the Test Runner window — including suites added later.

## Variants

`Screenshot.Variant` names whatever you are varying between runs. A language code is the usual choice, but a device aspect ratio or a difficulty works the same way. Each variant becomes a column in the report, and captures of different variants never overwrite each other — so several runs accumulate into one report, and re-running a single language refreshes only that column.

This is what makes the feature useful before there is anything to regress against. Run the flow once per language and the report becomes the thing you hand a translator: every screen their text has to fit into, next to the language they are translating from. When their translation arrives, run it again and show them the same report.

## Baselines

A capture is compared against `Baseline/` whenever a file of the same name is sitting there, and the report grows a *Compare to baseline* mode showing the baseline, the new capture, and a difference image with every changed pixel painted magenta.

Nothing fails by default — a gathering run wants every capture taken even when an earlier one moved. Set `Screenshot.FailOnMismatch` to turn drift into a test failure, with `Screenshot.MismatchTolerance` deciding how much drift is too much and `Screenshot.PixelThreshold` deciding how far one pixel may drift before it counts at all.

When a change is real and intended, `Window ▸ Analysis ▸ Minefield ▸ Screenshots ▸ Accept Captures As Baseline` promotes the last run over the baselines.

## Pin the resolution

`Screenshot.RequireResolution` is worth calling before the first capture of a run. Captures come from the back buffer, which in the editor is whatever size the Game View happens to be, and a `CanvasScaler` set to scale with screen size lays out differently at different aspect ratios. A baseline taken at one aspect and a capture taken at another produce a difference image that is nothing but noise.

If the Game View refuses to resize, the call throws and tells you to set its size dropdown by hand — which is better than quietly photographing the wrong layout.

## What comes out

Everything lands outside `Assets`, at `<project>/Screenshots` in the editor and under `Application.persistentDataPath` in a player, so Unity never imports any of it.

```
Screenshots/
  Current/<folder>/<name>.<variant>.png    this run
  Baseline/<folder>/<name>.<variant>.png   what it is compared against
  Diff/<folder>/<name>.<variant>.png       changed pixels, magenta
  manifest.json
  index.html                               folders, with counts and drift
  folder-<folder>.html                     one page per folder
  screenshots.css
  screenshots.js
```

The variant sits at the end of the file name rather than in a folder above it, so a capture point's folder holds every variant of it together. Opening `Current/tutorial-pinball` shows every page in every language at once — the same thing that folder's page in the report shows.

The report is a small static site rather than one page: an index listing every folder, and a page per folder you click into and back out of. A run that photographs thirteen games in three languages produces far too many images to scroll through in one go, but one folder at a time is the amount a person can actually judge.

Track `Baseline/` and ignore the rest. Pages are rewritten after every single capture rather than at the end of the run, so a run that fails or is interrupted still leaves a readable report of everything up to that point.

## Getting a capture worth comparing

The back buffer is read through the engine's screen capture module when the project has `com.unity.modules.screencapture` enabled, and through `Texture2D.ReadPixels` when it does not. The fallback trusts that no render texture is bound at end of frame, which a render pipeline is free to break, so enable the module if you can. Either route stores the result without an alpha channel, so baselines stay comparable across the two.

`Screenshot.Method` picks between them without touching the module. The reason to change it is a Unity quirk: on macOS with Metal and a scriptable render pipeline, the screen capture module makes the engine log `Ignoring depth surface load action as it is memoryless` on every single capture. It comes from native rendering code, so no log filter reaches it, and the captures are correct regardless — but a few hundred of them bury everything else in the console. Reading the back buffer directly avoids the message entirely:

```csharp
Screenshot.Method = CaptureMethod.BackBuffer;
```

Check one fixture after switching. If captures come out black, or show the world without its interface, then the pipeline did have something bound after all and the module is the way back.

Reading the back buffer is also the reason this works on UI at all. A `Screen Space - Overlay` canvas never appears when you render a camera into a `RenderTexture`, and overlay canvases are usually the entire thing being localized.

Two more things decide whether a difference image is meaningful:

- **Settling.** A beacon proves the game reached a state, not that it finished drawing it. `Screenshot.SettleFrames` waits a couple of frames first; raise it if captures come out mid-fade.
- **Motion.** Anything animating — a skeletal animation, a playing `PlayableDirector`, a particle system — lands on a different frame each run and will light up the whole difference image. For eyeballing translations this does not matter. For pixel comparison, drive those to a fixed time before capturing, or keep baselines only for the screens that hold still.

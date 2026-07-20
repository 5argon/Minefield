# Changelog

All notable changes to this package are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- **Screenshots.** `Screenshot.Take` photographs the back buffer at a chosen moment of a play mode
  test, and `ScreenshotTest` is a `SceneTest` for fixtures whose job is to walk the game and
  photograph it. Captures are written outside `Assets` alongside an HTML report.
  `Screenshot.Variant` gives a run a column in that report, so running the same fixture once per
  language produces every screen in every language next to each other — for spotting text that
  clipped or wrapped, and for handing to a translator as the reference of where their strings will
  land. A capture is compared against `Baseline/` when one exists, with a magenta difference image
  and an optional `Screenshot.FailOnMismatch`. Back buffer capture is used rather than a camera
  render because a `Screen Space - Overlay` canvas does not appear in the latter. See the
  Screenshots manual page.
- The report is a small static site : an index of folders you click into and back out of, plus a
  page per folder. It carries a search over every capture point's folder, name, description, and
  tags; a flat *All captures* view; per-variant filtering; *Columns* and *Grid* layouts with a size
  slider; and an *Only drifted* filter.
- `Screenshot.Method` chooses between the engine's screen capture module and a direct frame buffer
  read. The module is the default where the project has it, but on macOS with Metal it makes the
  engine log `Ignoring depth surface load action as it is memoryless` on every capture, which
  `CaptureMethod.BackBuffer` avoids.
- Captures can be given a description and tags at the call site
  (`Shot(name, description, tags…)`), which keeps them next to the navigation that produced the
  capture. Descriptions show beside the images and are searchable; tags become chips that cycle
  between leaving a tag alone, showing only what carries it, and hiding what carries it.
- `Window ▸ Analysis ▸ Minefield ▸ Screenshots` menu entries to open the report, promote a run's
  captures to baselines, and clear captures.
- A `minefield-screenshots` agent skill bundled at `.claude/skills/`, covering how to write a
  screenshot suite and the traps that waste a run — chiefly following observable state instead of
  counting actions the game is free to swallow.

### Fixed

- `Utility.ActionBetweenSceneAwakeAndStart` now unsubscribes its `SceneManager.sceneLoaded` handler once the target scene fires, so handlers no longer accumulate across Play sessions under Fast Enter Play Mode / no domain reload (default for new projects in Unity 6.6+, and the only option in 6.8).

### Changed

- **Async surface migrated from `IEnumerator` coroutines to `UnityEngine.Awaitable`.** The
  driving API — `Beacon.WaitUntil`, `Beacon.ClickWhen`, `Beacon.Click`, `Beacon.SpamUntil`,
  `Beacon.SpamWhile`, and the `Utility` click/wait helpers (`RaycastClick`, `WaitUntilFound`,
  `WaitUntilSceneLoaded`, `WaitForever`, `TouchLowerHalf`/`TouchUpperHalf`) plus
  `Graphic.ClickAtCenter()` — now returns `Awaitable` instead of `IEnumerator`. This enables
  `await Beacon.ClickWhen(...)` inside `async Task` play-mode tests, `try`/`catch` around waits,
  and real return values.
- `Utility.WaitUntilFound<T>()` now returns `Awaitable<T>` and hands back the component it found,
  instead of returning nothing.
- Minimum `com.unity.test-framework` raised to `1.6.0` (adds `MaxTime` on async tests and async
  `SetUp`/`TearDown`).

### Compatibility

- Existing `[UnityTest] IEnumerator` tests that do `yield return Beacon.ClickWhen(...)` **keep
  working unchanged**, because `Awaitable` implements `IEnumerator`.
- Breaking for callers that (a) stored these results in an `IEnumerator` variable, (b) passed a
  `Func<IEnumerator>` to `SpamUntil`/`SpamWhile` (now `Func<Awaitable>`), or (c) relied on
  `WaitUntilFound<T>()`/`ClickAtCenter()`'s old return type. Consider a `2.0.0` bump on release.

## [1.0.0]

### Changed

- Repository restructured to the UniTask-style layout: the repo root is now a real Unity
  project and the package lives at `src/Minefield/Assets/Minefield`. Install via
  `https://github.com/5argon/Minefield.git?path=src/Minefield/Assets/Minefield`.
- Minimum Unity version is now 6.3 LTS (6000.3).
- Documentation moved to a DocFX site (with a generated C# API reference) hosted at
  <https://exceed7.com/minefield>.

### Added

- `!` operator to invert any Minefield constraint (e.g. `!Is.Clickable`) for asserting the
  absence of a condition.
- Default timeouts on `WaitUntil` / `ClickWhen` (`Beacon.DefaultTimeout`, 30s) with
  diagnostic failure messages explaining why a constraint never passed.
- O(1) beacon registry via `OnEnable`/`OnDisable`, replacing full-scene scans.

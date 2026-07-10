# Changelog

All notable changes to this package are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

## [Unreleased]

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

# Changelog

All notable changes to this package are documented here. This project adheres to
[Semantic Versioning](https://semver.org/).

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

# Minefield Test Tools

Concise play-mode **navigation** testing for Unity uGUI. Attach enum "beacons" to scene
objects and drive them by identity — find, wait-until-clickable, click, assert — without
baking screen coordinates or object names into the test.

This repository is a **Unity project** (so you can open it, build test scenes, and run the
Test Runner against the package). The package itself lives at
[`src/Minefield/Assets/Minefield`](src/Minefield/Assets/Minefield), and its full documentation is in that folder's
[README](src/Minefield/Assets/Minefield/README.md).

## Install (Unity Package Manager, Git URL)

Unity can install a package that lives in a repo subfolder via the `?path=` query. Add this to
your project's `Packages/manifest.json` dependencies:

```json
"com.e7.minefield": "https://github.com/5argon/Minefield.git?path=src/Minefield/Assets/Minefield"
```

Or in the Editor: **Window ▸ Package Manager ▸ + ▸ Add package from git URL…** and paste:

```
https://github.com/5argon/Minefield.git?path=src/Minefield/Assets/Minefield
```

Pin to a tag or commit by appending `#<ref>` after the query, e.g.
`…?path=src/Minefield/Assets/Minefield#v1.0.0`.

Then add Minefield to your game's test discovery in `Packages/manifest.json`:

```json
"testables": [
  "com.e7.minefield"
]
```

See the [package README](src/Minefield/Assets/Minefield/README.md) for why `testables` is required, how to
link the `E7.Minefield` and `E7.Minefield.TestTools` assemblies, the design rationale behind
beacons, and the full API walkthrough.

## Requirements

- Unity 2022.3 LTS or newer
- uGUI (`com.unity.ugui`) and Test Framework (`com.unity.test-framework`)
- Addressable Asset System is optional — scene names can be plain build scenes or AAS keys

## License

See [LICENSE.md](src/Minefield/Assets/Minefield/LICENSE.md).

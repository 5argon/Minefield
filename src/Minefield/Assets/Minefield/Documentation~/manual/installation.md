# Installation

## Requirements

- **Unity 6.3 LTS (6000.3) or newer.**
- **uGUI** (`com.unity.ugui`) and **Test Framework** (`com.unity.test-framework`), both included with the Editor.
- `asmdef` support: the package is excluded from your game build via the Test Assembly checkbox, so no preprocessor directives are scattered through the code.
- **Addressables are optional.** When present, a scene name you pass is treated as either a regular build scene or an AAS key that resolves to the scene.

## Add the package

The package lives in the `src/Minefield/Assets/Minefield` subfolder of the repo, so install it from the Git URL with Unity's `?path=` query. Add this to your `Packages/manifest.json`:

```json
"com.e7.minefield": "https://github.com/5argon/Minefield.git?path=src/Minefield/Assets/Minefield"
```

Or use **Window ▸ Package Manager ▸ + ▸ Add package from git URL…** with:

```
https://github.com/5argon/Minefield.git?path=src/Minefield/Assets/Minefield
```

Pin a release by appending `#<tag-or-commit>`, e.g. `…?path=src/Minefield/Assets/Minefield#v1.0.0`. A Git-URL package does not auto-update; remove its entry from the `packages-lock.json` `lock` section to refetch the latest.

## Wire up the assemblies

- `E7.Minefield` — link your **game** `asmdef` to this.
- `E7.Minefield.TestTools` — link your **test** `asmdef` to this.
- Finally, add this to your project's `manifest.json`:

```json
  "testables": [
    "com.e7.minefield"
  ],
```

### Why `manifest.json` needs the `testables` entry

A reference from an `asmdef` to `UnityEngine.TestTools` and `NUnit.Framework` is only allowed for an `asmdef` marked "Test Assembly". Minefield, as a test-tools extension, needs those to call `Assert` for you, so `E7.Minefield.TestTools` has "Test Assembly" on. But Unity has a rule that linking a "Test Assembly" `asmdef` in your game to a "Test Assembly" `asmdef` in a package has no effect — which normally makes a "test tools extension assembly" impossible.

The workaround is `testables` in your game's `manifest.json`. Its intended purpose is to include a package's own tests in your game's test run, but it has the side effect of letting your game link up the "Test Assembly". Since Minefield contains no tests for itself, listing it under `testables` is the official way to use it — with none of its own tests polluting your project.

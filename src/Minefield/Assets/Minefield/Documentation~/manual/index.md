# Overview

`Minefield` is a library for writing concise **navigation** play mode tests in Unity, plus a set of guidelines for designing a game so it stays testable this way. The goal is that you never have to press the play button while developing navigation: the Unity Test Runner starts the scene for you, and if you make that a habit, your tests are already written by the time the scene is done. Writing a Minefield test takes so little time that you can do it while you build — when your brain is still in *creative mode* — instead of "giving in" to the play button and skipping the test.

## Motivation and emphasis on navigation

A long time ago the author ran a small team, and every release ended the same way: someone had to "go to all scenes and push all buttons" to make sure nothing regressed. Almost every game needs that kind of assurance, regardless of genre — more specific checks of game variables or presentation come later. The two original requirements, *go to all scenes* and *push all buttons*, map directly onto Minefield's two design pillars:

- **A single scene is a unit of test** — you can start any scene cleanly and test it in isolation.
- **Navigate when able, otherwise wait** — the test tries to interact every frame until it can, rather than sleeping for a guessed duration.

## Design in brief

A test class targets one scene via the `Scene` property, and every case in that class re-starts that scene fresh. You call `ActivateScene()` explicitly so you get a chance to set up state before the scene starts, without repeating the scene name on every case.

The defining feature is the **beacon** — a self-declared `enum` attached to a scene object through a small `MonoBehaviour`. It lets the test navigate by identity ("the training-mode button") without knowing any GameObject's name or any animation length to wait for. That keeps the test loosely coupled to the scene: the scene can be arbitrarily complex while the test stays a simple series of waits and clicks.

The theme running through Minefield is **test metadata**: by adding a little metadata (beacons, and later [reporters](reporters.md)) into your actual game, you drastically cut the amount of code each test needs. Because test code is written and repeated many times, that trade is very reasonable — and it encourages broader coverage. Think of beacons not as noise from a testing library, but as necessary components that make a scene *complete* and testable, even though they do nothing at runtime.

Read on for [why an explicit beacon is the right trade-off](why-beacons.md), and the [guidelines](guideline.md) that keep a project testable.

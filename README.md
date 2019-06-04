# Minefield Test Tools

**MINEFIELD DEPLOYED** (in construction)

## How to use

Clone it and use `+` button in your package manager to look for `package.json`, or use GitHub include by add this line to your `manifest.json` :

```json
"com.e7.notch-solution": "git://github.com/5argon/NotchSolution.git"
```

It does not update automatically when I push fixes to this repo. You must remove the `lock` section that appears in you `manifest.json` file to refectch.

Then,

- `E7.Minefield` : Link your game `asmdef` to this.
- `E7.Minefield.TestTools` : Link your test `asmdef` to this.
- Finally, go to `manifest.json` and include this at the end.

```json
  "testables":[
    "com.e7.minefield"
  ],
```

## Testing assembly shenanigans

- Reference from `asmdef` to `UnityEngine.TestTools` and `NUnit.Framework` is strictly for `asmdef` that is checked as "Test Assembly".
- Of course `Minefield` as a test tools extension need those to call `Assert` and such for you. So `E7.Minefield.TestTools` has "Test Assembly" on.
- There is a rule that linking "Test Assembly" `asmdef` in you game to `asmdef` in packages with also "Test Assembly" status has no effect. This had been a problem with trying to include ECS's base class for testing fo so long too. In effect, "test tools extension assembly" simply cannot be done in Unity.
- However there is a kinda hack with `testables` in your game's `manifest.json`. Its real intention seems to be so that your game's test includes the listed package's tests too. `Minefield` is a test **tools**, and contains no actual test for itself. However, `testables` have a side effect that it make your game able to link up "Test Assembly". And now it is luckily that `Minefield` doesn't have any tests for itself this became the only way to use it. (An opposite to ECS's test assembly, just wanting to use those classes will pollute your project with ECS tests.)

## Minefield's guideline for testable Unity project

- A **single** `Scene` is a testable unit. Don't make it a scene if you think you can't test on it individually, instead, use the improved prefabs from 2018.3.
- All scenes must work by itself under `LoadSceneMode.Single`, avoid `LoadSceneMode.Additive`. You should **not** compose your scene such that it includes multiple additive scenes, even if all that must started from loading a **single** scene. That scheme may be tempting but it make your project less testable. You should instead use 2018.3 nested prefabs to facilitate assembling scene from multiple pieces. Do not use an another scene as those pieces, use only prefabs. Note that non-additive scene could still be loaded asynchronously, only that when it activates it will clean up previous game objects. (Actually, the backend of improved prefabs is reusing the scene data structure! Every prefabs are scenes now.)
- A scene may only change its starting behaviour due to **external** `static` variables.
    - It must be possible to hack up this `static` variable before starting the scene to produce every kind of possible outcomes. For example, if your title screen is planned to be playing full intro on starting the game, but skipping intro if you back to it from other menu, you must have only a `bool` in your `static` that determines the outcome should the title plays intro or not. You should not try to figure out what is the previous scene, instead the previous scene will modify this `bool` and title screen doesn't have to be aware.
    - This `static` variable will be on a different `asmdef` that the scene refers to. In effect due to circular dependency restriction, the `static` variable has no knowledge of any scene's `asmdef`. (Other than variable naming which could be the name of scene that will take the variable) The restriction will force you to not use any scene specific data type on this `static` variable and use more primitive fields. You may use scene-agnostic data type such as your struct that represent player's save data.
    - You should not rely on leftover `GameObject` from previous scenes.
    - You should not make an inspector exposed public field on some objects in the scene in order to customize its starting behaviour. To customize starting behaviour as you iterate your game, make a `[RuntimeInitializeOnLoad]` script that hack and change the `static` variable before your scene loads. The scene should not be aware of this script, it should just load from whatever in the `static` variable at that time. If you need inspector GUI to do this, make a `ScriptableObject` in `Resources` then load and migrate its value to the `static` variable in your `[RuntimeInitializeOnLoad]`.
    - You should not rely on loading persistent file from disk. That file must be loaded and put into the `static` variable for the scene to read. If this load must be done every time as a saved player data for example, this logic should not be in any of scene's game object `Awake` or `Start` (the so-called "entry point") but instead you must use `[RuntimeInitializeOnLoad]` for the real entry point, so the code become scene-agnostic. You may `if` the scene name with `SceneManager` in this code in order to do it on only certain scene, as a workaround for not being able to use that scene's game objects.
    - All this is for that in testing code we have no GUI. We will be configuring by replacing the `static` variable with desired values and starting the scene over and over. If you design your `static` right, all possible outcomes will be covered.
- All scenes must be able to pass a test by just loading it without touching anything else, and wait for 5 seconds. With this design you are able to make a "lazy man's test" by just try loading each individual scene. Some design that prevents this is a scene which require other scenes to function, which if you followed the guideline such scene doesn't exist.
- In normal development, it must be possible to press play mode on any scene and start playing from that scene, no matter how "cheated" that may felt to you. You will be able to change up the `static` variable in order to test play your game on every possible conditions.
- If navigational component was designed with uGUI's `Selectable` componen, it's `interactable` field should be prevented appropriately when player shouldn't be able to interact with it. This way the test could interact as soon as `interactable` became true. And this way the test could detect bugs such as able to interact while the screen transition or on the first/clutch frame.
- If navigational component was designed with uGUI's event handlers such as `EventTrigger`,
- All scenes must have its own `asmdef` that should link to "core" `asmdef` with the `static` variable definition.

## Design

After following the guidelines, you will be able to use `Minefield` for the following benefits.

### Assembly planning

- `Minefield` should be completely independent from your **scene**, except for **beacons** (later on this). You will be referencing `E7.Minefield.TestTools` in your test assembly, but that assembly doesn't have to refer to any scene's `asmdef`. It could only refer to the "core" `asmdef` in order to hack up the `static` variable. Then `Minefield` could just start the scene and nothing more. If you followed the guidelines, the scene will be in your desired state to test.
- You could still reference your scene's `asmdef` if there is something specific you want to ask for. Also if you store your beacon label in there per scene `asmdef` you will have to reference to write the test.
- However even if you reference your scene `asmdef`, asserting on exposed fields of things is not a good design for test, since in the end you will use them as necessary portals to jump to the thing you actually want to test rather than wanted to test on them, and later you may introduced new exposed field just because you can't go to the desired things to test. This kind of "for test" exposed field should be avoided. (And actually exposed fields should be `[SerializeField] private` rather than `public`, so you can't use them from tests anyway.)

### `SceneTest`

By subclassing from this class in your test assembly :

- Each subclass will be a test for a single scene. You will have to provide your scene name because the `abstract` variable will ask you to.
- Each test case will be a fresh start of this scene. The built-in `[SetUp]` and `[TearDown]` will take care of everything for you.
- You will need to call `ActivateScene()` `protected` method **manually**. This design gives you an opportunity to hack up the `static` variable before allowing the scene to start.
- After starting the scene you will want to check up on something or navigate inside the scene after waiting for some period of time. We have no GUI in writing tests and it is difficult to use methods like `GameObject.Find` or `FindObjectOfType` to get the desired object to test "blindly". For this I have designed something called test **beacons**.

### `TestBeacon`

This is a way to use `enum` to refer to things in your scene. It make writing a test more fun because they could be auto completed and self described, rather than having to use `GameObject.Find` which rely on object's `string` name that may be refactored without the test updating along. `enum` could be mass-refactored by most code editor.

To use beacons, first you will declare some `enum` that represents all possible actions in the scene. This is called a beacon's **label**. This is like Flux or Redux's action string if you came from front end dev, however they could represent any test checking point and doesn't have to be strictly "action". Other than they will help us test, it is an overview of what's possible and a checklist if you are missing any tests or not.

```csharp
public class ModeSelectScreen : MonoBehaviour
{
    public enum Action
    {
        SwitchCharacter,
        ChangeName,
        Garden,
        TwoPlayers,
        EndlessParty,
        ThreeWins,
        Training,
        Arcade,
        ShowOption,
        HideOption,
        ChangeLanguage,
        APRanking,
        Back
    }
    ...
```

Next, declare a new class with that `enum` as the delegate of `TestBeacon<>`.

```csharp
using E7.Minefield;
public class ModeSelectBeacon : TestBeacon<ModeSelectScreen.Action> { }
```

You are now ready to attach this new class as a component to `GameObject` in the scene. The point of attach should be the raycast receiving elements, which depends if you got a `Button`, `EventTrigger`, or something else. The test tools can help you click on these objects that has a beacon component on it. It is also OK to attach beacons to any `GameObject`. You could query for them for assertion in the test, just that you can't use clicking methods on it.

Make sure you don't have to dig up any other objects that doesn't have a beacon. You will be using only the beacon's label `enum` in navigation and assertion.

### Navigation with beacons
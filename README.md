# Minefield Test Tools

`Minefield` is a library (still in-development) to help you program a concise play mode test in Unity, plus a guideline to design the game so that it is testable by this library.

This is an example of a complete test that could try to go from title scene to the next scene.

```csharp
using System.Collections;
using UnityEngine.TestTools;
using E7.Minefield;

public class TitleSceneTest : SceneTest
{
  protected override string Scene => "Title";

  [UnityTest]
  public IEnumerator TitleToModeSelectToTrainingMode()
  {
    ActivateScene();
    yield return Beacon.ClickWhenClickable(TitleLogic.Navigation.TouchAnywhere);
    yield return Beacon.WaitUntilClickable(ModeSelectScreen.Navigation.EnterTrainingMode);
  }
}
```

A defining feature includes the test **beacon**, which allows you to use an easy to understand `enum` that you declared yourself to navigate the scene without knowing any game object's name or any animation length to wait for. The `enum` is linked to scene object by a special `MonoBehaviour` carrying this `enum` serialized together with your scene, which prevents brittle test that would be written with hard-coded object name or component type matching. Plus it allows you to use object search box in Scene view to find and manage all beacons easily.

Beacons works well with 2018.3 new prefab workflow. In the case that you have multiple instances or variants of the prefab on the scene, you could vary the beacon as overrides to discern game objects in the test. In object name or component type matching way of writing tests, this would likely produce duplicate entries that you have to deal with.

Navigations are simulated by recreating the same `EventSystem` raycasting routine from `UnityEngine.UI` namespace that is used by real player, referenced from the [`UnityEngine.UI` repository](https://bitbucket.org/Unity-Technologies/ui).

An another feature is that you could write multiple tries per scene without specifying the scene name to load on every test case, thanks to `abstract` subclassing scheme. This make it possible to organize your tests into one class per scene. An explicit `ActivateScene()` call allows you to modify any context that would influence the test in your own `[SetUp]` or `[UnitySetUp]` before it starts.

## Requirements

- .NET 4.x : C# 7.3 needed for `Enum` generic constraints, `is` pattern matching expression, and expression bodied methods.
- `asmdef` support, the project is properly excluded from your actual game with Test Assembly checkbox, without `asmdef` it will be in your game since I don't want to litter preprocessor directives everywhere.
- Addressable Asset System package : The test could use the name you provided as either regular scene name or AAS key that should lead to the scene.
- Unity 2019.1 : `asmdef` version define feature so I could exclude AAS code from the library if you don't use AAS in the project.

## How to use

Clone it and use `+` button in your Package Manager menu to look for `package.json`, or use GitHub include by adding this line to your `manifest.json` :

```json
"com.e7.minefield": "git://github.com/5argon/Minefield.git"
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

Reasons for the need of `manifest.json` modification :

## Testing assembly shenanigans

- Reference from `asmdef` to `UnityEngine.TestTools` and `NUnit.Framework` is strictly for `asmdef` that is checked as "Test Assembly".
- Of course `Minefield` as a test tools extension need those to call `Assert` and such for you. So `E7.Minefield.TestTools` has "Test Assembly" on.
- There is a rule that linking "Test Assembly" `asmdef` in you game to `asmdef` in packages with also "Test Assembly" status has no effect. This had been a problem with trying to include ECS's base class for testing fo so long too. In effect, "test tools extension assembly" simply cannot be done in Unity.
- However there is a kinda hack with `testables` in your game's `manifest.json`. Its real intention seems to be so that your game's test includes the listed package's tests too. `Minefield` is a test **tools**, and contains no actual test for itself, however, `testables` have a side effect that it make your game able to link up "Test Assembly". And now it is luckily that `Minefield` doesn't have any tests for itself, this became the official way to use it. (An opposite to ECS's test assembly, just wanting to use those classes will pollute your project with ECS tests.)

## Minefield's guideline for testable Unity project

### A scene for testable unit, a prefab for content composition

- A **single** `Scene` is a testable unit. Don't make it a scene if you think you can't test on it individually, instead, use the improved prefabs from 2018.3.
- All scenes must work by itself under `LoadSceneMode.Single`, avoid `LoadSceneMode.Additive`. You should **not** compose your scene such that it includes multiple additive scenes, even if all that may started from loading a single scene. That scheme may be tempting in the past, but it make your project less testable, and also we now have 2018.3 nested prefabs to facilitate assembling scene from multiple pieces. Do not use an another scene as those pieces. Note that non-additive scene could still be loaded asynchronously, only that when it activates it will clean up previous game objects. (Actually, the backend of improved prefabs is reusing the scene data structure! Every prefabs are scenes now.)
- All scenes must be able to pass a test by just loading it without touching anything else, and wait for 5 seconds. With this design you are able to make a "lazy man's test" by just try loading each individual scene. Some design that prevents this is a scene which require other scenes to function, which if you followed the guideline such scene doesn't exist.
- In normal development, it must be possible to press play mode on any scene and start playing from that scene, no matter how "cheated" that may felt to you.

### A `static` variable to influence the entire scene

A scene may only change its starting behaviour due to **external** `static` variables.

- It must be possible to hack up this `static` variable before starting the scene to produce every kind of possible outcomes. For example, if your title screen is planned to be playing full intro on starting the game, but skipping intro if you back to it from other menu, you must have only a `bool` in your `static` that determines the outcome should the title plays intro or not. You should not try to figure out what is the previous scene, instead the previous scene will modify this `bool` and title screen doesn't have to be aware.
- This `static` variable will be on a different `asmdef` that the scene refers to. In effect due to circular dependency restriction, the `static` variable has no knowledge of any scene's `asmdef`. (Other than variable naming which could be the name of scene that will take the variable) The restriction will force you to not use any scene specific data type on this `static` variable and use more primitive fields. You may use scene-agnostic data type such as your struct that represent player's save data.
- You should not rely on leftover `GameObject` from previous scenes.
- You should not make an inspector exposed public field on some objects in the scene in order to customize its starting behaviour. To customize starting behaviour as you iterate your game, make a `[RuntimeInitializeOnLoad]` script that hack and change the `static` variable before your scene loads. The scene should not be aware of this script, it should just load from whatever in the `static` variable at that time. If you need inspector GUI to do this, make a `ScriptableObject` in `Resources` then load and migrate its value to the `static` variable in your `[RuntimeInitializeOnLoad]`.
- You should not rely on loading persistent file from disk. That file must be loaded and put into the `static` variable for the scene to read. If this load must be done every time as a saved player data for example, this logic should not be in any of scene's game object `Awake` or `Start` (the so-called "entry point") but instead you must use `[RuntimeInitializeOnLoad]` for the real entry point, so the code become scene-agnostic. You may `if` the scene name with `SceneManager` in this code in order to do it on only certain scene, as a workaround for not being able to use that scene's game objects.
- All this is for that in testing code we have no GUI. We will be configuring by replacing the `static` variable with desired values and starting the scene over and over. If you design your `static` right, all possible outcomes will be covered.
- There should be no `static` variable configuration that produces error on starting any scene, even default values. This will help you keep `null` orderly, and take care of default value type.
- The scene cannot have a logic that hack and change its own `static` variable, the scene can only change other scene's `static` variable in order to navigate to that scene. Some exception includes transition to the same scene, for example changing game's language and you want to refresh the scene.
- Instead of `static` adjustment before starting the scene, it is also acceptable to vary scene's behaviour by making a [prefab variant](https://docs.unity3d.com/Manual/PrefabVariants.html) with small difference, then put it on a different scene. For example :
    - I have a character select screen which is a bit different depends on if you come here on single player or two players mode. Instead of a `bool isTwoPlayers;` on the `static`, I could make a big prefab for use in two players mode first and name the scene `TwoPlayerCharacterSelect`.
    - Then make a variant of that big prefab that set inactive the right side character selector, then put that variant in a new scene called `SinglePlayerCharacterSelect`. This way, you are still using prefabs to compose your game in a way that it is maintainable. (Changes on either one should reflect to the other)
    - And also each scene is still a test unit according to the guidelines. In some case it may be better that you could explicitly say in the test that you want to test the single player one or two players one by scene name.
    - One other benefit is that at design time you could quickly switch between 2 variants and visually see the design, because `static` way only works at runtime. `static` is still needed for further adjusting details of the scene in dynamic way, such as unlocked characters.

### Keeping game's navigation in check

Games are full of **animation and transitions**, bugs in this area includes that user may able to do something when you don't want them to. This guideline is that you must properly **prevent** navigation game objects from being clicked when appropriate, like mid-transition. One common place for this kind of error is the first/last frame where you are going to disable things but it is still possible to interact in that frame.

In manual testing, some way is "can you break this test" where you have a user randomly spam the interested object or even an entire screen at all times to see if no strange behaviour occurs.

In automated testing, the test is able to simulate a fake click. However, one of a big test pain point I found is that it is difficult to blindly wait for x seconds and hope that the simulated click will go through. This wait may be more than necessary and adds test time, or less than necessary and cause test bugs that the click hits the air and does nothing.

How could automated test do an equivalent of manual "can you break this" test? I believe the correct approach is the test should repeatedly **try** to interact the interested object every frame until it is able to. This way it is even better than manual test since there is no missed frames. With `Minefield`, the test has several "wait until clickable (then click it)" methods that help waiting for these transitions, **without the test knowing transition length**. Next, you will learn what exact condition `Minefield` checks and wait for the click.

- If the player could do something at any moment, then so could the test. If this breaks the game then you have found a bug such as able to interact while the screen transition or on the first/clutch frame. As a principle, `Minefield` has some methods try every frame to click it until it is fine to click. You can choose to actually interact it or just wait for it.
- If navigational component was designed with uGUI's `IPointerDown/Up/ClickHandler`, (e.g. `EventTrigger`) if a raycast could hit it **the first**, the test will assume that it is now safe to simulate a click. You can prevent this by :
    - Parent `CanvasGroup` with `blocksRaycasts` as `false`.
    - Setting `raycastTarget` on your `Graphic`.
    - Make the ray receiver component disabled.
    - Make the ray receiver game object inactive.
    - Put something else on top like an 0 alpha `Image` that blocks raycasts, so your object is no longer the first to receive a raycast. This is common for popup dialog which may use transparent or darkened backgroud to prevent everything behind from being touched while the dialog is open.
- Additionally, if navigational component was designed with uGUI's `Selectable` component, it's `IsInteractable()` status should be prevented appropriately when player shouldn't be able to interact with it. In this case, it **do blocks raycasts** but nothing will happen if you click it for real in the game. Because of this if you have `Selectable`, `Minefield` will add one more criteria that it must also be `interactable`. You could prevent this by : 
    - Parent `CanvasGroup` with `interactable` as `false`.
    - Set `interactable` on your `Selectable` as `false`.

### Ensure all Unity's build tools are working

![run all](.Documentation/images/RunAll.png)

This **Run all in player** button is very useful as you could connect a device, press it, and left it alone without any further intervention. [In the future](https://forum.unity.com/threads/feedback-for-logassert-class.530539/#post-4559518) it is also going to be possible to just make a test build without device connected so you could distribute it to test farms like [Firebase Test Lab](https://firebase.google.com/docs/test-lab).

For now, let's make sure you have no fear of pressing this button every night before going to sleep. With `Minefield` you maybe able to create some good tests already, but something that may prevent this includes :

- Relying on bundle name. The bundle name is set to `com.UnityTestRunner.UnityTestRunner` automatically, it could break something like Firebase Unity API.
- Relying on `DEVELOPMENT_BUILD` preprocessor directive. The button will turn on `DEVELOPMENT_BUILD` and your game should execute properly with this directive on.
- Relying on manual post-build hacking, because this button currently send the build straight to the device.

After that, you will also be able to utilize [Unity Cloud Build](https://unity3d.com/unity/features/cloud-build)'s auto test per build feature.

![cloud build](.Documentation/images/CloudBuild.png)

## Design

After following the guidelines, you will be able to use `Minefield` for the following benefits.

### Assembly planning

You will have 3 kind of `asmdef` :

1. Core `asmdef` containing `static` variable that influences each scene in your game, among other things.
2. Scene `asmdef` referencing to core `asmdef` in order to access the `static` variable, or modify the `static` variable to make the next scene behave the way it wats.
3. Test `asmdef` referencing also to core `asmdef` to change the `static` variable before each test. It may optionally reference scene `asmdef`.

However even if you reference your scene `asmdef`, asserting on exposed fields of things is not a good design for test, since in the end you will use them as necessary portals to jump to the thing you actually want to test rather than wanted to test on them, and later you may introduced new exposed field just because you can't go to the desired things to test. This kind of "for test" exposed field should be avoided. (And actually exposed fields should be `[SerializeField] private` rather than `public`, so you can't use them from tests anyway.)

### `SceneTest`

By subclassing from this class in your test assembly :

- Each subclass will be a test for a single scene. You will have to provide your scene name because the `abstract` variable will ask you to.
- Each test case will be a fresh start of this scene. The built-in `[SetUp]` and `[TearDown]` will take care of everything for you.
- You will need to call `ActivateScene()` `protected` method **manually**. This design gives you an opportunity to hack up the `static` variable before allowing the scene to start, while don't have to specify the scene name on every test case because the subclass already asked you for it.
- Before your test case even begin the scene is already waited and 100% loaded thanks to `[UnitySetUp]`, and only needs an activation. Other scenes you may want to load in the test needs a proper wait but as the guideline says you shouldn't have to compose the test from multiple scenes to get along with `Minefield`.
- As in C# in general `static` variable do not reset in-between multiple test cases, **it will carry over**, because there is no domain reload in-between. (It is a good thing because the reload cost big performance hit.) You should always setup your `static` variable even though you think you don't want any particular value. In fact you may want a default, so you should set it to `default`/`new` instead of doing nothing. Do this in your own `[SetUp]` or `[UnitySetUp]` in your subclass.

After starting the scene you will want to check up on something or navigate inside the scene. We have no GUI in writing tests and it is difficult to use methods like `GameObject.Find` or `FindObjectOfType` to get the desired object to test "blindly". It is even more difficult to navigate "at the right time" when you could see nothing while writing a test. For this I have designed something called a **beacon**.

### Test beacons

This is a way to use `enum` to refer to `GameObject` in your scene, by "attaching the `enum`" to it. Attaching an `enum` is possible by a `MonoBehaviour` carrying that `enum`.

It make writing a test more fun because `enum` could be auto completed and self described, rather than having to use something like `GameObject.Find` which rely on object's `string` name that may be refactored without the test updating along. `enum` could be mass-refactored by most code editor.

It will also provide various test tools based on using these `enum`.

To use beacons, first you will declare some `enum` that represents all possible actions in the scene. This is called a beacon's **label**. This is like Flux or Redux's action string if you came from front end dev, however they could also represent any test checking point and doesn't have to be strictly "action". Other than they will help us test, it is an overview of what's possible, and a checklist if you are missing any tests or not.

```csharp
public class ModeSelectScreen : MonoBehaviour
{
    public enum Navigation
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

(Do not put a new entry inbetween the old ones, Unity serialize by `int` value and it will make old serialized value wrong. Or you could use an explicit integer.)

Next, declare a new class with that `enum` as the delegate of either `NavigationBeacon<>` if it is intended to be clicked on by uGUI event system, or `TestBeacon<>` for any `GameObject`. Put your `enum` type in the generic, and you should gain a serializable `enum` field of your type which shows up in editor.

```csharp
using E7.Minefield;
public class ModeSelectBeacon : NavigationBeacon<ModeSelectScreen.Navigation> { }
```

You are now ready to attach this new class as a component to `GameObject` in the scene. If it is a `NavigationBeacon<>`, the point of attach should be the raycast receiving elements, which depends if you got a `Button`, `EventTrigger`, or something else. The test tools can help you click on these objects. If `TestBeacon<>`, then it could be any `GameObject`.

As a bonus you could try typing `TestBeacon` or `NavigationBeacon` in the Scene view search box to return all beacons added so far. This is possible because each generic class you subclassed from is a subclass of non-generic version. The only purpose of this is for this use case because the search box couldn't list a generic class derived class even if the derived class has no generic type param, and it also could not search interfaces.

Make sure you don't have to dig up any other objects that doesn't have a beacon. It is the best if your test contains only beacon queries.

### Navigation with beacons

Navigation is available from `static` class entry point `Beacon.___`, which all of them require an `enum` as its argument, this `enum` must be on a beacon of type `NavigationBeacon<>`.

You may not have to assert anything at all to test navigation. If it doesn't throw any error then the test had already helped you.

This is an example of tests of my title scene. Started normally you could click on an `EventTrigger` "Touch to start" text to go to the next scene, and if the `static` contains an instruction to skip you are immediately skipped to the next scene (with some special transition that's not exactly the same as clicking "Touch to start".

The test could be completed by only `Beacon` calls.

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
    ActivateScene();
    yield return Beacon.WaitUntilClickable(TitleLogic.Navigation.TouchToStart);
    yield return Beacon.Click(TitleLogic.Navigation.TouchToStart);
    //Or use just `ClickWhenClickable(TitleLogic.Navigation.TouchToStart);` on this kind of pattern.
    yield return Beacon.WaitUntilClickable(ModeSelectScreen.Navigation.Training);
  }

  [UnityTest]
  public IEnumerator SkippingToModeSelect()
  {
    //Hacking the static variable to influence scene's behaviour, according to the guideline.
    SceneOptions.title = new SceneOptions.Title
    {
        titleMode = SceneOptions.Title.TitleMode.SkipToModeSelect
    };
    ActivateScene();
    yield return Beacon.WaitUntilClickable(ModeSelectScreen.Navigation.Training);
  }
}
```

You may not see any `Assert`, but the final `WaitUntilClickable` itself is already an implicit assertion that player could also continue on the scene. If that `WaitUntilClickable` which returns `CustomYieldInstruction` didn't go on, the test will fail from the timeout.

`Click` is a simulation of pointer down, **wait a frame**, and pointer up plus pointer click together in the next frame. So `yield return` is required because it is not an instantaneous action.

There are also various utilities that are not related to beacons available in `Utility` `static` class, like waiting for some `GameObject` to became active. They are used by the `Beacon` static class themselves, but in most cases you should try to stick to only `Beacon` class since that signifies that your beacon is enough or not.

### Assertion

Sometimes you don't want to just navigate around and call it a day. This is coming soon once I decided what's great to include.
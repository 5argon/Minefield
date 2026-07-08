# Play Button Replacement

You can turn the Test Runner tab into a "play button galore." Test cases become active development and design-iteration tools, not just regression guards. Advantages:

- No digging through the Project tab for the right scene — `SceneTest` knows how to start the scene from the case itself.
- You can start with any desired data state, since Minefield lets you do anything before `ActivateScene()`. A plain play-button press gives you no chance to alter state before `Awake`, which is often too late. (`[RuntimeInitializeOnLoad(RuntimeInitializeLoadType.BeforeSceneLoad)]` exists, but it applies to the whole project permanently and to every play mode test.)
  - Example: a Facebook-like reward that unlocks something permanently after returning. With a play-button press you can trigger it only once per save; then you must reset the save every time to test it again. Setting up per case is the real fix — and you get a regression test for free.
- Minefield's navigation methods can carry you to the place you want before handing control back — for instance a backpack/inventory screen that is a prefab, not a scene, and normally hard to reach without opening the menu and selecting it every time.

To do this, Minefield gives you two pieces: `[NoTimeout]` on the test method, and `yield return Utility.WaitForever();` after activating the scene. Now the case behaves like a supercharged play button:

```csharp
[UnityTest, NoTimeout]
public IEnumerator PlayFromBackpack()
{
    // Set up any state you want here, then:
    yield return ActivateScene();
    yield return Beacon.ClickWhen(Menu.Navigation.OpenBackpack, Is.Clickable);
    yield return Utility.WaitForever();   // hands control to you to play around
}
```

When you're done playing, "cement" the case by removing `[NoTimeout]` and the `WaitForever()` — and you now have a test covering everything you just touched. Keep doing this and you accumulate coverage as you build. It's like TDD, but at a higher integration-test level.

If some cases are purely development tools and not meant to become real tests, categorize them so the category dropdown makes them easy to find.

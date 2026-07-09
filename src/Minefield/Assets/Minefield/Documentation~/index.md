<div class="exc7-hero">
    <img src="images/splash.webp" alt="Minefield Test Tools">
    <h1 class="exc7-hero-title">Minefield Test Tools</h1>
    <p class="exc7-hero-desc">Concise play mode navigation testing for uGUI — drive your scenes by typed <code>enum</code> beacons, not object names or coordinates.</p>
</div>

> [!NOTE]
> Requires Unity 6.3 LTS (6000.3) or newer, plus uGUI (`com.unity.ugui`) and the Test Framework (`com.unity.test-framework`), both included with the Editor.

`Minefield` helps you write a concise **navigation** play mode test in Unity, along with a set of guidelines for designing a game that stays testable this way. The idea: stop reaching for the play button while building navigation. Let the Test Runner start the scene instead, and by the time the scene is finished you already have its tests — because writing a Minefield test is fast enough to do *while* you build, without breaking your creative flow.

Because these are still ordinary Unity tests, you can combine them with packages like [Performance Testing](https://docs.unity3d.com/Packages/com.unity.test-framework.performance@0.1/manual/index.html) to profile scenes while navigating.

A complete Minefield test reads as a short series of waits and clicks driven by an `enum` you declared yourself:

```csharp
using System.Collections;
using UnityEngine.TestTools;
using E7.Minefield;

public class SampleMinefieldTest : SceneTest
{
    protected override string Scene => "Title";

    [UnityTest]
    public IEnumerator TitleToTrainingNoUpperCharacter()
    {
        yield return ActivateScene();
        yield return Beacon.ClickWhen(TitleScreen.Navigation.TouchAnywhere, Is.Clickable);
        yield return Beacon.ClickWhen(ModeSelectScreen.Navigation.EnterTrainingMode, Is.Clickable);
        yield return Beacon.WaitUntil(CharacterSelectScreen.Navigation.ConfirmCharacter, Is.Clickable);
        Assert.Beacon(CharacterSelectScreen.Beacon.PlayerTwoCharacter, Is.Inactive);
    }
}
```

## Where to start

- [Overview and motivation](manual/index.md) — why navigation testing, and the two design pillars (scene-as-a-unit and beacons).
- [Why beacons](manual/why-beacons.md) — how a beacon compares to `data-testid` and role-based locators from the web testing world.
- [Testable project guideline](manual/guideline.md) — how to design scenes, `static` state, and navigation so they stay testable.
- [Installation](manual/installation.md) — install from the Git URL and wire up the assemblies.
- Walkthrough: [beacons](manual/beacons.md), [navigation and assertion](manual/navigation.md), [reporters](manual/reporters.md), and [the play-button replacement](manual/play-button.md).

## Easy way to pay for this software

Are you looking for a way to say thanks to this open source work other than code contribution?

It is easy! You can take a look at my myriad of niche Unity Asset Store **audio plugins** in [my publisher page](https://assetstore.unity.com/publishers/18007), grab something for your game, or tell your audio-caring friends about them. Thank you!

## License

[The license is MIT](https://github.com/5argon/Minefield/blob/master/src/Minefield/Assets/Minefield/LICENSE.md).

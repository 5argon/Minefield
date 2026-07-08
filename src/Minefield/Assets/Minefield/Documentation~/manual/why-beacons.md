# Why Beacons

A common first reaction to beacons is that they "pollute" your game objects with test-only components, so the test isn't really decoupled from the game. It's a fair worry, but it frames the trade-off the wrong way. **Every UI test couples to *something*.** The question is never "coupled or decoupled" — it's *what* you couple to, and whether a break is loud or silent.

## The web already had this debate

Modern web frameworks like [Playwright](https://playwright.dev/docs/locators) and [Testing Library](https://testing-library.com/docs/queries/about/) rank the ways you can locate an element, worst to best:

- **CSS selectors / XPath / DOM paths** couple to structure and styling. Both tools tell you to *avoid* these because the DOM changes often, leading to non-resilient tests. This is the direct analog of `GameObject.Find("Canvas/Panel/Button")` or `transform.GetChild(2)` hierarchy-crawling in Unity: it couples to incidental details that were never meant to be a test contract, and it breaks **silently** — the wrong object is found, or nothing is, with no error at author time.
- **Role / visible text** (`getByRole`, `getByText`) couple to user-facing semantics. This is Playwright's *preferred* tier, because the query also proves the element is reachable and accessible. The cost is coupling to copy, so renaming or localizing a label breaks the test — loudly, at least.
- **`data-testid`** is a deliberate, test-only attribute added to production markup so tests can find elements by a stable identity that survives restyles, restructures, and copy changes. Both tools treat this as the sanctioned escape hatch when role and text won't do.

## A beacon is `data-testid` — but better

A Minefield beacon is exactly `data-testid`: when structural and semantic queries get too flaky, you *add explicit test metadata to the production artifact.* So "it adds junk to my objects" isn't a Minefield wart — it's a well-understood, widely-accepted trade-off.

But an enum beacon is **better than `data-testid` on the axis that matters most**. `data-testid="training-button"` is a magic string on both sides — rename it in the markup, forget the test, and the test silently fails to find the element (the same failure mode as find-by-name; the web tolerates it only because JavaScript has no compile step). A Minefield enum removes that weakness: `ModeSelectScreen.Navigation.Training` is compile-checked on both the scene-authoring side and the test side. Delete or rename the member and **both fail to compile** — loud, immediate, un-ignorable.

| Approach | Couples to | How it breaks |
| --- | --- | --- |
| Find by name / hierarchy crawl | incidental structure | silently, or at runtime with no hint |
| Visible text / role | user-facing copy | loudly at runtime; also tests reachability |
| `data-testid` string | intentional contract | loudly at runtime, but a rename is silent |
| **Minefield enum beacon** | intentional contract | **at compile time, on both sides** |

## Two honest counterpoints

1. **Reachability.** `getByRole` fails when an element is invisible or out of the accessibility tree, so it doubles as an accessibility check. A bare beacon lookup bypasses the visual layer — an object can be found even if a real player couldn't see or hit it. Minefield recovers most of this through `Is.Clickable`, which does a real `EventSystem` raycast-first test plus `IsInteractable()`, and `ClickWhen(Is.Clickable)`, which retries every frame — conceptually identical to Playwright's **auto-waiting actionability** model. Prefer `Is.Clickable` over a raw `Get`/`FindActive` when you care whether a human could actually perform the action.
2. **Cost.** `data-testid` is a cheap string a build step can strip; a beacon is a `MonoBehaviour` shipped in your build, and it only covers objects you decided in advance to beacon. That last point is intentional (it forces you to enumerate what's worth testing), but it's a real flexibility difference versus a system that can locate anything on screen.

Why the heavier approach is *more* justified in Unity than on the web: the DOM hands you a semantic layer (ARIA roles, labels, accessible names) for free, so role-based locators have rich structure to query. A uGUI Canvas has essentially none — there is no accessibility tree to interrogate — so unless you add an explicit contract yourself, there is nothing stable and semantic to target. The web can lead with `getByRole` precisely because the platform gives it that layer gratis; Unity does not, which is why an explicit, typed beacon is a reasonable *default* in Unity rather than a last resort.

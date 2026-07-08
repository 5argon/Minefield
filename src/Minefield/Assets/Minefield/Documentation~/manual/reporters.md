# Reporters

Reporters are a set of `interface`s you add to your `MonoBehaviour` to assert more customized things. Think of them as another layer of **test metadata** — better than either exposing `public` fields just for tests (which ruins your class design) or climbing the object tree blindly with hierarchy traversal. Adding a reporter interface makes it explicit that this is for Minefield. Use it if you can accept the hack.

Reporter assertions go through the `Is.Reporting.___` fluent API, and where English grammar allows, some are also reachable from `Is.___`.

## `IMinefieldOnOffReporter`

Assert with `Is.On` / `Is.Off` (or `Is.Reporting.On` / `Is.Reporting.Off`). You define what "on" means — useful when a plain GameObject-active check isn't enough. For example, a component that shows and hides its members with `.enabled` isn't caught by `Is.Inactive`, so it can report a custom `IsOn`:

```csharp
public class APHint : MonoBehaviour, IMinefieldOnOffReporter
{
    public Image starImage;
    public TextMeshProUGUI numberText;

    public bool IsOn => starImage.enabled && numberText.enabled;
}
```

## `IMinefieldAmountReporter`

Used with `Is.Reporting.Amount(expectedAmount)` for a generic integer check. A health indicator that draws colored and hollow hearts is miserable to "count" from a test; having it report its remaining `Amount` yields far more concise test code:

```csharp
Assert.Beacon(GameSelector.Beacon.LowerScoreCounter, Is.Reporting.Amount(2));
Assert.Beacon(GameSelector.Beacon.UpperScoreCounter, Is.Reporting.Amount(1));
```

## `IMinefieldStatusReporter<T>`

`T` is constrained to `Enum`, and you provide a `T Status`. A limited `enum` is a natural way to summarize an overall state. You can assert with either `Is.Currently(___)` or `Is.Reporting.Status(___)` — the wording fits different scenarios (e.g. `Is.Currently(Pink)` for a character's color, `Is.Reporting.Status(Connected)` for a connection icon).

## `IMinefieldObjectReporter<T>`

Can report almost anything, but reach for it only when nothing else fits, since you assert with `Is.Reporting.Object(___)` (equality via `object.Equals`) — which may read awkwardly if the thing isn't really an "object" in the programming sense (for an `int`, prefer `IMinefieldAmountReporter`).

Together, the pattern is: pinpoint an object with a **beacon**, then query its **reporters** to assert.

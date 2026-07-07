using System;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    /// <summary>
    /// Inverts another <see cref="BeaconConstraint"/>. Create it with the <c>!</c> operator on any
    /// Minefield constraint, e.g. <c>!Is.Clickable</c> or <c>!Is.Active</c>, so you can wait for or
    /// assert the *absence* of a condition — a button that must stay locked mid-transition, an element
    /// that must disappear, and so on.
    ///
    /// The <c>!</c> operator is used instead of shadowing NUnit's <c>Is.Not</c>, because Minefield's
    /// <c>Is</c> inherits from <c>NUnit.Framework.Is</c> and hiding <c>Is.Not</c> would break ordinary
    /// NUnit expressions like <c>Is.Not.Null</c>.
    ///
    /// Example:
    /// <code>
    /// // Confirm the button is unclickable while the popup animates in.
    /// yield return Beacon.WaitUntil(Screen.Navigation.Confirm, !Is.Clickable);
    /// Assert.Beacon(Screen.Beacon.Portrait, !Is.Active);
    /// </code>
    /// </summary>
    public class NotConstraint : BeaconConstraint
    {
        private readonly BeaconConstraint inner;

        public NotConstraint(BeaconConstraint inner)
        {
            this.inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public override string Description => $"NOT ({inner.Description})";

        protected override ConstraintResult Assert()
        {
            // Re-run the wrapped constraint against the same requested beacon and invert its verdict.
            ConstraintResult innerResult = inner.ApplyToBeacon(beaconRequested);
            return new ConstraintResult(this, innerResult.ActualValue, isSuccess: !innerResult.IsSuccess);
        }

        public override string Diagnostic()
            => $"Expected beacon '{beaconRequested}' to NOT satisfy {inner.GetType().Name}, but it currently does.";
    }
}

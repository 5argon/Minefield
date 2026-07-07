using System;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public abstract class BeaconConstraint : Constraint
    {
        protected Enum beaconRequested;
        protected ILabelBeacon FoundBeacon { private set; get; }
        protected bool FindResult { private set; get; }
        //Overrides and interface implementation could not use constraints.
        public ConstraintResult ApplyToBeacon<BEACONTYPE>(BEACONTYPE beacon)
        where BEACONTYPE : Enum
            => ApplyToBeacon((Enum)beacon);

        /// <summary>
        /// Non-generic evaluation entry, used by <see cref="NotConstraint"/> to re-run a wrapped
        /// constraint against an already-boxed label.
        /// </summary>
        public ConstraintResult ApplyToBeacon(Enum beacon)
        {
            this.beaconRequested = beacon;
            this.FindResult = Beacon.FindActiveEnum(beacon, out ILabelBeacon found);
            this.FoundBeacon = found;
            return Assert();
        }

        protected abstract ConstraintResult Assert();

        public override ConstraintResult ApplyTo(object actual) => throw new NotImplementedException();

        /// <summary>
        /// Negates any Minefield constraint, e.g. <c>!Is.Clickable</c> or <c>!Is.Active</c>. Used
        /// instead of <c>Is.Not</c> so NUnit's own <c>Is.Not.*</c> expressions keep working.
        /// </summary>
        public static NotConstraint operator !(BeaconConstraint inner) => new NotConstraint(inner);

        /// <summary>
        /// Human-readable explanation of *why* this constraint currently fails, surfaced by
        /// <see cref="Beacon.WaitUntil{T}(T, BeaconConstraint, float)"/> when it times out. Call
        /// <see cref="ApplyToBeacon{BEACONTYPE}(BEACONTYPE)"/> first so the state below is populated.
        /// Override to give a more specific reason (see <see cref="ClickableConstraint"/>).
        /// </summary>
        public virtual string Diagnostic()
            => FindResult
                ? $"Beacon '{beaconRequested}' is active in the scene, but its condition ({GetType().Name}) is not met."
                : $"Beacon '{beaconRequested}' was NOT found active in the scene — the beacon is missing, or its GameObject (or a parent) is inactive.";
    }
}
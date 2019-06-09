using System;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public abstract class BeaconConstraint : Constraint
    {
        protected Enum beaconRequested;
        protected ITestBeacon FoundBeacon { private set; get; }
        protected bool FindResult { private set; get; }
        //Overrides and interface implementation could not use constraints.
        public ConstraintResult ApplyToBeacon<BEACONTYPE>(BEACONTYPE beacon)
        where BEACONTYPE : Enum
        {
            this.beaconRequested = beacon;
            this.FindResult = Beacon.FindActive(beacon, out ITestBeacon found);
            this.FoundBeacon = found;
            return Assert();
        }

        protected abstract ConstraintResult Assert();

        public override ConstraintResult ApplyTo(object actual) => throw new NotImplementedException();

    }
}
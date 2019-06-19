using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class ActiveConstraint : BeaconConstraint
    {
        public override string Description => $"Expecting game object with beacon {beaconRequested} to be active, but it is not found.";

        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult);
        }
    }
}
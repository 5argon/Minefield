using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class InactiveConstraint : BeaconConstraint
    {
        public override string Description => $"Expecting game object with beacon {FoundBeacon.Label} to be inactive, but it is active.";

        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: !FindResult);
        }
    }
}
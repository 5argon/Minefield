using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class ActiveConstraint : BeaconConstraint
    {
        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult);
        }
    }
}
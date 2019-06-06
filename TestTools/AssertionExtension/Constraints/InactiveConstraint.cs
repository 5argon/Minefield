//When integration testing using "on device" button DEVELOPMENT_BUILD is automatically on.
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class InactiveConstraint : BeaconConstraint
    {
        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: !FindResult);
        }
    }
}
using System.Linq;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class OffConstraint : OnOffConstraint
    {
        protected override ConstraintResult Assert()
        {
            var anyOff = GetOnOffs().Any(x => x.IsOn == false);
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && anyOff);
        }
    }
}
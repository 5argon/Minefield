using System.Linq;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class OnConstraint : OnOffConstraint
    {
        protected override ConstraintResult Assert()
        {
            var allOn = GetOnOffs().All(x => x.IsOn);
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && allOn);
        }
    }
}
using System.Linq;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class OnOffConstraint : ReporterBasedConstraint<IMinefieldOnOffReporter>
    {
        public override string Description
        => $"Expecting game object {FoundBeacon.GameObject.name} to report {(lookingForOn ? "On" : "Off")} but instead got {(lookingForOn ? "Off" : "On")}";

        protected bool lookingForOn;
        public OnOffConstraint(bool lookingForOn)
        {
            this.lookingForOn = lookingForOn;
        }

        protected override ConstraintResult Assert()
        {
            if (FindResult == false)
            {
                return new ConstraintResult(this, null, isSuccess: false);
            }

            if (lookingForOn)
            {
                var allOn = GetOnOffs().All(x => x.IsOn);
                return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && allOn);
            }
            else
            {
                var anyOff = GetOnOffs().Any(x => x.IsOn == false);
                return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && anyOff);
            }
        }

        protected IMinefieldOnOffReporter[] GetOnOffs() => GetAllReporters();
    }
}
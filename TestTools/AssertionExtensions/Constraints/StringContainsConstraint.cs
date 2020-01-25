using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class StringContainsConstraint : ReporterBasedConstraint<IMinefieldStringReporter>
    {
        public override string Description => FindResult ? $"Expecting reported string {ExpectedString} got {GetFirstReporter().String} instead. (reported from {FoundBeacon.GameObject.name})"
        : $"Beacon {beaconRequested} not found so could not get the requested reporter {nameof(IMinefieldAmountReporter)}.";

        string ExpectedString { get; }
        public StringContainsConstraint(string expectedString) => this.ExpectedString = expectedString;

        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && (GetFirstReporter().String.Contains(ExpectedString)));
        }
    }
}
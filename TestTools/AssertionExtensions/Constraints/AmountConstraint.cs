using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class AmountConstraint : ReporterBasedConstraint<IMinefieldAmountReporter>
    {
        public override string Description => FindResult ? $"Expecting reported amount {ExpectedAmount} got {GetFirstReporter().Amount} instead. (reported from {FoundBeacon.GameObject.name})"
        : $"Beacon {beaconRequested} not found so could not get the requested reporter {nameof(IMinefieldAmountReporter)}.";

        int ExpectedAmount { get; }
        public AmountConstraint(int expectedAmount) => this.ExpectedAmount = expectedAmount;

        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && (GetFirstReporter().Amount == ExpectedAmount));
        }
    }
}
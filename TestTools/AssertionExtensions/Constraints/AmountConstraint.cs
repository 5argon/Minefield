using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class AmountConstraint : ReporterBasedConstraint<IMinefieldAmountReporter>
    {
        int ExpectedAmount { get; }
        public AmountConstraint(int expectedAmount) => this.ExpectedAmount = expectedAmount;

        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && (GetFirstReporter().Amount == ExpectedAmount));
        }
    }
}
using System;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class StatusConstraint<T> : ReporterBasedConstraint<IMinefieldStatusReporter<T>>
    where T : Enum
    {
        public override string Description 
        => $"Expecting game object {FoundBeacon.GameObject.name} to report status {ExpectedStatus} but instead got {GetFirstReporter().Status}.";

        T ExpectedStatus { get; }
        public StatusConstraint(T expectedStatus) => this.ExpectedStatus = expectedStatus;
        protected override ConstraintResult Assert()
        {
            var reportedStatus = GetFirstReporter().Status;
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && reportedStatus.Equals(ExpectedStatus));
        }
    }
}
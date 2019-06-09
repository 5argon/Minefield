using System;
using NUnit.Framework.Constraints;

namespace E7.Minefield
{
    public class StatusConstraint<T> : ReporterBasedConstraint<IMinefieldStatusReporter<T>>
    where T : Enum
    {
        public override string Description 
        => FindResult ? $"Expecting game object {FoundBeacon.GameObject.name} to report status {ExpectedStatus} but instead got {GetFirstReporter().Status}."
        : $"Beacon {beaconRequested} not found so could not get the requested reporter {nameof(IMinefieldStatusReporter<T>)}.";

        T ExpectedStatus { get; }
        public StatusConstraint(T expectedStatus) => this.ExpectedStatus = expectedStatus;
        protected override ConstraintResult Assert()
        {
            return new ConstraintResult(this, FoundBeacon, isSuccess: FindResult && GetFirstReporter().Status.Equals(ExpectedStatus));
        }
    }
}